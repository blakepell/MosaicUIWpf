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
    /// One capture session against one device: opens the device, negotiates an RGB32 output, reads
    /// frames into a <see cref="WebcamFrameExchange"/> and releases every native resource when it ends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every Media Foundation object is created, used and released on a single dedicated MTA worker
    /// thread, so no COM pointer ever crosses threads.  The only exception is <see cref="ForceShutdownSource"/>,
    /// which may call IMFMediaSource::Shutdown from another thread (under <see cref="_sourceGate"/>) to
    /// unblock a read on a stalled device; Media Foundation sources are free threaded.
    /// </para>
    /// <para>
    /// Output format: the source reader is created with advanced video processing enabled and asked
    /// for MFVideoFormat_RGB32.  Media Foundation then inserts whatever decoder (MJPG) and color
    /// converter (NV12/YUY2) the device format needs, so no YUV conversion is implemented here.
    /// RGB32 is B,G,R,X in memory, which is exactly WPF's <see cref="PixelFormats.Bgr32"/>, so frames
    /// reach the <see cref="System.Windows.Media.Imaging.WriteableBitmap"/> with a plain memory copy.
    /// </para>
    /// <para>
    /// A session is single use: create a new instance to open the camera again.
    /// </para>
    /// </remarks>
    internal sealed class MediaFoundationCamera
    {
        private static readonly TimeSpan StopGracePeriod = TimeSpan.FromSeconds(2);
        private const int MaxNativeMediaTypes = 1024;
        private const int MaxConsecutiveFrameFailures = 30;

        private readonly WebcamFrameExchange _exchange;
        private readonly Action _frameReady;
        private readonly Action<MediaFoundationCamera, WebcamException> _faulted;
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Lock _sourceGate = new();

        private Thread? _thread;
        private volatile bool _stopRequested;
        private nint _source;
        private nint _reader;
        private int _width;
        private int _height;
        private int _defaultStride;
        private long _frameNumber;
        private int _lastFrameError;

        /// <summary>
        /// Initializes a new capture session.  Nothing is opened until <see cref="StartAsync"/>.
        /// </summary>
        /// <param name="device">The device to open.</param>
        /// <param name="requestedResolution">The requested mode, or <see langword="null"/> for automatic selection.</param>
        /// <param name="exchange">Receives captured frames.</param>
        /// <param name="frameReady">Invoked on the worker thread when the consumer must be scheduled.</param>
        /// <param name="faulted">Invoked on the worker thread, after all resources are released, when the session ends unexpectedly.</param>
        public MediaFoundationCamera(WebcamDevice device, WebcamResolution? requestedResolution, WebcamFrameExchange exchange,
            Action frameReady, Action<MediaFoundationCamera, WebcamException> faulted)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(exchange);
            ArgumentNullException.ThrowIfNull(frameReady);
            ArgumentNullException.ThrowIfNull(faulted);

            Device = device;
            RequestedResolution = requestedResolution;
            _exchange = exchange;
            _frameReady = frameReady;
            _faulted = faulted;
        }

        /// <summary>
        /// Gets the device this session opens.
        /// </summary>
        public WebcamDevice Device { get; }

        /// <summary>
        /// Gets the mode requested when the session was created.
        /// </summary>
        public WebcamResolution? RequestedResolution { get; }

        /// <summary>
        /// Gets the negotiated mode.  Valid once <see cref="StartAsync"/> has completed successfully.
        /// </summary>
        public WebcamResolution? ActualResolution { get; private set; }

        /// <summary>
        /// Gets the distinct modes offered by the device.  Valid once <see cref="StartAsync"/> has completed successfully.
        /// </summary>
        public IReadOnlyList<WebcamResolution> AvailableResolutions { get; private set; } = [];

        /// <summary>
        /// Starts the worker thread, which opens and configures the device.
        /// </summary>
        /// <returns>A task that completes when frames start flowing, or faults with a <see cref="WebcamException"/>.</returns>
        public Task StartAsync()
        {
            if (_thread is not null)
            {
                throw new InvalidOperationException("A capture session can only be started once.");
            }

            _thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "Mosaic Webcam capture"
            };

            _thread.SetApartmentState(ApartmentState.MTA);
            _thread.Start();

            return _started.Task;
        }

        /// <summary>
        /// Signals the worker to stop without waiting for it.  Used when the dispatcher shuts down.
        /// </summary>
        public void RequestStop() => _stopRequested = true;

        /// <summary>
        /// Stops frame acquisition and waits until every native resource has been released.  Never throws.
        /// </summary>
        public async Task StopAsync()
        {
            _stopRequested = true;

            if (_thread is null)
            {
                return;
            }

            // A healthy device returns from ReadSample within one frame interval.
            Task completed = _completed.Task;

            if (await Task.WhenAny(completed, Task.Delay(StopGracePeriod)).ConfigureAwait(false) == completed)
            {
                return;
            }

            // The device stalled: shutting the source down makes the pending ReadSample return.
            ForceShutdownSource();

            if (await Task.WhenAny(completed, Task.Delay(StopGracePeriod)).ConfigureAwait(false) != completed)
            {
                Debug.WriteLine($"Webcam: capture thread for '{Device.Name}' did not exit after a forced shutdown.");
            }
        }

        /// <summary>
        /// Opens <paramref name="device"/> just long enough to list the modes it offers.
        /// </summary>
        public static IReadOnlyList<WebcamResolution> QueryResolutions(WebcamDevice device)
        {
            using IDisposable lease = MediaFoundationLifetime.Acquire();
            nint source = 0;
            nint reader = 0;

            try
            {
                source = CreateDeviceSource(device);
                reader = CreateSourceReader(source);
                return WebcamModeSelector.GetDistinctResolutions(EnumerateModes(reader));
            }
            catch (Exception ex)
            {
                throw WebcamErrors.Translate(ex, device, WebcamErrorKind.DeviceUnavailable);
            }
            finally
            {
                ComUnknown.Release(ref reader);

                if (source != 0)
                {
                    MfMediaSource.Shutdown(source);
                    ComUnknown.Release(ref source);
                }
            }
        }

        private void Run()
        {
            IDisposable? lease = null;

            try
            {
                lease = MediaFoundationLifetime.Acquire();
                Open();
            }
            catch (Exception ex)
            {
                ReleaseNativeObjects();
                lease?.Dispose();
                _started.TrySetException(WebcamErrors.Translate(ex, Device, WebcamErrorKind.DeviceUnavailable));
                _completed.TrySetResult();
                return;
            }

            _started.TrySetResult();
            WebcamException? fault = null;

            try
            {
                ReadLoop();
            }
            catch (Exception ex) when (!_stopRequested)
            {
                fault = ClassifyStreamingFailure(ex);
            }
            catch (Exception ex)
            {
                // A requested stop (especially a forced source shutdown) can surface as a read failure; that is expected.
                Debug.WriteLine($"Webcam: read ended during stop: {ex.Message}");
            }
            finally
            {
                ReleaseNativeObjects();
                lease.Dispose();
                _completed.TrySetResult();
            }

            // Raised after cleanup so the device is already free when the application reacts.
            if (fault is not null)
            {
                _faulted(this, fault);
            }
        }

        private void Open()
        {
            nint source = CreateDeviceSource(Device);

            lock (_sourceGate)
            {
                _source = source;
            }

            _reader = CreateSourceReader(source);

            ThrowIfFailed(MfSourceReader.SetStreamSelection(_reader, AllStreams, false), "Deselecting streams");
            ThrowIfFailed(MfSourceReader.SetStreamSelection(_reader, FirstVideoStream, true), "Selecting the video stream");

            List<NativeVideoMode> modes = EnumerateModes(_reader);
            AvailableResolutions = WebcamModeSelector.GetDistinctResolutions(modes);

            foreach (var mode in WebcamModeSelector.OrderCandidates(modes, RequestedResolution))
            {
                if (TrySelectMode(mode))
                {
                    ReadCurrentFormat();
                    ActualResolution = new WebcamResolution(_width, _height, mode.FramesPerSecond);
                    return;
                }
            }

            throw WebcamErrors.Create(WebcamErrorKind.UnsupportedFormat, Device);
        }

        private static nint CreateDeviceSource(WebcamDevice device)
        {
            nint attributes = 0;

            try
            {
                ThrowIfFailed(MFCreateAttributes(out attributes, 2), "MFCreateAttributes");
                ThrowIfFailed(MfAttributes.SetGuid(attributes, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID), "Setting the source type");
                ThrowIfFailed(MfAttributes.SetString(attributes, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, device.SymbolicLink), "Setting the symbolic link");
                ThrowIfFailed(MFCreateDeviceSource(attributes, out nint source), "Opening the capture device");
                return source;
            }
            finally
            {
                ComUnknown.Release(ref attributes);
            }
        }

        private static nint CreateSourceReader(nint source)
        {
            nint attributes = 0;

            try
            {
                ThrowIfFailed(MFCreateAttributes(out attributes, 2), "MFCreateAttributes");

                // Lets the reader insert decoders and the video processor so any camera format can become RGB32.
                ThrowIfFailed(MfAttributes.SetUInt32(attributes, MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING, 1), "Enabling video processing");

                // A live preview favors latency over smoothing buffers.
                ThrowIfFailed(MfAttributes.SetUInt32(attributes, MF_LOW_LATENCY, 1), "Enabling low latency");

                ThrowIfFailed(MFCreateSourceReaderFromMediaSource(source, attributes, out nint reader), "Creating the source reader");
                return reader;
            }
            finally
            {
                ComUnknown.Release(ref attributes);
            }
        }

        private static List<NativeVideoMode> EnumerateModes(nint reader)
        {
            var modes = new List<NativeVideoMode>();

            for (int index = 0; index < MaxNativeMediaTypes; index++)
            {
                int hr = MfSourceReader.GetNativeMediaType(reader, FirstVideoStream, (uint)index, out nint mediaType);

                if (hr == MF_E_NO_MORE_TYPES)
                {
                    break;
                }

                ThrowIfFailed(hr, "Enumerating video formats");

                try
                {
                    if (MfAttributes.GetGuid(mediaType, MF_MT_MAJOR_TYPE, out Guid majorType) < 0 || majorType != MFMediaType_Video
                        || MfAttributes.GetGuid(mediaType, MF_MT_SUBTYPE, out Guid subtype) < 0
                        || MfAttributes.GetUInt64(mediaType, MF_MT_FRAME_SIZE, out ulong frameSize) < 0)
                    {
                        continue;
                    }

                    MfAttributes.GetUInt64(mediaType, MF_MT_FRAME_RATE, out ulong frameRate);
                    (uint width, uint height) = Unpack(frameSize);
                    (uint numerator, uint denominator) = Unpack(frameRate);

                    modes.Add(new NativeVideoMode(index, subtype, (int)width, (int)height, numerator, denominator));
                }
                finally
                {
                    ComUnknown.Release(ref mediaType);
                }
            }

            return modes;
        }

        /// <summary>
        /// Pins the device to <paramref name="mode"/> and requests RGB32 output of the same size.
        /// Setting the native type first stops the reader from silently picking a different device
        /// format (for example a 5 fps uncompressed mode instead of 30 fps MJPG).
        /// </summary>
        private bool TrySelectMode(NativeVideoMode mode)
        {
            nint nativeType = 0;
            nint outputType = 0;

            try
            {
                if (MfSourceReader.GetNativeMediaType(_reader, FirstVideoStream, (uint)mode.Index, out nativeType) < 0
                    || MfSourceReader.SetCurrentMediaType(_reader, FirstVideoStream, nativeType) < 0)
                {
                    return false;
                }

                if (mode.Subtype == MFVideoFormat_RGB32)
                {
                    return true;
                }

                return MFCreateMediaType(out outputType) >= 0
                    && MfAttributes.SetGuid(outputType, MF_MT_MAJOR_TYPE, MFMediaType_Video) >= 0
                    && MfAttributes.SetGuid(outputType, MF_MT_SUBTYPE, MFVideoFormat_RGB32) >= 0
                    && MfAttributes.SetUInt64(outputType, MF_MT_FRAME_SIZE, Pack((uint)mode.Width, (uint)mode.Height)) >= 0
                    && MfAttributes.SetUInt64(outputType, MF_MT_FRAME_RATE, Pack(mode.FrameRateNumerator, mode.FrameRateDenominator)) >= 0
                    && MfSourceReader.SetCurrentMediaType(_reader, FirstVideoStream, outputType) >= 0;
            }
            finally
            {
                ComUnknown.Release(ref nativeType);
                ComUnknown.Release(ref outputType);
            }
        }

        /// <summary>
        /// Reads the negotiated output size and stride.  Called after configuration and whenever the
        /// reader reports that the current media type changed.
        /// </summary>
        private void ReadCurrentFormat()
        {
            ThrowIfFailed(MfSourceReader.GetCurrentMediaType(_reader, FirstVideoStream, out nint mediaType), "Reading the output format");

            try
            {
                if (MfAttributes.GetGuid(mediaType, MF_MT_SUBTYPE, out Guid subtype) < 0 || subtype != MFVideoFormat_RGB32)
                {
                    throw WebcamErrors.Create(WebcamErrorKind.UnsupportedFormat, Device);
                }

                ThrowIfFailed(MfAttributes.GetUInt64(mediaType, MF_MT_FRAME_SIZE, out ulong frameSize), "Reading the frame size");
                (uint width, uint height) = Unpack(frameSize);

                if (width == 0 || height == 0 || width > 16384 || height > 16384)
                {
                    throw WebcamErrors.Create(WebcamErrorKind.UnsupportedFormat, Device);
                }

                _width = (int)width;
                _height = (int)height;

                // Only used when a buffer does not expose IMF2DBuffer.  Absent means top-down, tightly packed.
                _defaultStride = MfAttributes.GetUInt32(mediaType, MF_MT_DEFAULT_STRIDE, out uint stride) >= 0 ? (int)stride : _width * 4;
            }
            finally
            {
                ComUnknown.Release(ref mediaType);
            }
        }

        /// <summary>
        /// The frame loop.  Allocation free in steady state: no LINQ, no per-frame arrays, no RCWs.
        /// </summary>
        private void ReadLoop()
        {
            int consecutiveFailures = 0;

            while (!_stopRequested)
            {
                int hr = MfSourceReader.ReadSample(_reader, FirstVideoStream, out uint flags, out long timestamp, out nint sample);

                try
                {
                    if (_stopRequested)
                    {
                        break;
                    }

                    ThrowIfFailed(hr, "Reading a video frame");

                    if ((flags & ReaderFlagError) != 0)
                    {
                        throw WebcamErrors.Create(WebcamErrorKind.FrameAcquisitionFailed, Device);
                    }

                    if ((flags & ReaderFlagEndOfStream) != 0)
                    {
                        throw WebcamErrors.Create(WebcamErrorKind.DeviceDisconnected, Device);
                    }

                    if ((flags & ReaderFlagCurrentMediaTypeChanged) != 0)
                    {
                        ReadCurrentFormat();
                    }

                    // Stream ticks and gaps carry no sample.
                    if (sample == 0)
                    {
                        continue;
                    }

                    if (TryCopySample(sample, timestamp))
                    {
                        consecutiveFailures = 0;

                        if (_exchange.Publish())
                        {
                            _frameReady();
                        }
                    }
                    else if (++consecutiveFailures >= MaxConsecutiveFrameFailures)
                    {
                        // Isolated bad frames are skipped; a persistent failure ends the session.
                        throw WebcamErrors.Create(WebcamErrorKind.FrameAcquisitionFailed, Device, Marshal.GetExceptionForHR(_lastFrameError));
                    }
                }
                finally
                {
                    ComUnknown.Release(ref sample);
                }
            }
        }

        private bool TryCopySample(nint sample, long timestamp)
        {
            nint buffer = 0;
            nint buffer2D = 0;

            try
            {
                int hr = MfSample.ConvertToContiguousBuffer(sample, out buffer);

                if (hr >= 0)
                {
                    WebcamFrame frame = _exchange.WriteFrame;
                    frame.EnsureFormat(_width, _height);

                    // IMF2DBuffer reports the real pitch and orientation; fall back to a plain lock otherwise.
                    hr = ComUnknown.QueryInterface(buffer, IID_IMF2DBuffer, out buffer2D) >= 0
                        ? Copy2D(buffer2D, frame)
                        : CopyContiguous(buffer, frame);

                    if (hr >= 0)
                    {
                        frame.Timestamp = timestamp;
                        frame.FrameNumber = ++_frameNumber;
                        return true;
                    }
                }

                _lastFrameError = hr;
                return false;
            }
            finally
            {
                ComUnknown.Release(ref buffer2D);
                ComUnknown.Release(ref buffer);
            }
        }

        private static unsafe int Copy2D(nint buffer2D, WebcamFrame frame)
        {
            int hr = Mf2DBuffer.Lock2D(buffer2D, out byte* scanline0, out int pitch);

            if (hr < 0)
            {
                return hr;
            }

            try
            {
                if (Math.Abs((long)pitch) < frame.Stride)
                {
                    return MF_E_INVALIDMEDIATYPE;
                }

                CopyRows(scanline0, pitch, frame);
                return S_OK;
            }
            finally
            {
                Mf2DBuffer.Unlock2D(buffer2D);
            }
        }

        private unsafe int CopyContiguous(nint buffer, WebcamFrame frame)
        {
            int hr = MfMediaBuffer.Lock(buffer, out byte* data, out uint length);

            if (hr < 0)
            {
                return hr;
            }

            try
            {
                long pitch = Math.Abs((long)_defaultStride);
                long required = pitch * (frame.Height - 1) + frame.Stride;

                if (pitch < frame.Stride || length < required)
                {
                    return MF_E_INVALIDMEDIATYPE;
                }

                // A negative default stride means bottom-up rows: the first scan line is stored last.
                byte* scanline0 = _defaultStride < 0 ? data + pitch * (frame.Height - 1) : data;
                CopyRows(scanline0, _defaultStride, frame);
                return S_OK;
            }
            finally
            {
                MfMediaBuffer.Unlock(buffer);
            }
        }

        /// <summary>
        /// Copies the frame into the staging buffer as tightly packed top-down rows.
        /// </summary>
        private static unsafe void CopyRows(byte* scanline0, long pitch, WebcamFrame frame)
        {
            int rowBytes = frame.Stride;
            int height = frame.Height;

            fixed (byte* destination = frame.Pixels)
            {
                if (pitch == rowBytes)
                {
                    long total = (long)rowBytes * height;
                    Buffer.MemoryCopy(scanline0, destination, frame.Pixels.Length, total);
                    return;
                }

                for (int y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(scanline0 + y * pitch, destination + (long)y * rowBytes, rowBytes, rowBytes);
                }
            }
        }

        private WebcamException ClassifyStreamingFailure(Exception exception)
        {
            WebcamException translated = WebcamErrors.Translate(exception, Device, WebcamErrorKind.FrameAcquisitionFailed);

            // Unplugging a camera surfaces as a variety of generic errors; confirm by checking whether the device still exists.
            if (translated.Kind is WebcamErrorKind.FrameAcquisitionFailed or WebcamErrorKind.Unknown && !WebcamDeviceManager.IsConnected(Device))
            {
                return WebcamErrors.Create(WebcamErrorKind.DeviceDisconnected, Device, exception);
            }

            return translated;
        }

        private void ForceShutdownSource()
        {
            lock (_sourceGate)
            {
                if (_source != 0)
                {
                    MfMediaSource.Shutdown(_source);
                }
            }
        }

        /// <summary>
        /// Releases the reader and the source.  Worker thread only; idempotent.
        /// </summary>
        private void ReleaseNativeObjects()
        {
            // Releasing the reader shuts down a source it was created from; the explicit Shutdown below
            // is a harmless no-op then and guarantees the device handle is closed either way.
            ComUnknown.Release(ref _reader);

            lock (_sourceGate)
            {
                if (_source != 0)
                {
                    MfMediaSource.Shutdown(_source);
                    ComUnknown.Release(ref _source);
                }
            }
        }
    }
}
