/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Covers the date arithmetic and formatting behind <see cref="DateSpinner"/>. None of this needs a WPF
    /// control, which is exactly why it lives in its own class.
    /// </summary>
    public class DateSpinnerCalendarTests
    {
        [Fact]
        public void Normalize_Strips_The_Time_Of_Day()
        {
            var value = new DateTime(2025, 3, 15, 17, 42, 9);

            Assert.Equal(new DateTime(2025, 3, 15), DateSpinnerCalendar.Normalize(value));
            Assert.Null(DateSpinnerCalendar.Normalize((DateTime?)null));
        }

        [Fact]
        public void Clamp_Pins_A_Date_Into_The_Range()
        {
            var min = new DateTime(2025, 1, 1);
            var max = new DateTime(2025, 12, 31);

            Assert.Equal(min, DateSpinnerCalendar.Clamp(new DateTime(2020, 6, 1), min, max));
            Assert.Equal(max, DateSpinnerCalendar.Clamp(new DateTime(2030, 6, 1), min, max));
            Assert.Equal(new DateTime(2025, 6, 1), DateSpinnerCalendar.Clamp(new DateTime(2025, 6, 1, 23, 0, 0), min, max));
        }

        [Fact]
        public void Clamp_Tolerates_A_Reversed_Range_By_Trusting_The_Minimum()
        {
            var min = new DateTime(2025, 6, 1);
            var max = new DateTime(2020, 1, 1);

            Assert.Equal(min, DateSpinnerCalendar.Clamp(new DateTime(2030, 1, 1), min, max));
        }

        [Theory]
        [InlineData(2025, 2, 28)]
        [InlineData(2024, 2, 29)]
        [InlineData(1900, 2, 28)]
        [InlineData(2000, 2, 29)]
        [InlineData(2025, 4, 30)]
        [InlineData(2025, 1, 31)]
        public void DaysInMonth_Handles_Leap_Years_And_Month_Lengths(int year, int month, int expected)
        {
            Assert.Equal(expected, DateSpinnerCalendar.DaysInMonth(year, month));
        }

        [Fact]
        public void Compose_Clamps_The_Day_When_Moving_To_A_Shorter_Month()
        {
            // January 31 to February keeps the day where it fits and clamps it where it does not.
            Assert.Equal(new DateTime(2025, 2, 28), DateSpinnerCalendar.Compose(2025, 2, 31));
            Assert.Equal(new DateTime(2024, 2, 29), DateSpinnerCalendar.Compose(2024, 2, 31));
            Assert.Equal(new DateTime(2025, 4, 30), DateSpinnerCalendar.Compose(2025, 4, 31));
            Assert.Equal(new DateTime(2025, 3, 31), DateSpinnerCalendar.Compose(2025, 3, 31));
        }

        [Fact]
        public void Compose_Clamps_Out_Of_Range_Parts_Rather_Than_Throwing()
        {
            Assert.Equal(new DateTime(2025, 1, 1), DateSpinnerCalendar.Compose(2025, 0, 0));
            Assert.Equal(new DateTime(2025, 12, 31), DateSpinnerCalendar.Compose(2025, 13, 99));
        }

        [Fact]
        public void GetMonthRange_Narrows_At_The_Minimum_Boundary()
        {
            var min = new DateTime(2025, 3, 15);
            var max = new DateTime(2030, 12, 31);

            // January and February 2025 fall before the minimum.
            Assert.Equal((3, 12), DateSpinnerCalendar.GetMonthRange(2025, min, max));

            // Any later year in the range offers every month.
            Assert.Equal((1, 12), DateSpinnerCalendar.GetMonthRange(2026, min, max));
        }

        [Fact]
        public void GetMonthRange_Narrows_At_The_Maximum_Boundary()
        {
            var min = new DateTime(2020, 1, 1);
            var max = new DateTime(2025, 7, 4);

            Assert.Equal((1, 7), DateSpinnerCalendar.GetMonthRange(2025, min, max));
            Assert.Equal((1, 12), DateSpinnerCalendar.GetMonthRange(2024, min, max));
        }

        [Fact]
        public void GetMonthRange_Returns_An_Empty_Range_For_A_Year_Outside_The_Bounds()
        {
            var min = new DateTime(2025, 1, 1);
            var max = new DateTime(2025, 12, 31);
            var (first, last) = DateSpinnerCalendar.GetMonthRange(2024, min, max);

            Assert.True(last < first);
        }

        [Fact]
        public void GetDayRange_Narrows_In_The_Boundary_Month()
        {
            var min = new DateTime(2025, 3, 15);
            var max = new DateTime(2025, 9, 10);

            // Days 1 through 14 of March 2025 fall before the minimum.
            Assert.Equal((15, 31), DateSpinnerCalendar.GetDayRange(2025, 3, min, max));

            // Days 11 through 30 of September 2025 fall after the maximum.
            Assert.Equal((1, 10), DateSpinnerCalendar.GetDayRange(2025, 9, min, max));

            // A month in the middle is unrestricted.
            Assert.Equal((1, 30), DateSpinnerCalendar.GetDayRange(2025, 6, min, max));
        }

        [Fact]
        public void GetDayRange_Respects_February_Lengths_At_The_Maximum()
        {
            var min = new DateTime(2020, 1, 1);

            // A maximum past the end of February still stops at the real last day of the month.
            Assert.Equal((1, 29), DateSpinnerCalendar.GetDayRange(2024, 2, min, new DateTime(2024, 2, 29)));
            Assert.Equal((1, 28), DateSpinnerCalendar.GetDayRange(2025, 2, min, new DateTime(2025, 3, 31)));
        }

        [Fact]
        public void FormatMonth_Honors_The_Display_Mode()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal("January", DateSpinnerCalendar.FormatMonth(2025, 1, culture, DateSpinnerMonthDisplayMode.FullName, null));
            Assert.Equal("Jan", DateSpinnerCalendar.FormatMonth(2025, 1, culture, DateSpinnerMonthDisplayMode.AbbreviatedName, null));
            Assert.Equal("1", DateSpinnerCalendar.FormatMonth(2025, 1, culture, DateSpinnerMonthDisplayMode.Numeric, null));
            Assert.Equal("01", DateSpinnerCalendar.FormatMonth(2025, 1, culture, DateSpinnerMonthDisplayMode.TwoDigitNumeric, null));
        }

        [Fact]
        public void FormatMonth_Is_Culture_Aware()
        {
            Assert.Equal("janvier", DateSpinnerCalendar.FormatMonth(2025, 1, new CultureInfo("fr-FR"), DateSpinnerMonthDisplayMode.FullName, null));
            Assert.Equal("Januar", DateSpinnerCalendar.FormatMonth(2025, 1, new CultureInfo("de-DE"), DateSpinnerMonthDisplayMode.FullName, null));
        }

        [Fact]
        public void An_Explicit_Format_Wins_Over_The_Display_Mode()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal("Mar", DateSpinnerCalendar.FormatMonth(2025, 3, culture, DateSpinnerMonthDisplayMode.FullName, "MMM"));
            Assert.Equal("03", DateSpinnerCalendar.FormatDay(2025, 3, 3, culture, "dd"));
            Assert.Equal("25", DateSpinnerCalendar.FormatYear(2025, culture, "yy"));
        }

        [Fact]
        public void A_Format_The_Runtime_Cannot_Honor_Falls_Back_Instead_Of_Throwing()
        {
            var culture = new CultureInfo("en-US");

            // A lone letter is a standard format specifier, and most of the alphabet is not a valid one.
            Assert.Equal("January", DateSpinnerCalendar.FormatMonth(2025, 1, culture, DateSpinnerMonthDisplayMode.FullName, "q"));
            Assert.Equal("5", DateSpinnerCalendar.FormatDay(2025, 1, 5, culture, "q"));
            Assert.Equal("2025", DateSpinnerCalendar.FormatYear(2025, culture, "q"));
        }

        [Fact]
        public void Default_Formats_Are_Numeric_Day_And_Four_Digit_Year()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal("5", DateSpinnerCalendar.FormatDay(2025, 1, 5, culture, null));
            Assert.Equal("2025", DateSpinnerCalendar.FormatYear(2025, culture, null));
            Assert.Equal("0007", DateSpinnerCalendar.FormatYear(7, culture, null));
        }

        [Fact]
        public void GetFieldOrder_Follows_The_Culture_Short_Date_Pattern()
        {
            Assert.Equal(
                new[] { DateSpinnerField.Month, DateSpinnerField.Day, DateSpinnerField.Year },
                DateSpinnerCalendar.GetFieldOrder(new CultureInfo("en-US")));

            Assert.Equal(
                new[] { DateSpinnerField.Day, DateSpinnerField.Month, DateSpinnerField.Year },
                DateSpinnerCalendar.GetFieldOrder(new CultureInfo("en-GB")));

            Assert.Equal(
                new[] { DateSpinnerField.Year, DateSpinnerField.Month, DateSpinnerField.Day },
                DateSpinnerCalendar.GetFieldOrder(new CultureInfo("ja-JP")));
        }

        [Fact]
        public void GetFieldOrder_Always_Returns_All_Three_Fields()
        {
            // A pattern with no day component still has to place the day somewhere.
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.DateTimeFormat.ShortDatePattern = "MM/yyyy";

            var order = DateSpinnerCalendar.GetFieldOrder(culture);

            Assert.Equal(3, order.Count);
            Assert.Equal(DateSpinnerField.Month, order[0]);
            Assert.Equal(DateSpinnerField.Year, order[1]);
            Assert.Contains(DateSpinnerField.Day, order);
        }

        [Fact]
        public void GetFieldOrder_Ignores_Quoted_Literals_In_The_Pattern()
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();

            // The quoted "day" must not be read as a day specifier ahead of the real one.
            culture.DateTimeFormat.ShortDatePattern = "'day' yyyy-MM-dd";

            Assert.Equal(
                new[] { DateSpinnerField.Year, DateSpinnerField.Month, DateSpinnerField.Day },
                DateSpinnerCalendar.GetFieldOrder(culture));
        }
    }
}
