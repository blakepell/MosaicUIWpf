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
    /// Reference counts Media Foundation startup so any number of cameras (and device enumerations)
    /// can coexist: the first lease calls MFStartup and the last lease released calls MFShutdown.
    /// </summary>
    internal static class MediaFoundationLifetime
    {
        private static readonly Lock Gate = new();
        private static int _referenceCount;

        /// <summary>
        /// Acquires a Media Foundation lease.  Dispose the returned object exactly when the caller no
        /// longer holds any Media Foundation object.
        /// </summary>
        /// <exception cref="WebcamException">Media Foundation is unavailable or failed to start.</exception>
        public static IDisposable Acquire()
        {
            lock (Gate)
            {
                if (_referenceCount == 0)
                {
                    int hr;

                    try
                    {
                        hr = MediaFoundationInterop.MFStartup(MediaFoundationInterop.MfVersion, MediaFoundationInterop.MfStartupLite);
                    }
                    catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
                    {
                        // Windows "N" editions ship without Media Foundation unless the Media Feature Pack is installed.
                        throw new WebcamException(WebcamErrorKind.InitializationFailed,
                            "Windows Media Foundation is not available on this system. On Windows N editions install the Media Feature Pack.", null, ex);
                    }

                    if (hr < 0)
                    {
                        throw new WebcamException(WebcamErrorKind.InitializationFailed,
                            $"Windows Media Foundation failed to start (HRESULT 0x{hr:X8}).", null, Marshal.GetExceptionForHR(hr));
                    }
                }

                _referenceCount++;
            }

            return new Lease();
        }

        private static void Release()
        {
            lock (Gate)
            {
                if (_referenceCount == 0)
                {
                    Debug.Fail("Media Foundation lease released more times than acquired.");
                    return;
                }

                if (--_referenceCount == 0)
                {
                    MediaFoundationInterop.MFShutdown();
                }
            }
        }

        private sealed class Lease : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                // Guard against double dispose releasing someone else's reference.
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    Release();
                }
            }
        }
    }
}
