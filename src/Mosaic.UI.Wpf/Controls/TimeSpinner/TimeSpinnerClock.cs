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
    /// The clock arithmetic and formatting behind <see cref="TimeSpinner"/>, factored out so it can be tested
    /// without realizing a control template. Every member is pure, nothing here touches WPF or the control's state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Times are treated as whole minutes since midnight, so seconds and sub-second precision are discarded. All
    /// reasoning happens on a twenty four hour clock internally; the twelve hour presentation is a projection
    /// applied at the edges by <see cref="ToHour12"/>, <see cref="ToMeridiem"/>, and <see cref="Compose"/>.
    /// </para>
    /// <para>
    /// Culture only influences how values are rendered, not which values exist. A twelve hour spinner offers the
    /// same hours in every culture.
    /// </para>
    /// </remarks>
    public static class TimeSpinnerClock
    {
        /// <summary>
        /// The number of whole minutes in a day. One past the largest valid minute of day.
        /// </summary>
        public const int MinutesPerDay = 24 * 60;

        /// <summary>
        /// The hour values a twelve hour wheel offers, in the order they are displayed. Twelve leads because it is
        /// the hour that follows eleven on a twelve hour clock, which makes the wheel's order match ascending real
        /// time within a half day rather than ascending hour number.
        /// </summary>
        private static readonly int[] HourValues = { 12, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        /// <summary>
        /// The hours a twelve hour wheel offers, in display order: 12 followed by 1 through 11.
        /// </summary>
        public static IReadOnlyList<int> GetHourValues()
        {
            return HourValues;
        }

        /// <summary>
        /// The minutes a wheel offers for a given step, starting at zero and stopping before sixty. A step of one
        /// yields every minute, a step of fifteen yields 0, 15, 30, and 45.
        /// </summary>
        /// <param name="interval">The minute step. Coerced by <see cref="CoerceInterval"/> first.</param>
        public static IReadOnlyList<int> GetMinuteValues(int interval)
        {
            interval = CoerceInterval(interval);

            var values = new List<int>((60 / interval) + 1);

            for (int minute = 0; minute < 60; minute += interval)
            {
                values.Add(minute);
            }

            return values;
        }

        /// <summary>
        /// Constrains a minute step to a usable range. A step below one would produce an infinite wheel and a step
        /// above sixty would produce an empty one, so both are pulled back into 1 through 60.
        /// </summary>
        /// <param name="interval">The requested step.</param>
        public static int CoerceInterval(int interval)
        {
            return Math.Clamp(interval, 1, 60);
        }

        /// <summary>
        /// Discards seconds and any finer precision so that two values in the same minute always compare equal.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        public static TimeOnly Normalize(TimeOnly value)
        {
            return new TimeOnly(value.Hour, value.Minute);
        }

        /// <summary>
        /// Discards seconds from a nullable time, preserving <see langword="null"/>.
        /// </summary>
        /// <param name="value">The value to normalize.</param>
        public static TimeOnly? Normalize(TimeOnly? value)
        {
            return value.HasValue ? Normalize(value.Value) : null;
        }

        /// <summary>
        /// The number of whole minutes between midnight and <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The time to convert.</param>
        public static int ToMinuteOfDay(TimeOnly value)
        {
            return (value.Hour * 60) + value.Minute;
        }

        /// <summary>
        /// Rebuilds a time from a minute of day, clamping into the single day the value has to live in. Minute of
        /// day arithmetic is deliberately not allowed to wrap, because a time spinner has no next day to roll into.
        /// </summary>
        /// <param name="minuteOfDay">Minutes since midnight.</param>
        public static TimeOnly FromMinuteOfDay(int minuteOfDay)
        {
            minuteOfDay = Math.Clamp(minuteOfDay, 0, MinutesPerDay - 1);

            return new TimeOnly(minuteOfDay / 60, minuteOfDay % 60);
        }

        /// <summary>
        /// The hour a twelve hour wheel shows for a time. Midnight and noon both read as 12.
        /// </summary>
        /// <param name="value">The time to project.</param>
        public static int ToHour12(TimeOnly value)
        {
            int hour = value.Hour % 12;

            return hour == 0 ? 12 : hour;
        }

        /// <summary>
        /// The half of the day a time falls in. Noon counts as PM and midnight as AM, following the convention every
        /// twelve hour clock uses.
        /// </summary>
        /// <param name="value">The time to project.</param>
        public static TimeSpinnerMeridiem ToMeridiem(TimeOnly value)
        {
            return value.Hour < 12 ? TimeSpinnerMeridiem.Am : TimeSpinnerMeridiem.Pm;
        }

        /// <summary>
        /// The twenty four hour clock hour for a twelve hour wheel position.
        /// </summary>
        /// <param name="hour12">The hour shown on the wheel, 1 through 12.</param>
        /// <param name="meridiem">Which half of the day the hour belongs to.</param>
        public static int ToHour24(int hour12, TimeSpinnerMeridiem meridiem)
        {
            int hour = Math.Clamp(hour12, 1, 12) % 12;

            return meridiem == TimeSpinnerMeridiem.Pm ? hour + 12 : hour;
        }

        /// <summary>
        /// Builds a time from the three wheel positions. Out of range parts are clamped rather than rejected, so a
        /// half-updated set of wheels can never produce an invalid time part way through an edit.
        /// </summary>
        /// <param name="hour12">The hour shown on the wheel, 1 through 12.</param>
        /// <param name="minute">The minute, 0 through 59.</param>
        /// <param name="meridiem">Which half of the day the time falls in.</param>
        public static TimeOnly Compose(int hour12, int minute, TimeSpinnerMeridiem meridiem)
        {
            return new TimeOnly(ToHour24(hour12, meridiem), Math.Clamp(minute, 0, 59));
        }

        /// <summary>
        /// Clamps a time into an inclusive range. Both bounds are normalized to whole minutes first, and a reversed
        /// range is tolerated by treating <paramref name="minimum"/> as authoritative.
        /// </summary>
        /// <param name="value">The time to clamp.</param>
        /// <param name="minimum">The earliest permitted time.</param>
        /// <param name="maximum">The latest permitted time.</param>
        public static TimeOnly Clamp(TimeOnly value, TimeOnly minimum, TimeOnly maximum)
        {
            int total = ToMinuteOfDay(Normalize(value));
            int min = ToMinuteOfDay(Normalize(minimum));
            int max = ToMinuteOfDay(Normalize(maximum));

            if (max < min)
            {
                max = min;
            }

            return FromMinuteOfDay(Math.Clamp(total, min, max));
        }

        /// <summary>
        /// Moves a time onto the nearest value the wheels actually offer, which means both inside the permitted
        /// range and on the minute step.
        /// </summary>
        /// <remarks>
        /// Clamping alone is not enough once <paramref name="interval"/> is greater than one, because a bound such
        /// as 5:59 PM is not itself on a fifteen minute step. Snapping searches outward from the requested time for
        /// the closest offered value, so the result is always something the user can also reach by scrolling. When
        /// the range admits no value on the step at all, the clamped time is returned unchanged rather than throwing.
        /// </remarks>
        /// <param name="value">The time to snap.</param>
        /// <param name="minimum">The earliest permitted time.</param>
        /// <param name="maximum">The latest permitted time.</param>
        /// <param name="interval">The minute step the wheel offers.</param>
        public static TimeOnly Snap(TimeOnly value, TimeOnly minimum, TimeOnly maximum, int interval)
        {
            interval = CoerceInterval(interval);

            var clamped = Clamp(value, minimum, maximum);
            int target = ToMinuteOfDay(clamped);
            int min = ToMinuteOfDay(Normalize(minimum));
            int max = ToMinuteOfDay(Normalize(maximum));

            if (max < min)
            {
                max = min;
            }

            if (IsOnStep(target, interval))
            {
                return clamped;
            }

            // Search outward a minute at a time. The whole day is only 1440 minutes, so the loop is bounded and
            // cheap even in the pathological case where the range holds a single offered value.
            for (int distance = 1; distance < MinutesPerDay; distance++)
            {
                int below = target - distance;
                int above = target + distance;
                bool exhausted = true;

                if (below >= min)
                {
                    exhausted = false;

                    if (IsOnStep(below, interval))
                    {
                        return FromMinuteOfDay(below);
                    }
                }

                if (above <= max)
                {
                    exhausted = false;

                    if (IsOnStep(above, interval))
                    {
                        return FromMinuteOfDay(above);
                    }
                }

                if (exhausted)
                {
                    break;
                }
            }

            return clamped;
        }

        /// <summary>
        /// Whether a minute of day sits on the wheel's minute step. The step restarts every hour, so 6:00 and 6:15
        /// are on a fifteen minute step but 6:50 is not, regardless of where the day began.
        /// </summary>
        private static bool IsOnStep(int minuteOfDay, int interval)
        {
            return (minuteOfDay % 60) % interval == 0;
        }

        /// <summary>
        /// Whether a specific minute of a specific clock hour falls inside the permitted range.
        /// </summary>
        /// <param name="hour12">The hour shown on the wheel, 1 through 12.</param>
        /// <param name="minute">The minute of the hour.</param>
        /// <param name="meridiem">Which half of the day the hour belongs to.</param>
        /// <param name="minimum">The earliest permitted time.</param>
        /// <param name="maximum">The latest permitted time.</param>
        public static bool IsMinuteSelectable(int hour12, int minute, TimeSpinnerMeridiem meridiem, TimeOnly minimum, TimeOnly maximum)
        {
            int total = (ToHour24(hour12, meridiem) * 60) + Math.Clamp(minute, 0, 59);
            int min = ToMinuteOfDay(Normalize(minimum));
            int max = ToMinuteOfDay(Normalize(maximum));

            return total >= min && total <= (max < min ? min : max);
        }

        /// <summary>
        /// Whether an hour holds at least one offered minute inside the permitted range. An hour that holds none is
        /// still rendered on the wheel but cannot be landed on, which keeps the wheel's geometry stable as the user
        /// scrolls across a boundary.
        /// </summary>
        /// <param name="hour12">The hour shown on the wheel, 1 through 12.</param>
        /// <param name="meridiem">Which half of the day the hour belongs to.</param>
        /// <param name="minimum">The earliest permitted time.</param>
        /// <param name="maximum">The latest permitted time.</param>
        /// <param name="interval">The minute step the wheel offers.</param>
        public static bool IsHourSelectable(int hour12, TimeSpinnerMeridiem meridiem, TimeOnly minimum, TimeOnly maximum, int interval)
        {
            interval = CoerceInterval(interval);

            for (int minute = 0; minute < 60; minute += interval)
            {
                if (IsMinuteSelectable(hour12, minute, meridiem, minimum, maximum))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a half of the day holds at least one offered time inside the permitted range.
        /// </summary>
        /// <param name="meridiem">The half of the day to test.</param>
        /// <param name="minimum">The earliest permitted time.</param>
        /// <param name="maximum">The latest permitted time.</param>
        /// <param name="interval">The minute step the wheel offers.</param>
        public static bool IsMeridiemSelectable(TimeSpinnerMeridiem meridiem, TimeOnly minimum, TimeOnly maximum, int interval)
        {
            foreach (int hour in HourValues)
            {
                if (IsHourSelectable(hour, meridiem, minimum, maximum, interval))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Renders an hour value. Defaults to the unpadded number, which is how a twelve hour clock is written in
        /// United States English.
        /// </summary>
        /// <param name="hour12">The hour shown on the wheel, 1 through 12.</param>
        /// <param name="meridiem">Which half of the day the hour belongs to, used when <paramref name="format"/> is supplied.</param>
        /// <param name="culture">The culture supplying number formatting.</param>
        /// <param name="format">An optional standard or custom time format string, for example <c>hh</c>.</param>
        public static string FormatHour(int hour12, TimeSpinnerMeridiem meridiem, CultureInfo culture, string? format)
        {
            hour12 = Math.Clamp(hour12, 1, 12);

            if (string.IsNullOrEmpty(format))
            {
                return hour12.ToString(culture);
            }

            return SafeFormat(new DateTime(2000, 1, 1, ToHour24(hour12, meridiem), 0, 0), format, culture, () => hour12.ToString(culture));
        }

        /// <summary>
        /// Renders a minute value. Defaults to two digits, since a single digit minute is not a form anyone writes.
        /// </summary>
        /// <param name="minute">The minute of the hour.</param>
        /// <param name="culture">The culture supplying number formatting.</param>
        /// <param name="format">An optional standard or custom time format string.</param>
        public static string FormatMinute(int minute, CultureInfo culture, string? format)
        {
            minute = Math.Clamp(minute, 0, 59);

            if (string.IsNullOrEmpty(format))
            {
                return minute.ToString("00", culture);
            }

            return SafeFormat(new DateTime(2000, 1, 1, 0, minute, 0), format, culture, () => minute.ToString("00", culture));
        }

        /// <summary>
        /// Renders an AM or PM designator.
        /// </summary>
        /// <param name="meridiem">The half of the day to render.</param>
        /// <param name="culture">The culture supplying the designators.</param>
        /// <param name="mode">How to case the designator.</param>
        public static string FormatMeridiem(TimeSpinnerMeridiem meridiem, CultureInfo culture, TimeSpinnerMeridiemDisplayMode mode)
        {
            string designator = meridiem == TimeSpinnerMeridiem.Am
                ? culture.DateTimeFormat.AMDesignator
                : culture.DateTimeFormat.PMDesignator;

            // A culture that writes times on a twenty four hour clock publishes empty designators. This control is
            // twelve hour by definition, so it needs something to render.
            if (string.IsNullOrWhiteSpace(designator))
            {
                designator = meridiem == TimeSpinnerMeridiem.Am ? "AM" : "PM";
            }

            return mode switch
            {
                TimeSpinnerMeridiemDisplayMode.Uppercase => designator.ToUpper(culture),
                TimeSpinnerMeridiemDisplayMode.Lowercase => designator.ToLower(culture),
                _ => designator
            };
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
    }
}
