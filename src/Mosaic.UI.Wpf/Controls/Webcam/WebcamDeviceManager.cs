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

using System.Threading.Tasks;
using Mosaic.UI.Wpf.Common;
using static Mosaic.UI.Wpf.Controls.MediaFoundationInterop;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Enumerates the video capture devices available to the <see cref="Webcam"/> control.
    /// </summary>
    public static class WebcamDeviceManager
    {
        /// <summary>
        /// Gets the video capture devices currently connected to the system.
        /// </summary>
        /// <remarks>
        /// Enumeration is synchronous and inexpensive (it does not open any device), so it is safe to
        /// call from the UI thread.  In the XAML designer an empty list is returned.
        /// </remarks>
        /// <returns>The available devices, in the order reported by Windows.</returns>
        /// <exception cref="WebcamException">Media Foundation is unavailable or enumeration failed.</exception>
        public static IReadOnlyList<WebcamDevice> GetDevices()
        {
            if (DesignerHelper.IsInDesignMode)
            {
                return [];
            }

            using IDisposable lease = MediaFoundationLifetime.Acquire();

            try
            {
                return EnumerateDevices();
            }
            catch (Exception ex)
            {
                throw WebcamErrors.Translate(ex, null, WebcamErrorKind.InitializationFailed);
            }
        }

        /// <summary>
        /// Gets the capture modes supported by <paramref name="device"/>, largest first.
        /// </summary>
        /// <remarks>
        /// Querying modes has to open the device briefly (it does not start streaming), which can take a
        /// noticeable fraction of a second, so the work runs on a background thread.  While a
        /// <see cref="Webcam"/> is running, <see cref="Webcam.AvailableResolutions"/> already holds this list.
        /// </remarks>
        /// <param name="device">The device to query.</param>
        /// <returns>The distinct supported resolutions and frame rates.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
        /// <exception cref="WebcamException">The device could not be opened.</exception>
        public static Task<IReadOnlyList<WebcamResolution>> GetResolutionsAsync(WebcamDevice device)
        {
            ArgumentNullException.ThrowIfNull(device);

            if (DesignerHelper.IsInDesignMode)
            {
                return Task.FromResult<IReadOnlyList<WebcamResolution>>([]);
            }

            return Task.Run(() => MediaFoundationCamera.QueryResolutions(device));
        }

        /// <summary>
        /// Returns whether <paramref name="device"/> is still present.  Used to confirm disconnects.
        /// </summary>
        internal static bool IsConnected(WebcamDevice device)
        {
            try
            {
                foreach (var candidate in GetDevices())
                {
                    if (candidate.Equals(device))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (WebcamException)
            {
                // If enumeration itself fails we cannot tell; assume present so the original error is reported.
                return true;
            }
        }

        private static unsafe List<WebcamDevice> EnumerateDevices()
        {
            nint attributes = 0;
            nint activateArray = 0;
            uint count = 0;

            try
            {
                ThrowIfFailed(MFCreateAttributes(out attributes, 1), "MFCreateAttributes");
                ThrowIfFailed(MfAttributes.SetGuid(attributes, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID), "Setting the source type");
                ThrowIfFailed(MFEnumDeviceSources(attributes, out activateArray, out count), "MFEnumDeviceSources");

                var devices = new List<WebcamDevice>((int)count);
                nint* activates = (nint*)activateArray;

                for (int i = 0; i < count; i++)
                {
                    if (MfAttributes.GetString(activates[i], MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, out string? symbolicLink) < 0
                        || string.IsNullOrWhiteSpace(symbolicLink))
                    {
                        continue;
                    }

                    MfAttributes.GetString(activates[i], MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME, out string? name);
                    devices.Add(new WebcamDevice(name ?? symbolicLink, symbolicLink));
                }

                return devices;
            }
            finally
            {
                // Every IMFActivate in the array is released exactly once, even if reading one of them threw.
                if (activateArray != 0)
                {
                    nint* activates = (nint*)activateArray;

                    for (int i = 0; i < count; i++)
                    {
                        ComUnknown.Release(ref activates[i]);
                    }

                    Marshal.FreeCoTaskMem(activateArray);
                }

                ComUnknown.Release(ref attributes);
            }
        }
    }
}
