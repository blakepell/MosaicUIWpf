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
    /// Covers <see cref="TimeSpinnerClock"/>, the pure clock arithmetic behind <see cref="TimeSpinner"/>. None of
    /// these need a WPF thread, since nothing here touches a control.
    /// </summary>
    public class TimeSpinnerClockTests
    {
        #region Projection

        [Theory]
        [InlineData(0, 12)]
        [InlineData(1, 1)]
        [InlineData(11, 11)]
        [InlineData(12, 12)]
        [InlineData(13, 1)]
        [InlineData(23, 11)]
        public void Hour_Twelve_Is_What_Midnight_And_Noon_Both_Read_As(int hour24, int expected)
        {
            Assert.Equal(expected, TimeSpinnerClock.ToHour12(new TimeOnly(hour24, 0)));
        }

        [Theory]
        [InlineData(0, TimeSpinnerMeridiem.Am)]
        [InlineData(11, TimeSpinnerMeridiem.Am)]
        [InlineData(12, TimeSpinnerMeridiem.Pm)]
        [InlineData(23, TimeSpinnerMeridiem.Pm)]
        public void Noon_Counts_As_Pm_And_Midnight_As_Am(int hour24, TimeSpinnerMeridiem expected)
        {
            Assert.Equal(expected, TimeSpinnerClock.ToMeridiem(new TimeOnly(hour24, 0)));
        }

        [Theory]
        [InlineData(12, TimeSpinnerMeridiem.Am, 0)]
        [InlineData(12, TimeSpinnerMeridiem.Pm, 12)]
        [InlineData(1, TimeSpinnerMeridiem.Am, 1)]
        [InlineData(1, TimeSpinnerMeridiem.Pm, 13)]
        [InlineData(11, TimeSpinnerMeridiem.Pm, 23)]
        public void Compose_Round_Trips_Through_The_Twenty_Four_Hour_Clock(int hour12, TimeSpinnerMeridiem meridiem, int expected)
        {
            Assert.Equal(new TimeOnly(expected, 30), TimeSpinnerClock.Compose(hour12, 30, meridiem));
        }

        [Fact]
        public void Normalize_Discards_Seconds()
        {
            Assert.Equal(new TimeOnly(17, 42), TimeSpinnerClock.Normalize(new TimeOnly(17, 42, 9, 500)));
        }

        [Fact]
        public void The_Hour_Wheel_Leads_With_Twelve_So_Scrolling_Down_Moves_Forward_In_Time()
        {
            Assert.Equal(new[] { 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, TimeSpinnerClock.GetHourValues());
        }

        #endregion

        #region Minute Values

        [Fact]
        public void A_Step_Of_One_Offers_Every_Minute()
        {
            Assert.Equal(60, TimeSpinnerClock.GetMinuteValues(1).Count);
        }

        [Fact]
        public void A_Step_Of_Fifteen_Offers_Four_Minutes()
        {
            Assert.Equal(new[] { 0, 15, 30, 45 }, TimeSpinnerClock.GetMinuteValues(15));
        }

        [Fact]
        public void A_Step_That_Does_Not_Divide_Sixty_Leaves_A_Short_Gap_At_The_End_Of_The_Hour()
        {
            Assert.Equal(new[] { 0, 7, 14, 21, 28, 35, 42, 49, 56 }, TimeSpinnerClock.GetMinuteValues(7));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(61, 60)]
        public void An_Unusable_Step_Is_Coerced_Rather_Than_Rejected(int requested, int expected)
        {
            Assert.Equal(expected, TimeSpinnerClock.CoerceInterval(requested));
        }

        #endregion

        #region Clamping And Snapping

        [Fact]
        public void A_Time_Before_The_Minimum_Is_Clamped_Up()
        {
            Assert.Equal(new TimeOnly(8, 0), TimeSpinnerClock.Clamp(new TimeOnly(6, 0), new TimeOnly(8, 0), new TimeOnly(17, 0)));
        }

        [Fact]
        public void A_Time_After_The_Maximum_Is_Clamped_Down()
        {
            Assert.Equal(new TimeOnly(17, 0), TimeSpinnerClock.Clamp(new TimeOnly(18, 30), new TimeOnly(8, 0), new TimeOnly(17, 0)));
        }

        [Fact]
        public void A_Reversed_Range_Treats_The_Minimum_As_Authoritative()
        {
            Assert.Equal(new TimeOnly(17, 0), TimeSpinnerClock.Clamp(new TimeOnly(9, 0), new TimeOnly(17, 0), new TimeOnly(8, 0)));
        }

        [Theory]
        [InlineData(7, 9, 0)]
        [InlineData(8, 9, 15)]
        [InlineData(0, 9, 0)]
        [InlineData(45, 9, 45)]
        public void Snapping_Moves_To_The_Nearest_Offered_Minute(int minute, int expectedHour, int expectedMinute)
        {
            var snapped = TimeSpinnerClock.Snap(new TimeOnly(9, minute), new TimeOnly(0, 0), new TimeOnly(23, 59), 15);

            Assert.Equal(new TimeOnly(expectedHour, expectedMinute), snapped);
        }

        [Fact]
        public void Snapping_Never_Leaves_The_Permitted_Range_Even_When_A_Bound_Is_Off_The_Step()
        {
            // 5:50 PM is the maximum but is not on a fifteen minute step, so the last reachable value is 5:45 PM.
            var snapped = TimeSpinnerClock.Snap(new TimeOnly(18, 0), new TimeOnly(8, 0), new TimeOnly(17, 50), 15);

            Assert.Equal(new TimeOnly(17, 45), snapped);
        }

        [Fact]
        public void Snapping_Falls_Back_To_The_Clamped_Time_When_The_Range_Offers_Nothing_On_The_Step()
        {
            // A range of a single off-step minute has no value on the step at all.
            var snapped = TimeSpinnerClock.Snap(new TimeOnly(9, 0), new TimeOnly(9, 7), new TimeOnly(9, 7), 15);

            Assert.Equal(new TimeOnly(9, 7), snapped);
        }

        #endregion

        #region Selectability

        [Fact]
        public void An_Hour_Outside_The_Range_Is_Rendered_But_Not_Selectable()
        {
            // 12 AM is midnight, which a business hours range excludes.
            Assert.False(TimeSpinnerClock.IsHourSelectable(12, TimeSpinnerMeridiem.Am, new TimeOnly(8, 0), new TimeOnly(17, 0), 1));
            Assert.True(TimeSpinnerClock.IsHourSelectable(9, TimeSpinnerMeridiem.Am, new TimeOnly(8, 0), new TimeOnly(17, 0), 1));
        }

        [Fact]
        public void A_Boundary_Hour_Is_Selectable_When_It_Holds_At_Least_One_Offered_Minute()
        {
            // The range starts at 8:30, so 8 AM is still reachable but only from its later minutes.
            Assert.True(TimeSpinnerClock.IsHourSelectable(8, TimeSpinnerMeridiem.Am, new TimeOnly(8, 30), new TimeOnly(17, 0), 15));
            Assert.False(TimeSpinnerClock.IsMinuteSelectable(8, 0, TimeSpinnerMeridiem.Am, new TimeOnly(8, 30), new TimeOnly(17, 0)));
            Assert.True(TimeSpinnerClock.IsMinuteSelectable(8, 30, TimeSpinnerMeridiem.Am, new TimeOnly(8, 30), new TimeOnly(17, 0)));
        }

        [Fact]
        public void An_Hour_Whose_Only_In_Range_Minutes_Fall_Off_The_Step_Is_Not_Selectable()
        {
            // The range opens at 8:50 and the step is fifteen, so nothing in the 8 o'clock hour can be landed on.
            Assert.False(TimeSpinnerClock.IsHourSelectable(8, TimeSpinnerMeridiem.Am, new TimeOnly(8, 50), new TimeOnly(17, 0), 15));
        }

        [Fact]
        public void A_Half_Of_The_Day_Outside_The_Range_Is_Not_Selectable()
        {
            Assert.False(TimeSpinnerClock.IsMeridiemSelectable(TimeSpinnerMeridiem.Am, new TimeOnly(13, 0), new TimeOnly(17, 0), 1));
            Assert.True(TimeSpinnerClock.IsMeridiemSelectable(TimeSpinnerMeridiem.Pm, new TimeOnly(13, 0), new TimeOnly(17, 0), 1));
        }

        #endregion

        #region Formatting

        [Fact]
        public void Hours_Default_To_The_Unpadded_Number_And_Minutes_To_Two_Digits()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal("9", TimeSpinnerClock.FormatHour(9, TimeSpinnerMeridiem.Am, culture, null));
            Assert.Equal("05", TimeSpinnerClock.FormatMinute(5, culture, null));
        }

        [Fact]
        public void A_Format_String_Wins_Over_The_Default()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal("09", TimeSpinnerClock.FormatHour(9, TimeSpinnerMeridiem.Am, culture, "hh"));
            Assert.Equal("21", TimeSpinnerClock.FormatHour(9, TimeSpinnerMeridiem.Pm, culture, "HH"));
        }

        [Fact]
        public void A_Format_String_The_Runtime_Cannot_Honor_Degrades_Rather_Than_Throwing()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal("9", TimeSpinnerClock.FormatHour(9, TimeSpinnerMeridiem.Am, culture, "%"));
        }

        [Fact]
        public void Designators_Follow_The_Culture_And_The_Display_Mode()
        {
            var culture = new CultureInfo("en-US");

            Assert.Equal("AM", TimeSpinnerClock.FormatMeridiem(TimeSpinnerMeridiem.Am, culture, TimeSpinnerMeridiemDisplayMode.Culture));
            Assert.Equal("pm", TimeSpinnerClock.FormatMeridiem(TimeSpinnerMeridiem.Pm, culture, TimeSpinnerMeridiemDisplayMode.Lowercase));
        }

        [Fact]
        public void A_Twenty_Four_Hour_Culture_Publishes_No_Designators_So_The_Invariant_Ones_Are_Used()
        {
            // A twelve hour control still has to render something, and a blank wheel is not an option.
            var culture = new CultureInfo("de-DE");

            Assert.NotEqual(string.Empty, TimeSpinnerClock.FormatMeridiem(TimeSpinnerMeridiem.Am, culture, TimeSpinnerMeridiemDisplayMode.Culture));
        }

        #endregion
    }
}
