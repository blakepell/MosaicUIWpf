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
    public class CalendarEvent : ObservableObject
    {
        private DateTime _startDate;
        private DateTime _endDate;
        private string _title = string.Empty;
        private string? _description;
        private Brush? _background;
        private bool _isReadOnly;

        /// <summary>
        /// Gets or sets the inclusive start of the appointment.
        /// </summary>
        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        /// <summary>
        /// Gets or sets the exclusive end of the appointment.
        /// </summary>
        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        /// <summary>
        /// Gets or sets the appointment title.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets the optional secondary appointment text.
        /// </summary>
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// Gets or sets the optional brush that overrides the calendar's default event background.
        /// </summary>
        public Brush? Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the appointment cannot be moved by dragging.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the appointment cannot be moved; otherwise, <see langword="false"/>.
        /// The default is <see langword="false"/>.
        /// </value>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }
    }
}
