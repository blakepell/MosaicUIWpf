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
    /// Categorizes webcam failures so callers can react without parsing messages or HRESULTs.
    /// </summary>
    public enum WebcamErrorKind
    {
        /// <summary>
        /// The failure could not be categorized.
        /// </summary>
        Unknown,

        /// <summary>
        /// <see cref="Webcam.StartAsync"/> was called while <see cref="Webcam.Device"/> is <see langword="null"/>.
        /// </summary>
        DeviceNotSelected,

        /// <summary>
        /// The device could not be found or opened.
        /// </summary>
        DeviceUnavailable,

        /// <summary>
        /// The device is being used exclusively by another application.
        /// </summary>
        DeviceInUse,

        /// <summary>
        /// Windows denied access to the camera, typically because of camera privacy settings.
        /// </summary>
        AccessDenied,

        /// <summary>
        /// The device was disconnected while it was streaming.
        /// </summary>
        DeviceDisconnected,

        /// <summary>
        /// Windows Media Foundation could not be initialized.
        /// </summary>
        InitializationFailed,

        /// <summary>
        /// The device offers no video format that can be converted for display.
        /// </summary>
        UnsupportedFormat,

        /// <summary>
        /// Frames could not be read from the device.
        /// </summary>
        FrameAcquisitionFailed
    }

    /// <summary>
    /// The exception reported by the <see cref="Webcam"/> control and <see cref="WebcamDeviceManager"/>.
    /// </summary>
    public sealed class WebcamException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebcamException"/> class.
        /// </summary>
        /// <param name="kind">The category of the failure.</param>
        /// <param name="message">A message that describes the failure.</param>
        /// <param name="device">The device involved, if any.</param>
        /// <param name="innerException">The underlying exception, if any.</param>
        public WebcamException(WebcamErrorKind kind, string message, WebcamDevice? device = null, Exception? innerException = null)
            : base(message, innerException)
        {
            Kind = kind;
            Device = device;

            if (innerException is not null)
            {
                HResult = innerException.HResult;
            }
        }

        /// <summary>
        /// Gets the category of the failure.
        /// </summary>
        public WebcamErrorKind Kind { get; }

        /// <summary>
        /// Gets the device involved in the failure, if known.
        /// </summary>
        public WebcamDevice? Device { get; }
    }
}
