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
    /// Provides data for the <see cref="TimeSpinner.SelectedTimeChanged"/> routed event.
    /// </summary>
    public class TimeSpinnerTimeChangedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpinnerTimeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The object raising the event.</param>
        /// <param name="oldTime">The previously selected time, or <see langword="null"/> if nothing was selected.</param>
        /// <param name="newTime">The newly selected time, or <see langword="null"/> if the selection was cleared.</param>
        public TimeSpinnerTimeChangedEventArgs(RoutedEvent routedEvent, object source, TimeOnly? oldTime, TimeOnly? newTime)
            : base(routedEvent, source)
        {
            this.OldTime = oldTime;
            this.NewTime = newTime;
        }

        /// <summary>
        /// The time that was selected before the change, or <see langword="null"/> if nothing was selected. Always
        /// normalized to whole minutes.
        /// </summary>
        public TimeOnly? OldTime { get; }

        /// <summary>
        /// The time that is selected after the change, or <see langword="null"/> if the selection was cleared.
        /// Always normalized to whole minutes.
        /// </summary>
        public TimeOnly? NewTime { get; }
    }
}
