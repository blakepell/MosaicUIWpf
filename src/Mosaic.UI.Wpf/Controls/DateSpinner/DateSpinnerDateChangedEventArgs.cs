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
    /// Provides data for the <see cref="DateSpinner.SelectedDateChanged"/> routed event.
    /// </summary>
    public class DateSpinnerDateChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateSpinnerDateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The object raising the event.</param>
        /// <param name="oldDate">The previously selected date, or <see langword="null"/> if nothing was selected.</param>
        /// <param name="newDate">The newly selected date, or <see langword="null"/> if the selection was cleared.</param>
        public DateSpinnerDateChangedEventArgs(RoutedEvent routedEvent, object source, DateTime? oldDate, DateTime? newDate)
            : base(routedEvent, source)
        {
            this.OldDate = oldDate;
            this.NewDate = newDate;
        }

        /// <summary>
        /// The date that was selected before the change, or <see langword="null"/> if nothing was selected.
        /// Always normalized to midnight.
        /// </summary>
        public DateTime? OldDate { get; }

        /// <summary>
        /// The date that is selected after the change, or <see langword="null"/> if the selection was cleared.
        /// Always normalized to midnight.
        /// </summary>
        public DateTime? NewDate { get; }
    }
}
