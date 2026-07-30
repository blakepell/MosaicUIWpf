/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Globalization;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The date arithmetic and formatting behind <see cref="DateSpinner"/>, factored out so it can be tested
    /// without realizing a control template. Every member is pure, nothing here touches WPF or the control's state.
    /// </summary>
    /// <remarks>
    /// All calculations run on the proleptic Gregorian calendar exposed by <see cref="DateTime"/> and ignore the
    /// time of day. Culture only influences how values are rendered and the order the fields appear in, not which
    /// values exist.
    /// </remarks>
    public static class DateSpinnerCalendar
    {
        /// <summary>
        /// Strips the time of day so that two dates on the same calendar day always compare equal.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        public static DateTime Normalize(DateTime value)
        {
            return value.Date;
        }

        /// <summary>
        /// Strips the time of day from a nullable date, preserving <see langword="null"/>.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        public static DateTime? Normalize(DateTime? value)
        {
            return value?.Date;
        }

        /// <summary>
        /// Clamps a date into an inclusive range. Both bounds are normalized first, and a reversed range is
        /// tolerated by treating <paramref name="minimum"/> as authoritative.
        /// </summary>
        /// <param name="value">The date to clamp.</param>
        /// <param name="minimum">The earliest permitted date.</param>
        /// <param name="maximum">The latest permitted date.</param>
        public static DateTime Clamp(DateTime value, DateTime minimum, DateTime maximum)
        {
            var date = Normalize(value);
            var min = Normalize(minimum);
            var max = Normalize(maximum);

            if (max < min)
            {
                max = min;
            }

            if (date < min)
            {
                return min;
            }

            return date > max ? max : date;
        }

        /// <summary>
        /// Builds a date from its parts, clamping the day to the last valid day of the target month so that a
        /// transition such as January 31 to February can never produce an invalid date. Leap years are handled by
        /// <see cref="DateTime.DaysInMonth"/>.
        /// </summary>
        /// <param name="year">The year, clamped to the range <see cref="DateTime"/> supports.</param>
        /// <param name="month">The month number, clamped to 1 through 12.</param>
        /// <param name="day">The desired day of month. Clamped down when the month is too short.</param>
        public static DateTime Compose(int year, int month, int day)
        {
            year = Math.Clamp(year, 1, 9999);
            month = Math.Clamp(month, 1, 12);
            day = Math.Clamp(day, 1, DateTime.DaysInMonth(year, month));

            return new DateTime(year, month, day);
        }

        /// <summary>
        /// The inclusive range of month numbers selectable in <paramref name="year"/>. The range narrows in the
        /// first and last year of the permitted range, so a minimum of March 15 2025 yields 3 through 12 for 2025.
        /// </summary>
        /// <param name="year">The year being displayed.</param>
        /// <param name="minimum">The spinner's minimum date.</param>
        /// <param name="maximum">The spinner's maximum date.</param>
        public static (int First, int Last) GetMonthRange(int year, DateTime minimum, DateTime maximum)
        {
            var min = Normalize(minimum);
            var max = Normalize(maximum);

            int first = year == min.Year ? min.Month : 1;
            int last = year == max.Year ? max.Month : 12;

            // A year entirely outside the range has no selectable months at all.
            if (year < min.Year || year > max.Year)
            {
                return (0, -1);
            }

            return (first, last);
        }

        /// <summary>
        /// The inclusive range of days selectable in <paramref name="year"/> and <paramref name="month"/>. The
        /// range narrows in the boundary month, so a minimum of March 15 2025 yields 15 through 31 for March 2025.
        /// </summary>
        /// <param name="year">The year being displayed.</param>
        /// <param name="month">The month being displayed.</param>
        /// <param name="minimum">The spinner's minimum date.</param>
        /// <param name="maximum">The spinner's maximum date.</param>
        public static (int First, int Last) GetDayRange(int year, int month, DateTime minimum, DateTime maximum)
        {
            var min = Normalize(minimum);
            var max = Normalize(maximum);

            month = Math.Clamp(month, 1, 12);
            int daysInMonth = DateTime.DaysInMonth(Math.Clamp(year, 1, 9999), month);

            int first = year == min.Year && month == min.Month ? min.Day : 1;
            int last = year == max.Year && month == max.Month ? Math.Min(max.Day, daysInMonth) : daysInMonth;

            var monthStart = new DateTime(Math.Clamp(year, 1, 9999), month, 1);

            // A month entirely outside the range has no selectable days at all.
            if (monthStart.AddDays(daysInMonth - 1) < min || monthStart > max)
            {
                return (0, -1);
            }

            return (first, last);
        }

        /// <summary>
        /// The number of days in the supplied month, honoring leap years.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month number.</param>
        public static int DaysInMonth(int year, int month)
        {
            return DateTime.DaysInMonth(Math.Clamp(year, 1, 9999), Math.Clamp(month, 1, 12));
        }

        /// <summary>
        /// Renders a month value.
        /// </summary>
        /// <param name="year">The year the month belongs to, used when <paramref name="format"/> is supplied.</param>
        /// <param name="month">The month number.</param>
        /// <param name="culture">The culture supplying month names and number formatting.</param>
        /// <param name="mode">How to render the month when <paramref name="format"/> is <see langword="null"/>.</param>
        /// <param name="format">An optional standard or custom date format string that wins over <paramref name="mode"/>.</param>
        public static string FormatMonth(int year, int month, CultureInfo culture, DateSpinnerMonthDisplayMode mode, string? format)
        {
            month = Math.Clamp(month, 1, 12);

            if (!string.IsNullOrEmpty(format))
            {
                return SafeFormat(new DateTime(Math.Clamp(year, 1, 9999), month, 1), format, culture, () => culture.DateTimeFormat.GetMonthName(month));
            }

            return mode switch
            {
                DateSpinnerMonthDisplayMode.AbbreviatedName => culture.DateTimeFormat.GetAbbreviatedMonthName(month),
                DateSpinnerMonthDisplayMode.Numeric => month.ToString(culture),
                DateSpinnerMonthDisplayMode.TwoDigitNumeric => month.ToString("00", culture),
                _ => culture.DateTimeFormat.GetMonthName(month)
            };
        }

        /// <summary>
        /// Renders a day of month value. Defaults to the localized number with no padding.
        /// </summary>
        /// <param name="year">The year the day belongs to, used when <paramref name="format"/> is supplied.</param>
        /// <param name="month">The month the day belongs to, used when <paramref name="format"/> is supplied.</param>
        /// <param name="day">The day of month.</param>
        /// <param name="culture">The culture supplying number formatting.</param>
        /// <param name="format">An optional standard or custom date format string.</param>
        public static string FormatDay(int year, int month, int day, CultureInfo culture, string? format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return day.ToString(culture);
            }

            return SafeFormat(Compose(year, month, day), format, culture, () => day.ToString(culture));
        }

        /// <summary>
        /// Renders a year value. Defaults to four digits.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="culture">The culture supplying number formatting.</param>
        /// <param name="format">An optional standard or custom date format string.</param>
        public static string FormatYear(int year, CultureInfo culture, string? format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return year.ToString("0000", culture);
            }

            return SafeFormat(new DateTime(Math.Clamp(year, 1, 9999), 1, 1), format, culture, () => year.ToString("0000", culture));
        }

        /// <summary>
        /// Applies a user supplied format string, falling back to a safe default when the string is not a format
        /// <see cref="DateTime"/> understands. A bad format in XAML should degrade, not throw during layout.
        /// </summary>
        private static string SafeFormat(DateTime value, string format, CultureInfo culture, Func<string> fallback)
        {
            try
            {
                return value.ToString(format, culture);
            }
            catch (FormatException)
            {
                return fallback();
            }
        }

        /// <summary>
        /// The order the month, day, and year fields should be laid out in for a culture, derived from the
        /// culture's short date pattern. United States English yields month, day, year while most of Europe
        /// yields day, month, year.
        /// </summary>
        /// <param name="culture">The culture whose short date pattern determines the order.</param>
        /// <returns>All three fields, in display order. Fields absent from the pattern are appended in the order month, day, year.</returns>
        public static IReadOnlyList<DateSpinnerField> GetFieldOrder(CultureInfo culture)
        {
            string pattern = culture.DateTimeFormat.ShortDatePattern ?? string.Empty;
            var order = new List<DateSpinnerField>(3);
            bool inQuote = false;
            char quoteChar = '\0';

            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];

                // An escaped character is literal text and never a format specifier.
                if (c == '\\')
                {
                    i++;
                    continue;
                }

                if (inQuote)
                {
                    if (c == quoteChar)
                    {
                        inQuote = false;
                    }

                    continue;
                }

                if (c == '\'' || c == '"')
                {
                    inQuote = true;
                    quoteChar = c;
                    continue;
                }

                DateSpinnerField? field = c switch
                {
                    'M' => DateSpinnerField.Month,
                    'd' => DateSpinnerField.Day,
                    'y' => DateSpinnerField.Year,
                    _ => null
                };

                if (field.HasValue && !order.Contains(field.Value))
                {
                    order.Add(field.Value);
                }
            }

            // A pattern that omits a component (or a culture with no usable pattern) still needs a full order.
            foreach (var field in new[] { DateSpinnerField.Month, DateSpinnerField.Day, DateSpinnerField.Year })
            {
                if (!order.Contains(field))
                {
                    order.Add(field);
                }
            }

            return order;
        }
    }
}
