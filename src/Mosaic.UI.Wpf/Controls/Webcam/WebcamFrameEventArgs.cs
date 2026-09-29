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
    /// Provides data for the <see cref="Webcam.FrameReceived"/> event.
    /// </summary>
    /// <remarks>
    /// The event is raised on the UI thread after the preview has been updated.  No bitmap is created
    /// for it: the pixel data stays in the control's reusable staging buffer and can be copied with
    /// <see cref="CopyPixels"/> <b>only while the handler is running</b>.  Use
    /// <see cref="Webcam.CaptureFrameAsync"/> when a standalone <see cref="System.Windows.Media.Imaging.BitmapSource"/> is needed.
    /// </remarks>
    public sealed class WebcamFrameEventArgs : EventArgs
    {
        private WebcamFrame? _frame;

        internal WebcamFrameEventArgs(WebcamFrame frame)
        {
            _frame = frame;
            PixelWidth = frame.Width;
            PixelHeight = frame.Height;
            Stride = frame.Stride;
            Timestamp = TimeSpan.FromTicks(frame.Timestamp);
            FrameNumber = frame.FrameNumber;
        }

        /// <summary>
        /// Gets the frame width, in pixels.
        /// </summary>
        public int PixelWidth { get; }

        /// <summary>
        /// Gets the frame height, in pixels.
        /// </summary>
        public int PixelHeight { get; }

        /// <summary>
        /// Gets the number of bytes per row copied by <see cref="CopyPixels"/>.
        /// </summary>
        public int Stride { get; }

        /// <summary>
        /// Gets the pixel format of the frame, which is always <see cref="PixelFormats.Bgr32"/> (top-down rows).
        /// </summary>
        public PixelFormat PixelFormat => PixelFormats.Bgr32;

        /// <summary>
        /// Gets the capture timestamp reported by the device.
        /// </summary>
        public TimeSpan Timestamp { get; }

        /// <summary>
        /// Gets the sequence number of the frame since the camera started.  Gaps indicate frames that
        /// were dropped because the UI was busy.
        /// </summary>
        public long FrameNumber { get; }

        /// <summary>
        /// Copies the frame's pixels (<see cref="Stride"/> × <see cref="PixelHeight"/> bytes) into <paramref name="destination"/>.
        /// </summary>
        /// <param name="destination">The buffer that receives the pixels.</param>
        /// <exception cref="InvalidOperationException">Called after the event handler returned.</exception>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
        public void CopyPixels(Span<byte> destination)
        {
            WebcamFrame frame = _frame ?? throw new InvalidOperationException("Frame pixels are only available while the FrameReceived handler is running.");
            int length = Stride * PixelHeight;

            if (destination.Length < length)
            {
                throw new ArgumentException($"The destination must be at least {length} bytes.", nameof(destination));
            }

            frame.Pixels.AsSpan(0, length).CopyTo(destination);
        }

        internal void Invalidate() => _frame = null;
    }
}
