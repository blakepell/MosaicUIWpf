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
    /// Provides data for the <see cref="AutoCompleteBox.LookupFailed"/> routed event.
    /// </summary>
    public sealed class AutoCompleteBoxLookupFailedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoCompleteBoxLookupFailedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event associated with this instance.</param>
        /// <param name="source">The event source.</param>
        /// <param name="exception">The lookup exception.</param>
        public AutoCompleteBoxLookupFailedEventArgs(RoutedEvent routedEvent, object source, Exception exception)
            : base(routedEvent, source)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets the exception thrown by the lookup provider.
        /// </summary>
        public Exception Exception { get; }
    }
}
