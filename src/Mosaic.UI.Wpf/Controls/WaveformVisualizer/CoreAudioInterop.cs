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
    /// Provides safe release behavior for runtime callable COM wrappers.
    /// </summary>
    internal static class CoreAudioCom
    {
        /// <summary>
        /// Releases the specified object when it is represented by a runtime callable wrapper.
        /// </summary>
        /// <param name="instance">The possible runtime callable wrapper to release.</param>
        public static void Release(object? instance)
        {
            if (instance is not null && Marshal.IsComObject(instance))
            {
                Marshal.ReleaseComObject(instance);
            }
        }
    }

    /// <summary>
    /// Specifies the direction of audio data flow.
    /// </summary>
    internal enum EDataFlow { Render, Capture, All }

    /// <summary>
    /// Specifies the role assigned to an audio endpoint.
    /// </summary>
    internal enum ERole { Console, Multimedia, Communications }

    /// <summary>
    /// Specifies whether an audio client shares an endpoint.
    /// </summary>
    internal enum AudioClientShareMode { Shared, Exclusive }

    /// <summary>
    /// Specifies options used to initialize an audio client stream.
    /// </summary>
    [Flags]
    internal enum AudioClientStreamFlags : uint
    {
        /// <summary>
        /// Uses no optional audio stream behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Captures the audio stream played by a render endpoint.
        /// </summary>
        Loopback = 0x00020000
    }

    /// <summary>
    /// Specifies the COM server contexts used to create an object.
    /// </summary>
    internal enum ClsCtx : uint
    {
        /// <summary>
        /// Enables all supported COM server contexts.
        /// </summary>
        All = 0x17
    }

    /// <summary>
    /// Describes the base format of waveform audio data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct WaveFormatEx
    {
        /// <summary>
        /// Stores the waveform audio format type.
        /// </summary>
        public ushort FormatTag;
        /// <summary>
        /// Stores the number of audio channels.
        /// </summary>
        public ushort Channels;
        /// <summary>
        /// Stores the sample rate in samples per second.
        /// </summary>
        public uint SamplesPerSec;
        /// <summary>
        /// Stores the required average data transfer rate.
        /// </summary>
        public uint AvgBytesPerSec;
        /// <summary>
        /// Stores the block alignment in bytes.
        /// </summary>
        public ushort BlockAlign;
        /// <summary>
        /// Stores the number of bits per sample.
        /// </summary>
        public ushort BitsPerSample;
        /// <summary>
        /// Stores the size of additional format information.
        /// </summary>
        public ushort ExtraSize;
    }

    /// <summary>
    /// Describes an extensible waveform audio format.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct WaveFormatExtensible
    {
        /// <summary>
        /// Stores the base waveform format.
        /// </summary>
        public WaveFormatEx Format;
        /// <summary>
        /// Stores the number of valid bits in each sample.
        /// </summary>
        public ushort ValidBitsPerSample;
        /// <summary>
        /// Stores the assignment of channels to speaker positions.
        /// </summary>
        public uint ChannelMask;
        /// <summary>
        /// Stores the data format subtype.
        /// </summary>
        public Guid SubFormat;
    }

    /// <summary>
    /// Creates Windows multimedia device enumerators.
    /// </summary>
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator;

    /// <summary>
    /// Enumerates and monitors Windows multimedia audio devices.
    /// </summary>
    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        /// <summary>
        /// Enumerates audio endpoint devices.
        /// </summary>
        void EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IMMDeviceCollection devices);
        /// <summary>
        /// Gets the default audio endpoint for a data flow and role.
        /// </summary>
        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
        /// <summary>
        /// Gets an audio endpoint by identifier.
        /// </summary>
        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

        /// <summary>
        /// Registers an endpoint notification callback.
        /// </summary>
        [PreserveSig]
        int RegisterEndpointNotificationCallback(IMMNotificationClient client);

        /// <summary>
        /// Unregisters an endpoint notification callback.
        /// </summary>
        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }

    /// <summary>
    /// Enumerates Windows multimedia audio endpoint devices.
    /// </summary>
    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceCollection
    {
        /// <summary>
        /// Gets the number of devices in the collection.
        /// </summary>
        void GetCount(out uint count);

        /// <summary>
        /// Gets the device at the specified index.
        /// </summary>
        void Item(uint deviceIndex, out IMMDevice device);
    }

    /// <summary>
    /// Represents a Windows multimedia audio endpoint device.
    /// </summary>
    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        /// <summary>
        /// Creates an instance of an interface exposed by the device.
        /// </summary>
        void Activate(
            ref Guid iid,
            ClsCtx clsCtx,
            IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object instance);

        /// <summary>
        /// Opens the property store for the device.
        /// </summary>
        void OpenPropertyStore(uint storageMode, out IPropertyStore properties);

        /// <summary>
        /// Gets the endpoint device identifier.
        /// </summary>
        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    }

    /// <summary>
    /// Reads properties associated with a Windows audio endpoint.
    /// </summary>
    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        /// <summary>
        /// Gets the number of properties in the store.
        /// </summary>
        void GetCount(out uint propertyCount);

        /// <summary>
        /// Gets the key at the specified index.
        /// </summary>
        void GetAt(uint propertyIndex, out PropertyKey key);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        void GetValue(ref PropertyKey key, out PropVariant value);

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        void SetValue(ref PropertyKey key, ref PropVariant value);

        /// <summary>
        /// Saves changes to the property store.
        /// </summary>
        void Commit();
    }

    /// <summary>
    /// Configures and controls a Windows audio client stream.
    /// </summary>
    [ComImport]
    [Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClient
    {
        /// <summary>
        /// Initializes the audio stream.
        /// </summary>
        void Initialize(
            AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long bufferDuration,
            long periodicity,
            IntPtr format,
            ref Guid audioSessionGuid);

        /// <summary>
        /// Gets the endpoint buffer size.
        /// </summary>
        void GetBufferSize(out uint bufferFrames);
        /// <summary>
        /// Gets the stream latency.
        /// </summary>
        void GetStreamLatency(out long latency);
        /// <summary>
        /// Gets the amount of queued audio data.
        /// </summary>
        void GetCurrentPadding(out uint paddingFrames);
        /// <summary>
        /// Determines whether the endpoint supports an audio format.
        /// </summary>
        void IsFormatSupported(AudioClientShareMode shareMode, IntPtr format, out IntPtr closestMatch);
        /// <summary>
        /// Gets the endpoint mix format.
        /// </summary>
        void GetMixFormat(out IntPtr deviceFormat);
        /// <summary>
        /// Gets the default and minimum device periods.
        /// </summary>
        void GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);
        /// <summary>
        /// Starts the audio stream.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops the audio stream.
        /// </summary>
        void Stop();
        /// <summary>
        /// Resets the audio stream.
        /// </summary>
        void Reset();
        /// <summary>
        /// Assigns an event handle to the audio stream.
        /// </summary>
        void SetEventHandle(IntPtr eventHandle);
        /// <summary>
        /// Gets a service exposed by the audio client.
        /// </summary>
        void GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }

    /// <summary>
    /// Reads captured audio packets from an audio client.
    /// </summary>
    [ComImport]
    [Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioCaptureClient
    {
        /// <summary>
        /// Gets the next captured audio packet.
        /// </summary>
        void GetBuffer(
            out IntPtr data,
            out uint framesToRead,
            out uint flags,
            out long devicePosition,
            out long qpcPosition);

        /// <summary>
        /// Releases a captured audio packet.
        /// </summary>
        void ReleaseBuffer(uint framesRead);
        /// <summary>
        /// Gets the number of frames in the next captured packet.
        /// </summary>
        void GetNextPacketSize(out uint packetFrames);
    }

    /// <summary>
    /// Receives notifications when audio endpoint devices change.
    /// </summary>
    [ComImport]
    [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMNotificationClient
    {
        /// <summary>
        /// Handles a change to an endpoint device state.
        /// </summary>
        void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string? deviceId, uint newState);
        /// <summary>
        /// Handles the addition of an endpoint device.
        /// </summary>
        void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string? deviceId);
        /// <summary>
        /// Handles the removal of an endpoint device.
        /// </summary>
        void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string? deviceId);
        /// <summary>
        /// Handles a change to the default endpoint device.
        /// </summary>
        void OnDefaultDeviceChanged(EDataFlow flow, ERole role, [MarshalAs(UnmanagedType.LPWStr)] string? defaultDeviceId);
        /// <summary>
        /// Handles a change to an endpoint device property.
        /// </summary>
        void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string? deviceId, PropertyKey key);
    }

    /// <summary>
    /// Identifies a property in a Windows property store.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyKey
    {
        /// <summary>
        /// Stores the property format identifier.
        /// </summary>
        public Guid FormatId;
        /// <summary>
        /// Stores the property identifier.
        /// </summary>
        public uint PropertyId;
    }

    /// <summary>
    /// Stores a value returned by a Windows property store.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct PropVariant
    {
        /// <summary>
        /// Stores the variant type.
        /// </summary>
        [FieldOffset(0)]
        public ushort VariantType;

        /// <summary>
        /// Stores a pointer value.
        /// </summary>
        [FieldOffset(8)]
        public IntPtr PointerValue;

        /// <summary>
        /// Gets the string represented by this value.
        /// </summary>
        /// <returns>The represented string, or <see langword="null" /> when the value is not a string.</returns>
        public readonly string? GetString()
        {
            const ushort StringVariantType = 31;
            return VariantType == StringVariantType && PointerValue != IntPtr.Zero
                ? Marshal.PtrToStringUni(PointerValue)
                : null;
        }

        /// <summary>
        /// Releases memory owned by this value.
        /// </summary>
        public void Clear()
        {
            PropVariantClear(ref this);
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);
    }
}
