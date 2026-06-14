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
    /// Enumerates active Windows audio input endpoints.
    /// </summary>
    internal static class AudioInputDeviceEnumerator
    {
        private const uint ActiveDeviceState = 0x1;
        private const uint ReadPropertyStore = 0;
        private static PropertyKey FriendlyNameKey = new()
        {
            FormatId = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
            PropertyId = 14
        };

        /// <summary>
        /// Gets the active input endpoints, including an entry that follows the default endpoint.
        /// </summary>
        /// <returns>A list of selectable audio input endpoints.</returns>
        public static List<AudioInputDevice> GetDevices()
        {
            List<AudioInputDevice> devices = [new(null, "Default input device", true)];
            IMMDeviceEnumerator enumerator = CoreAudioCom.CreateDeviceEnumerator();
            try
            {
                enumerator.EnumAudioEndpoints(EDataFlow.Capture, ActiveDeviceState, out IMMDeviceCollection collection);
                try
                {
                    collection.GetCount(out uint count);
                    for (uint index = 0; index < count; index++)
                    {
                        collection.Item(index, out IMMDevice device);
                        try
                        {
                            Marshal.ThrowExceptionForHR(device.GetId(out string id));
                            devices.Add(new AudioInputDevice(id, GetFriendlyName(device) ?? id, false));
                        }
                        finally
                        {
                            CoreAudioCom.Release(device);
                        }
                    }
                }
                finally
                {
                    CoreAudioCom.Release(collection);
                }
            }
            finally
            {
                CoreAudioCom.Release(enumerator);
            }

            return devices;
        }

        private static string? GetFriendlyName(IMMDevice device)
        {
            device.OpenPropertyStore(ReadPropertyStore, out IPropertyStore propertyStore);
            try
            {
                propertyStore.GetValue(ref FriendlyNameKey, out PropVariant value);
                try
                {
                    return value.GetString();
                }
                finally
                {
                    value.Clear();
                }
            }
            finally
            {
                CoreAudioCom.Release(propertyStore);
            }
        }
    }
}
