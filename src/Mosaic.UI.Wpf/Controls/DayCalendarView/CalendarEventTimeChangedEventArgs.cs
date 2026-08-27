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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides the proposed old and new times when a calendar event is dragged.
    /// </summary>
    public sealed class CalendarEventTimeChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEventTimeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="calendarEvent">The source object supplied by the calendar's items source.</param>
        /// <param name="oldStart">The original event start.</param>
        /// <param name="oldEnd">The original event end.</param>
        /// <param name="newStart">The proposed event start.</param>
        /// <param name="newEnd">The proposed event end.</param>
        public CalendarEventTimeChangedEventArgs(object calendarEvent, DateTime oldStart, DateTime oldEnd, DateTime newStart, DateTime newEnd)
        {
            Event = calendarEvent ?? throw new ArgumentNullException(nameof(calendarEvent));
            OldStart = oldStart;
            OldEnd = oldEnd;
            NewStart = newStart;
            NewEnd = newEnd;
        }

        /// <summary>
        /// Gets the original object from the calendar's items source.
        /// </summary>
        public object Event { get; }

        /// <summary>
        /// Gets the original start time.
        /// </summary>
        public DateTime OldStart { get; }

        /// <summary>
        /// Gets the original end time.
        /// </summary>
        public DateTime OldEnd { get; }

        /// <summary>
        /// Gets the proposed start time.
        /// </summary>
        public DateTime NewStart { get; }

        /// <summary>
        /// Gets the proposed end time.
        /// </summary>
        public DateTime NewEnd { get; }
    }
}
