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

using static Mosaic.UI.Wpf.Controls.MediaFoundationInterop;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Translates Media Foundation failures into categorized <see cref="WebcamException"/> instances
    /// with messages an application can show to a user.
    /// </summary>
    internal static class WebcamErrors
    {
        /// <summary>
        /// Wraps <paramref name="exception"/> in a <see cref="WebcamException"/>, classifying it by HRESULT.
        /// </summary>
        /// <param name="exception">The failure to translate.  A <see cref="WebcamException"/> is returned unchanged.</param>
        /// <param name="device">The device involved, if any.</param>
        /// <param name="fallback">The category used when the HRESULT is not recognized.</param>
        public static WebcamException Translate(Exception exception, WebcamDevice? device, WebcamErrorKind fallback)
        {
            if (exception is WebcamException webcamException)
            {
                return webcamException;
            }

            WebcamErrorKind kind = Classify(exception.HResult) ?? fallback;
            string message = Describe(kind, device);

            if (exception is COMException)
            {
                message += $" (HRESULT 0x{exception.HResult:X8})";
            }

            return new WebcamException(kind, message, device, exception);
        }

        /// <summary>
        /// Creates a <see cref="WebcamException"/> with the standard message for <paramref name="kind"/>.
        /// </summary>
        public static WebcamException Create(WebcamErrorKind kind, WebcamDevice? device, Exception? innerException = null)
        {
            return new WebcamException(kind, Describe(kind, device), device, innerException);
        }

        private static WebcamErrorKind? Classify(int hr)
        {
            if (hr == E_ACCESSDENIED)
            {
                return WebcamErrorKind.AccessDenied;
            }

            if (hr == ERROR_BUSY || hr == MF_E_HW_MFT_FAILED_START_STREAMING || hr == MF_E_VIDEO_RECORDING_DEVICE_PREEMPTED || hr == MF_E_VIDEO_DEVICE_LOCKED)
            {
                return WebcamErrorKind.DeviceInUse;
            }

            if (hr == MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED || hr == ERROR_DEVICE_NOT_CONNECTED || hr == ERROR_DEVICE_REMOVED)
            {
                return WebcamErrorKind.DeviceDisconnected;
            }

            if (hr == ERROR_FILE_NOT_FOUND || hr == MF_E_NOT_FOUND)
            {
                return WebcamErrorKind.DeviceUnavailable;
            }

            if (hr == MF_E_INVALIDMEDIATYPE || hr == MF_E_TOPO_CODEC_NOT_FOUND)
            {
                return WebcamErrorKind.UnsupportedFormat;
            }

            return null;
        }

        private static string Describe(WebcamErrorKind kind, WebcamDevice? device)
        {
            string name = device is null ? "The camera" : $"The camera '{device.Name}'";

            return kind switch
            {
                WebcamErrorKind.DeviceNotSelected => "No webcam device is selected. Set Webcam.Device before starting.",
                WebcamErrorKind.DeviceUnavailable => $"{name} could not be opened. It may have been disconnected.",
                WebcamErrorKind.DeviceInUse => $"{name} is in use by another application.",
                WebcamErrorKind.AccessDenied => $"Windows denied access to {name.ToLowerInvariantFirst()}. Check that camera access is allowed for desktop apps in Settings > Privacy & security > Camera.",
                WebcamErrorKind.DeviceDisconnected => $"{name} was disconnected.",
                WebcamErrorKind.InitializationFailed => "Windows Media Foundation could not be initialized.",
                WebcamErrorKind.UnsupportedFormat => $"{name} does not offer a video format that can be displayed.",
                WebcamErrorKind.FrameAcquisitionFailed => $"Frames could not be read from {name.ToLowerInvariantFirst()}.",
                _ => $"{name} reported an unexpected error."
            };
        }

        private static string ToLowerInvariantFirst(this string value)
        {
            return value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value[1..];
        }
    }
}
