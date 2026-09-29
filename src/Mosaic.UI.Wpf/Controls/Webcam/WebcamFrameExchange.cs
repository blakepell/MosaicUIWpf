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
    /// A top-down Bgr32 staging frame owned by a <see cref="WebcamFrameExchange"/>.
    /// </summary>
    internal sealed class WebcamFrame
    {
        public byte[] Pixels { get; private set; } = [];

        public int Width { get; private set; }

        public int Height { get; private set; }

        /// <summary>
        /// Bytes per row in <see cref="Pixels"/>; always <c>Width * 4</c> (rows are tightly packed).
        /// </summary>
        public int Stride { get; private set; }

        public long Timestamp { get; set; }

        public long FrameNumber { get; set; }

        /// <summary>
        /// Sets the frame dimensions, growing the pixel buffer only when it is too small.  This is the
        /// only allocation in the frame path and happens once per slot per resolution increase.
        /// </summary>
        public void EnsureFormat(int width, int height)
        {
            int stride = checked(width * 4);
            int length = checked(stride * height);

            if (Pixels.Length < length)
            {
                Pixels = new byte[length];
            }

            Width = width;
            Height = height;
            Stride = stride;
        }
    }

    /// <summary>
    /// A lock-light triple buffer that hands the newest captured frame from the camera worker to the
    /// UI thread.  The producer never waits for the consumer; if the UI falls behind, intermediate
    /// frames are overwritten (dropped) so the display always shows the latest frame and no backlog
    /// can form.  <see cref="Publish"/> reports when a UI update needs to be scheduled, which bounds the
    /// dispatcher queue to a single pending operation.
    /// </summary>
    internal sealed class WebcamFrameExchange
    {
        private readonly Lock _gate = new();
        private WebcamFrame _write = new();
        private WebcamFrame _ready = new();
        private WebcamFrame _read = new();
        private bool _hasReady;
        private bool _renderPending;

        /// <summary>
        /// Gets the slot the producer fills next.  Only the producer thread may use it.
        /// </summary>
        public WebcamFrame WriteFrame => _write;

        /// <summary>
        /// Publishes <see cref="WriteFrame"/> as the latest frame (replacing any frame the consumer has
        /// not picked up yet) and hands the producer a free slot.
        /// </summary>
        /// <returns><see langword="true"/> when the consumer has no update pending and must be scheduled.</returns>
        public bool Publish()
        {
            lock (_gate)
            {
                (_write, _ready) = (_ready, _write);
                _hasReady = true;

                if (_renderPending)
                {
                    return false;
                }

                _renderPending = true;
                return true;
            }
        }

        /// <summary>
        /// Takes the latest published frame.  The returned slot belongs to the consumer until its next
        /// call, and the producer never writes to it in the meantime.
        /// </summary>
        /// <returns>The latest frame, or <see langword="null"/> when nothing new was published.</returns>
        public WebcamFrame? AcquireLatest()
        {
            lock (_gate)
            {
                _renderPending = false;

                if (!_hasReady)
                {
                    return null;
                }

                (_read, _ready) = (_ready, _read);
                _hasReady = false;
                return _read;
            }
        }
    }
}
