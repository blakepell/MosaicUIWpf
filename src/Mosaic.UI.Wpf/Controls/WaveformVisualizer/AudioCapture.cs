/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls.WaveformVisualizer
{
    /// <summary>
    /// Captures and buffers samples from a Windows audio endpoint.
    /// </summary>
    internal sealed class AudioCapture : IDisposable
    {
        private const int MaxBufferSize = 4096;
        private const uint SilentBufferFlag = 0x2;
        private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00AA00389B71");
        private static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00AA00389B71");

        private readonly object sampleLock = new();
        private readonly Queue<double> sampleBuffer = new(MaxBufferSize);
        private IAudioClient? audioClient;
        private IAudioCaptureClient? captureClient;
        private WaveFormatEx waveFormat;
        private Thread? captureThread;
        private volatile bool isCapturing;
        private bool isFloat;
        private ushort bitsPerSample;

        /// <summary>
        /// Gets a snapshot of the most recently captured audio samples.
        /// </summary>
        /// <returns>A snapshot of normalized audio samples.</returns>
        public double[] GetRecentSamples()
        {
            lock (sampleLock)
            {
                return sampleBuffer.ToArray();
            }
        }

        /// <summary>
        /// Initializes capture for the specified audio device.
        /// </summary>
        /// <param name="device">The audio endpoint to capture.</param>
        /// <param name="streamFlags">A bitwise combination of the enumeration values that specifies the capture options.</param>
        /// <exception cref="InvalidOperationException">The audio device does not provide a mix format.</exception>
        /// <exception cref="NotSupportedException">The device uses an unsupported sample format.</exception>
        public void Initialize(IMMDevice device, AudioClientStreamFlags streamFlags)
        {
            ReleaseAudioInterfaces();
            ClearSamples();

            Guid audioClientId = typeof(IAudioClient).GUID;
            device.Activate(ref audioClientId, ClsCtx.All, IntPtr.Zero, out object client);
            audioClient = (IAudioClient)client;
            audioClient.GetMixFormat(out IntPtr formatPointer);

            if (formatPointer == IntPtr.Zero)
            {
                throw new InvalidOperationException("The default audio device did not provide a mix format.");
            }

            try
            {
                waveFormat = Marshal.PtrToStructure<WaveFormatEx>(formatPointer);
                ConfigureSampleFormat(formatPointer);

                const long bufferDuration = 1_000_000;
                Guid sessionId = Guid.Empty;
                audioClient.Initialize(
                    AudioClientShareMode.Shared,
                    streamFlags,
                    bufferDuration,
                    0,
                    formatPointer,
                    ref sessionId);
            }
            finally
            {
                Marshal.FreeCoTaskMem(formatPointer);
            }

            Guid captureClientId = typeof(IAudioCaptureClient).GUID;
            audioClient.GetService(ref captureClientId, out object captureService);
            captureClient = (IAudioCaptureClient)captureService;
        }

        /// <summary>
        /// Starts capturing audio samples on a background thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">The capture has not been initialized.</exception>
        public void Start()
        {
            if (audioClient is null || captureClient is null)
            {
                throw new InvalidOperationException("Audio capture has not been initialized.");
            }

            if (isCapturing)
            {
                return;
            }

            audioClient.Start();
            isCapturing = true;
            captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "WASAPI waveform capture"
            };
            captureThread.Start();
        }

        /// <summary>
        /// Stops capturing audio samples.
        /// </summary>
        public void Stop()
        {
            if (captureThread is null)
            {
                return;
            }

            isCapturing = false;
            captureThread.Join();
            captureThread = null;
            audioClient?.Stop();
        }

        /// <summary>
        /// Releases the capture thread and native audio interfaces.
        /// </summary>
        public void Dispose()
        {
            Stop();
            ReleaseAudioInterfaces();
        }

        private void ConfigureSampleFormat(IntPtr formatPointer)
        {
            bitsPerSample = waveFormat.BitsPerSample;

            switch (waveFormat.FormatTag)
            {
                case 0xFFFE:
                    WaveFormatExtensible extensible = Marshal.PtrToStructure<WaveFormatExtensible>(formatPointer);
                    isFloat = extensible.SubFormat == IeeeFloatSubFormat;
                    if (!isFloat && extensible.SubFormat != PcmSubFormat)
                    {
                        throw new NotSupportedException("The default audio device uses an unsupported sample format.");
                    }

                    bitsPerSample = extensible.Format.BitsPerSample;
                    break;
                case 3:
                    isFloat = true;
                    break;
                case 1:
                    isFloat = false;
                    break;
                default:
                    throw new NotSupportedException($"Audio format tag {waveFormat.FormatTag} is unsupported.");
            }

            bool supported = isFloat ? bitsPerSample is 32 or 64 : bitsPerSample is 16 or 32;
            if (!supported)
            {
                throw new NotSupportedException($"A {bitsPerSample}-bit sample format is unsupported.");
            }
        }

        private void CaptureLoop()
        {
            try
            {
                while (isCapturing)
                {
                    Thread.Sleep(10);
                    DrainAvailablePackets();
                }
            }
            catch (COMException) when (!isCapturing)
            {
                // Stopping the audio client can interrupt a pending COM call.
            }
            catch (COMException)
            {
                isCapturing = false;
            }
        }

        private void DrainAvailablePackets()
        {
            if (captureClient is null)
            {
                return;
            }

            captureClient.GetNextPacketSize(out uint packetFrames);
            while (isCapturing && packetFrames > 0)
            {
                captureClient.GetBuffer(out IntPtr data, out uint frameCount, out uint flags, out _, out _);
                try
                {
                    if ((flags & SilentBufferFlag) != 0)
                    {
                        AddSilentFrames(frameCount);
                    }
                    else if (data != IntPtr.Zero && frameCount > 0)
                    {
                        AddFrames(data, frameCount);
                    }
                }
                finally
                {
                    captureClient.ReleaseBuffer(frameCount);
                }

                captureClient.GetNextPacketSize(out packetFrames);
            }
        }

        private void AddFrames(IntPtr data, uint frameCount)
        {
            int bytesPerSample = bitsPerSample / 8;
            int byteLength = checked((int)(frameCount * waveFormat.BlockAlign));
            byte[] buffer = new byte[byteLength];
            Marshal.Copy(data, buffer, 0, byteLength);

            lock (sampleLock)
            {
                for (uint frame = 0; frame < frameCount; frame++)
                {
                    double frameTotal = 0;
                    for (ushort channel = 0; channel < waveFormat.Channels; channel++)
                    {
                        int offset = checked((int)(frame * waveFormat.BlockAlign + channel * bytesPerSample));
                        frameTotal += ReadSample(buffer, offset);
                    }

                    EnqueueSample(frameTotal / waveFormat.Channels);
                }
            }
        }

        private void AddSilentFrames(uint frameCount)
        {
            lock (sampleLock)
            {
                for (uint frame = 0; frame < frameCount; frame++)
                {
                    EnqueueSample(0);
                }
            }
        }

        private double ReadSample(byte[] buffer, int offset)
        {
            if (isFloat)
            {
                return bitsPerSample == 32
                    ? BitConverter.ToSingle(buffer, offset)
                    : BitConverter.ToDouble(buffer, offset);
            }

            return bitsPerSample == 16
                ? BitConverter.ToInt16(buffer, offset) / 32768.0
                : BitConverter.ToInt32(buffer, offset) / 2147483648.0;
        }

        private void EnqueueSample(double sample)
        {
            sampleBuffer.Enqueue(Math.Clamp(sample, -1, 1));
            if (sampleBuffer.Count > MaxBufferSize)
            {
                sampleBuffer.Dequeue();
            }
        }

        private void ClearSamples()
        {
            lock (sampleLock)
            {
                sampleBuffer.Clear();
            }
        }

        private void ReleaseAudioInterfaces()
        {
            if (captureClient is not null)
            {
                CoreAudioCom.Release(captureClient);
                captureClient = null;
            }

            if (audioClient is not null)
            {
                CoreAudioCom.Release(audioClient);
                audioClient = null;
            }
        }
    }
}
