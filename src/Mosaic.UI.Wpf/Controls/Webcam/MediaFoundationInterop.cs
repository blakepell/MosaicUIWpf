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
// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Media Foundation entry points, GUIDs and constants used by the webcam implementation.
    /// </summary>
    /// <remarks>
    /// COM interfaces are called through raw vtable function pointers (see <see cref="MfAttributes"/> and
    /// siblings) rather than runtime callable wrappers.  That keeps the per-frame path allocation free,
    /// makes every release deterministic and explicit, and sidesteps apartment marshalling entirely.
    /// All values were taken from the Windows SDK headers (mfapi.h, mfidl.h, mfreadwrite.h, mferror.h).
    /// </remarks>
    internal static partial class MediaFoundationInterop
    {
        /// <summary>MF_VERSION (MF_SDK_VERSION 0x0002 &lt;&lt; 16 | MF_API_VERSION 0x0070).</summary>
        public const uint MfVersion = 0x00020070;

        /// <summary>MFSTARTUP_LITE: capture does not need the network/socket stack.</summary>
        public const uint MfStartupLite = 0x1;

        /// <summary>MF_SOURCE_READER_FIRST_VIDEO_STREAM.</summary>
        public const uint FirstVideoStream = 0xFFFFFFFC;

        /// <summary>MF_SOURCE_READER_ALL_STREAMS.</summary>
        public const uint AllStreams = 0xFFFFFFFE;

        /// <summary>MF_SOURCE_READERF_ERROR.</summary>
        public const uint ReaderFlagError = 0x1;

        /// <summary>MF_SOURCE_READERF_ENDOFSTREAM.</summary>
        public const uint ReaderFlagEndOfStream = 0x2;

        /// <summary>MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED.</summary>
        public const uint ReaderFlagCurrentMediaTypeChanged = 0x20;

        public const int S_OK = 0;
        public static readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        public static readonly int E_NOINTERFACE = unchecked((int)0x80004002);
        public static readonly int ERROR_FILE_NOT_FOUND = unchecked((int)0x80070002);
        public static readonly int ERROR_BUSY = unchecked((int)0x800700AA);
        public static readonly int ERROR_GEN_FAILURE = unchecked((int)0x8007001F);
        public static readonly int ERROR_DEVICE_NOT_CONNECTED = unchecked((int)0x8007048F);
        public static readonly int ERROR_DEVICE_REMOVED = unchecked((int)0x80070651);
        public static readonly int MF_E_INVALIDMEDIATYPE = unchecked((int)0xC00D36B4);
        public static readonly int MF_E_NO_MORE_TYPES = unchecked((int)0xC00D36B9);
        public static readonly int MF_E_NOT_FOUND = unchecked((int)0xC00D36D5);
        public static readonly int MF_E_ATTRIBUTENOTFOUND = unchecked((int)0xC00D36E6);
        public static readonly int MF_E_HW_MFT_FAILED_START_STREAMING = unchecked((int)0xC00D3704);
        public static readonly int MF_E_SHUTDOWN = unchecked((int)0xC00D3E85);
        public static readonly int MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED = unchecked((int)0xC00D3EA2);
        public static readonly int MF_E_VIDEO_RECORDING_DEVICE_PREEMPTED = unchecked((int)0xC00D3EA3);
        public static readonly int MF_E_VIDEO_DEVICE_LOCKED = unchecked((int)0xC00D4E24);
        public static readonly int MF_E_TOPO_CODEC_NOT_FOUND = unchecked((int)0xC00D5212);

        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE = new("c60ac5fe-252a-478f-a0ef-bc8fa5f7cad3");
        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID = new("8ac3587a-4ae7-42d8-99e0-0a6013eef90f");
        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME = new("60d0e559-52f8-4fa2-bbce-acdb34a8ec01");
        public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK = new("58f0aad8-22bf-4f8a-bb3d-d2c4978c6e2f");

        public static readonly Guid MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING = new("0f81da2c-b537-4672-a8b2-a681b17307a3");
        public static readonly Guid MF_LOW_LATENCY = new("9c27891a-ed7a-40e1-88e8-b22727a024ee");

        public static readonly Guid MF_MT_MAJOR_TYPE = new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        public static readonly Guid MF_MT_SUBTYPE = new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        public static readonly Guid MF_MT_FRAME_SIZE = new("1652c33d-d6b2-4012-b834-72030849a37d");
        public static readonly Guid MF_MT_FRAME_RATE = new("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");
        public static readonly Guid MF_MT_DEFAULT_STRIDE = new("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");

        public static readonly Guid MFMediaType_Video = new("73646976-0000-0010-8000-00aa00389b71");

        /// <summary>MFVideoFormat_RGB32 (D3DFMT_X8R8G8B8): B,G,R,X bytes in memory, i.e. WPF <c>Bgr32</c>.</summary>
        public static readonly Guid MFVideoFormat_RGB32 = new("00000016-0000-0010-8000-00aa00389b71");
        public static readonly Guid MFVideoFormat_NV12 = new("3231564e-0000-0010-8000-00aa00389b71");
        public static readonly Guid MFVideoFormat_YUY2 = new("32595559-0000-0010-8000-00aa00389b71");
        public static readonly Guid MFVideoFormat_MJPG = new("47504a4d-0000-0010-8000-00aa00389b71");

        public static readonly Guid IID_IMF2DBuffer = new("7dc9d5f9-9ed9-44ec-9bbf-0600bb589fbb");

        [LibraryImport("mfplat.dll")]
        public static partial int MFStartup(uint version, uint flags);

        [LibraryImport("mfplat.dll")]
        public static partial int MFShutdown();

        [LibraryImport("mfplat.dll")]
        public static partial int MFCreateAttributes(out nint attributes, uint initialSize);

        [LibraryImport("mfplat.dll")]
        public static partial int MFCreateMediaType(out nint mediaType);

        [LibraryImport("mf.dll")]
        public static partial int MFEnumDeviceSources(nint attributes, out nint activateArray, out uint count);

        [LibraryImport("mf.dll")]
        public static partial int MFCreateDeviceSource(nint attributes, out nint mediaSource);

        [LibraryImport("mfreadwrite.dll")]
        public static partial int MFCreateSourceReaderFromMediaSource(nint mediaSource, nint attributes, out nint sourceReader);

        /// <summary>
        /// Throws a <see cref="COMException"/> describing <paramref name="operation"/> when <paramref name="hr"/> is a failure code.
        /// </summary>
        public static void ThrowIfFailed(int hr, string operation)
        {
            if (hr < 0)
            {
                throw new COMException($"{operation} failed (HRESULT 0x{hr:X8}).", hr);
            }
        }

        /// <summary>
        /// Packs two 32-bit values the way MFSetAttributeSize / MFSetAttributeRatio do.
        /// </summary>
        public static ulong Pack(uint high, uint low) => ((ulong)high << 32) | low;

        /// <summary>
        /// Unpacks a value produced by MFGetAttributeSize / MFGetAttributeRatio.
        /// </summary>
        public static (uint High, uint Low) Unpack(ulong value) => ((uint)(value >> 32), (uint)value);
    }

    /// <summary>
    /// IUnknown helpers.  <see cref="Release"/> is the single place COM references are released.
    /// </summary>
    internal static unsafe class ComUnknown
    {
        /// <summary>
        /// Returns the function pointer stored in the specified vtable slot of a COM interface pointer.
        /// </summary>
        public static void* Slot(nint instance, int index) => (*(void***)instance)[index];

        /// <summary>
        /// IUnknown::QueryInterface (slot 0).
        /// </summary>
        public static int QueryInterface(nint instance, Guid iid, out nint result)
        {
            nint value;
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Slot(instance, 0))(instance, &iid, &value);
            result = hr >= 0 ? value : 0;
            return hr;
        }

        /// <summary>
        /// Releases a COM reference (IUnknown::Release, slot 2) and clears the caller's field so the same
        /// reference can never be released twice.  A zero pointer is ignored.
        /// </summary>
        public static void Release(ref nint instance)
        {
            nint pointer = Interlocked.Exchange(ref instance, 0);

            if (pointer != 0)
            {
                ((delegate* unmanaged[Stdcall]<nint, uint>)Slot(pointer, 2))(pointer);
            }
        }
    }

    /// <summary>
    /// IMFAttributes methods.  Usable on any interface deriving from IMFAttributes
    /// (IMFActivate, IMFMediaType, IMFSample) because they share the vtable prefix.
    /// </summary>
    internal static unsafe class MfAttributes
    {
        public static int GetUInt32(nint attributes, Guid key, out uint value)
        {
            uint result;
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, uint*, int>)ComUnknown.Slot(attributes, 7))(attributes, &key, &result);
            value = hr >= 0 ? result : 0;
            return hr;
        }

        public static int GetUInt64(nint attributes, Guid key, out ulong value)
        {
            ulong result;
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, ulong*, int>)ComUnknown.Slot(attributes, 8))(attributes, &key, &result);
            value = hr >= 0 ? result : 0;
            return hr;
        }

        public static int GetGuid(nint attributes, Guid key, out Guid value)
        {
            Guid result;
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, int>)ComUnknown.Slot(attributes, 10))(attributes, &key, &result);
            value = hr >= 0 ? result : Guid.Empty;
            return hr;
        }

        /// <summary>
        /// IMFAttributes::GetAllocatedString; the native string is copied and freed before returning.
        /// </summary>
        public static int GetString(nint attributes, Guid key, out string? value)
        {
            char* text = null;
            uint length;
            int hr = ((delegate* unmanaged[Stdcall]<nint, Guid*, char**, uint*, int>)ComUnknown.Slot(attributes, 13))(attributes, &key, &text, &length);

            try
            {
                value = hr >= 0 && text != null ? new string(text, 0, (int)length) : null;
            }
            finally
            {
                if (text != null)
                {
                    Marshal.FreeCoTaskMem((nint)text);
                }
            }

            return hr;
        }

        public static int SetUInt32(nint attributes, Guid key, uint value)
        {
            return ((delegate* unmanaged[Stdcall]<nint, Guid*, uint, int>)ComUnknown.Slot(attributes, 21))(attributes, &key, value);
        }

        public static int SetUInt64(nint attributes, Guid key, ulong value)
        {
            return ((delegate* unmanaged[Stdcall]<nint, Guid*, ulong, int>)ComUnknown.Slot(attributes, 22))(attributes, &key, value);
        }

        public static int SetGuid(nint attributes, Guid key, Guid value)
        {
            return ((delegate* unmanaged[Stdcall]<nint, Guid*, Guid*, int>)ComUnknown.Slot(attributes, 24))(attributes, &key, &value);
        }

        public static int SetString(nint attributes, Guid key, string value)
        {
            fixed (char* text = value)
            {
                return ((delegate* unmanaged[Stdcall]<nint, Guid*, char*, int>)ComUnknown.Slot(attributes, 25))(attributes, &key, text);
            }
        }
    }

    /// <summary>
    /// IMFMediaSource methods (IMFMediaEventGenerator occupies slots 3-6).
    /// </summary>
    internal static unsafe class MfMediaSource
    {
        /// <summary>IMFMediaSource::Shutdown (slot 12).  Safe to call more than once.</summary>
        public static int Shutdown(nint source)
        {
            return ((delegate* unmanaged[Stdcall]<nint, int>)ComUnknown.Slot(source, 12))(source);
        }
    }

    /// <summary>
    /// IMFSourceReader methods.
    /// </summary>
    internal static unsafe class MfSourceReader
    {
        public static int SetStreamSelection(nint reader, uint streamIndex, bool selected)
        {
            return ((delegate* unmanaged[Stdcall]<nint, uint, int, int>)ComUnknown.Slot(reader, 4))(reader, streamIndex, selected ? 1 : 0);
        }

        public static int GetNativeMediaType(nint reader, uint streamIndex, uint mediaTypeIndex, out nint mediaType)
        {
            nint result;
            int hr = ((delegate* unmanaged[Stdcall]<nint, uint, uint, nint*, int>)ComUnknown.Slot(reader, 5))(reader, streamIndex, mediaTypeIndex, &result);
            mediaType = hr >= 0 ? result : 0;
            return hr;
        }

        public static int GetCurrentMediaType(nint reader, uint streamIndex, out nint mediaType)
        {
            nint result;
            int hr = ((delegate* unmanaged[Stdcall]<nint, uint, nint*, int>)ComUnknown.Slot(reader, 6))(reader, streamIndex, &result);
            mediaType = hr >= 0 ? result : 0;
            return hr;
        }

        public static int SetCurrentMediaType(nint reader, uint streamIndex, nint mediaType)
        {
            return ((delegate* unmanaged[Stdcall]<nint, uint, uint*, nint, int>)ComUnknown.Slot(reader, 7))(reader, streamIndex, null, mediaType);
        }

        /// <summary>
        /// IMFSourceReader::ReadSample (slot 9) in synchronous mode.  Blocks until a sample, a stream
        /// event or an error is available.  <paramref name="sample"/> may be zero (e.g. stream ticks).
        /// </summary>
        public static int ReadSample(nint reader, uint streamIndex, out uint streamFlags, out long timestamp, out nint sample)
        {
            uint actualStreamIndex;
            uint flags;
            long time;
            nint result = 0;
            int hr = ((delegate* unmanaged[Stdcall]<nint, uint, uint, uint*, uint*, long*, nint*, int>)ComUnknown.Slot(reader, 9))(
                reader, streamIndex, 0, &actualStreamIndex, &flags, &time, &result);

            streamFlags = flags;
            timestamp = time;
            sample = result;
            return hr;
        }
    }

    /// <summary>
    /// IMFSample methods.
    /// </summary>
    internal static unsafe class MfSample
    {
        /// <summary>
        /// IMFSample::ConvertToContiguousBuffer (slot 41).  For single-buffer video samples this returns
        /// the existing buffer (AddRef'd) without copying.
        /// </summary>
        public static int ConvertToContiguousBuffer(nint sample, out nint buffer)
        {
            nint result;
            int hr = ((delegate* unmanaged[Stdcall]<nint, nint*, int>)ComUnknown.Slot(sample, 41))(sample, &result);
            buffer = hr >= 0 ? result : 0;
            return hr;
        }
    }

    /// <summary>
    /// IMFMediaBuffer methods.  Every successful <see cref="Lock"/> must be paired with <see cref="Unlock"/>.
    /// </summary>
    internal static unsafe class MfMediaBuffer
    {
        public static int Lock(nint buffer, out byte* data, out uint currentLength)
        {
            byte* pointer;
            uint maxLength;
            uint length;
            int hr = ((delegate* unmanaged[Stdcall]<nint, byte**, uint*, uint*, int>)ComUnknown.Slot(buffer, 3))(buffer, &pointer, &maxLength, &length);
            data = hr >= 0 ? pointer : null;
            currentLength = hr >= 0 ? length : 0;
            return hr;
        }

        public static int Unlock(nint buffer)
        {
            return ((delegate* unmanaged[Stdcall]<nint, int>)ComUnknown.Slot(buffer, 4))(buffer);
        }
    }

    /// <summary>
    /// IMF2DBuffer methods.  Every successful <see cref="Lock2D"/> must be paired with <see cref="Unlock2D"/>.
    /// </summary>
    internal static unsafe class Mf2DBuffer
    {
        /// <summary>
        /// IMF2DBuffer::Lock2D (slot 3).  Returns the first scan line and the pitch, which is negative
        /// for bottom-up images, so orientation is handled without consulting MF_MT_DEFAULT_STRIDE.
        /// </summary>
        public static int Lock2D(nint buffer, out byte* scanline0, out int pitch)
        {
            byte* pointer;
            int stride;
            int hr = ((delegate* unmanaged[Stdcall]<nint, byte**, int*, int>)ComUnknown.Slot(buffer, 3))(buffer, &pointer, &stride);
            scanline0 = hr >= 0 ? pointer : null;
            pitch = hr >= 0 ? stride : 0;
            return hr;
        }

        public static int Unlock2D(nint buffer)
        {
            return ((delegate* unmanaged[Stdcall]<nint, int>)ComUnknown.Slot(buffer, 4))(buffer);
        }
    }
}
