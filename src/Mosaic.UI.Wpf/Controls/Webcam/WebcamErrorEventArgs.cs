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
    /// Provides data for the <see cref="Webcam.Error"/> event.
    /// </summary>
    public sealed class WebcamErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebcamErrorEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception that describes the failure.</param>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public WebcamErrorEventArgs(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            Exception = exception;
        }

        /// <summary>
        /// Gets the exception that describes the failure.  This is normally a <see cref="WebcamException"/>.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the category of the failure.
        /// </summary>
        public WebcamErrorKind Kind => (Exception as WebcamException)?.Kind ?? WebcamErrorKind.Unknown;

        /// <summary>
        /// Gets the device involved in the failure, if known.
        /// </summary>
        public WebcamDevice? Device => (Exception as WebcamException)?.Device;
    }
}
