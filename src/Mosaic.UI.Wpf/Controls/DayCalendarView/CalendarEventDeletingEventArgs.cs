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
    /// Provides the event proposed for deletion and allows a handler to cancel the removal.
    /// </summary>
    public sealed class CalendarEventDeletingEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEventDeletingEventArgs"/> class.
        /// </summary>
        /// <param name="calendarEvent">The source object supplied by the calendar's items source.</param>
        public CalendarEventDeletingEventArgs(object calendarEvent)
        {
            Event = calendarEvent ?? throw new ArgumentNullException(nameof(calendarEvent));
        }

        /// <summary>
        /// Gets the original object from the calendar's items source that is proposed for deletion.
        /// </summary>
        public object Event { get; }

        /// <summary>
        /// Gets or sets a value that indicates whether the deletion should be abandoned.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to leave the event in the items source; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool Cancel { get; set; }
    }
}
