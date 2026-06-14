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
    /// Tracks input endpoint changes and manages shared-mode capture from the selected endpoint.
    /// </summary>
    internal sealed class InputDeviceTracker : IMMNotificationClient, IDisposable
    {
        private const uint ActiveDeviceState = 0x1;

        private readonly AudioCapture capture;
        private readonly string? deviceId;
        private readonly Action devicesChanged;
        private readonly object stateLock = new();
        private IMMDeviceEnumerator? deviceEnumerator;
        private bool isStarted;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDeviceTracker"/> class.
        /// </summary>
        /// <param name="capture">The capture buffer that receives endpoint samples.</param>
        /// <param name="deviceId">The selected endpoint identifier, or <see langword="null" /> to follow the default endpoint.</param>
        /// <param name="devicesChanged">The callback invoked when the available endpoints change.</param>
        public InputDeviceTracker(AudioCapture capture, string? deviceId, Action devicesChanged)
        {
            this.capture = capture;
            this.deviceId = deviceId;
            this.devicesChanged = devicesChanged;
        }

        /// <summary>
        /// Starts monitoring audio input endpoints and begins shared-mode capture.
        /// </summary>
        public void Start()
        {
            lock (stateLock)
            {
                if (isStarted)
                {
                    return;
                }

                deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                try
                {
                    Marshal.ThrowExceptionForHR(deviceEnumerator.RegisterEndpointNotificationCallback(this));
                    isStarted = true;
                    StartSelectedDevice();
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }
        }

        /// <summary>
        /// Stops monitoring audio input endpoints and releases the capture resources.
        /// </summary>
        public void Dispose()
        {
            lock (stateLock)
            {
                if (isStarted)
                {
                    capture.Stop();
                    if (deviceEnumerator is not null)
                    {
                        deviceEnumerator.UnregisterEndpointNotificationCallback(this);
                    }

                    isStarted = false;
                }

                if (deviceEnumerator is not null)
                {
                    CoreAudioCom.Release(deviceEnumerator);
                    deviceEnumerator = null;
                }
            }

            capture.Dispose();
        }

        /// <inheritdoc/>
        public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId)
        {
            if (flow != EDataFlow.Capture || role != ERole.Console)
            {
                return;
            }

            devicesChanged();
            if (deviceId is null)
            {
                RestartCapture();
            }
        }

        /// <inheritdoc/>
        public void OnDeviceStateChanged(string? changedDeviceId, uint newState)
        {
            devicesChanged();
            if (deviceId == changedDeviceId)
            {
                if ((newState & ActiveDeviceState) != 0)
                {
                    RestartCapture();
                }
                else
                {
                    capture.Stop();
                }
            }
        }

        /// <inheritdoc/>
        public void OnDeviceAdded(string? addedDeviceId)
        {
            devicesChanged();
            if (deviceId == addedDeviceId)
            {
                RestartCapture();
            }
        }

        /// <inheritdoc/>
        public void OnDeviceRemoved(string? removedDeviceId)
        {
            devicesChanged();
            if (deviceId == removedDeviceId)
            {
                capture.Stop();
            }
        }

        /// <inheritdoc/>
        public void OnPropertyValueChanged(string? changedDeviceId, PropertyKey key) => devicesChanged();

        private void RestartCapture()
        {
            lock (stateLock)
            {
                if (!isStarted)
                {
                    return;
                }

                capture.Stop();
                StartSelectedDevice();
            }
        }

        private void StartSelectedDevice()
        {
            IMMDevice device;
            if (deviceId is null)
            {
                deviceEnumerator!.GetDefaultAudioEndpoint(EDataFlow.Capture, ERole.Console, out device);
            }
            else
            {
                deviceEnumerator!.GetDevice(deviceId, out device);
            }

            try
            {
                capture.Initialize(device, AudioClientStreamFlags.None);
                capture.Start();
            }
            finally
            {
                CoreAudioCom.Release(device);
            }
        }
    }
}
