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
    /// Describes the lifecycle state of a <see cref="Webcam"/> control.
    /// </summary>
    public enum WebcamState
    {
        /// <summary>
        /// No camera is open.
        /// </summary>
        Stopped,

        /// <summary>
        /// The camera is being opened and configured.
        /// </summary>
        Starting,

        /// <summary>
        /// The camera is open and frames are being delivered.
        /// </summary>
        Running,

        /// <summary>
        /// The camera is being closed and its resources released.
        /// </summary>
        Stopping,

        /// <summary>
        /// The camera failed to start or stopped unexpectedly.  All resources have been released and
        /// <see cref="Webcam.StartAsync"/> may be called again.
        /// </summary>
        Error
    }
}
