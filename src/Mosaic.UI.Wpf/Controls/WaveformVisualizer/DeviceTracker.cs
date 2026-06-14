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
    /// Tracks the default console render device and redirects loopback capture when it changes.
    /// </summary>
    /// <param name="capture">The loopback capture instance to manage.</param>
    internal sealed class DeviceTracker(AudioCapture capture) : IMMNotificationClient, IDisposable
    {
        private readonly object stateLock = new();
        private IMMDeviceEnumerator? deviceEnumerator;
        private bool isStarted;

        /// <summary>
        /// Starts monitoring the default render device and begins capture.
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
                    StartDefaultDevice();
                }
                catch
                {
                    Stop();
                    throw;
                }
            }
        }

        /// <summary>
        /// Stops monitoring the default render device and ends capture.
        /// </summary>
        public void Stop()
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
                }

                if (deviceEnumerator is not null)
                {
                    CoreAudioCom.Release(deviceEnumerator);
                    deviceEnumerator = null;
                }

                isStarted = false;
            }
        }

        /// <summary>
        /// Stops device tracking and releases the associated capture resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
            capture.Dispose();
        }

        /// <inheritdoc/>
        public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId)
        {
            if (flow != EDataFlow.Render || role != ERole.Console)
            {
                return;
            }

            lock (stateLock)
            {
                if (isStarted)
                {
                    capture.Stop();
                    StartDefaultDevice();
                }
            }
        }

        /// <inheritdoc/>
        public void OnDeviceStateChanged(string? deviceId, uint newState) { }

        /// <inheritdoc/>
        public void OnDeviceAdded(string? deviceId) { }

        /// <inheritdoc/>
        public void OnDeviceRemoved(string? deviceId) { }

        /// <inheritdoc/>
        public void OnPropertyValueChanged(string? deviceId, PropertyKey key) { }

        private void StartDefaultDevice()
        {
            deviceEnumerator!.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Console, out IMMDevice device);
            try
            {
                capture.Initialize(device, AudioClientStreamFlags.Loopback);
                capture.Start();
            }
            finally
            {
                CoreAudioCom.Release(device);
            }
        }
    }
}
