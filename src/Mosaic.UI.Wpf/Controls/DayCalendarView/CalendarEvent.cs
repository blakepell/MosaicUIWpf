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
    /// Represents a mutable calendar appointment suitable for use with <see cref="DayCalendarView"/>.
    /// </summary>
    public partial class CalendarEvent : ObservableObject
    {
        /// <summary>
        /// Gets or sets the inclusive start of the appointment.
        /// </summary>
        [ObservableProperty]
        public partial DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the exclusive end of the appointment.
        /// </summary>
        [ObservableProperty]
        public partial DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets the appointment title.
        /// </summary>
        [ObservableProperty]
        public partial string Title { get; set; }

        /// <summary>
        /// Gets or sets the optional secondary appointment text.
        /// </summary>
        [ObservableProperty]
        public partial string? Description { get; set; }

        /// <summary>
        /// Gets or sets the optional brush that overrides the calendar's default event background.
        /// </summary>
        [ObservableProperty]
        public partial Brush? Background { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the appointment cannot be moved by dragging.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the appointment cannot be moved; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        [ObservableProperty]
        public partial bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the appointment can be deleted from the calendar.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if pressing <c>Delete</c> while the event has focus may remove it;
        /// otherwise, <see langword="false"/>. The default is <see langword="true"/>.
        /// </value>
        [ObservableProperty]
        public partial bool CanDelete { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEvent"/> class.
        /// </summary>
        public CalendarEvent()
        {
            Title = string.Empty;
            CanDelete = true;
        }
    }
}
