/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowCue.Interop;

namespace WindowCue.Services
{
    /// <summary>
    /// Extracts WPF-compatible icons from windows and executable files.
    /// All returned <see cref="ImageSource"/> instances are frozen and thread-safe.
    /// </summary>
    public class IconExtractionService
    {
        private readonly BackgroundOperationRunner _work = new();

        /// <summary>Extracts a frozen icon without blocking the caller or queuing behind hung work.</summary>
        public async Task<ImageSource> ExtractIconAsync(IntPtr handle, string? path)
        {
            var result = await _work.RunAsync(() => ExtractIcon(handle, path)).ConfigureAwait(false);
            return result.Completed ? result.Value ?? _fallback : _fallback;
        }

        private static readonly BitmapSource _fallback = CreateFallback();

        private static BitmapSource CreateFallback()
        {
            // 32×32 solid gray placeholder
            int width = 32, height = 32;
            int stride = width * 4;
            var pixels = new byte[stride * height];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i]     = 100; // B
                pixels[i + 1] = 100; // G
                pixels[i + 2] = 100; // R
                pixels[i + 3] = 200; // A
            }
            var bmp = BitmapSource.Create(width, height, 96, 96,
                PixelFormats.Bgra32, null, pixels, stride);
            bmp.Freeze();
            return bmp;
        }

        /// <summary>
        /// Extracts an icon for the given window. Falls back to executable icon,
        /// then to a placeholder. The returned source is frozen.
        /// </summary>
        public ImageSource ExtractIcon(IntPtr windowHandle, string? executablePath)
        {
            IntPtr hIcon = IntPtr.Zero;
            bool ownsIcon = false;

            // 1. WM_GETICON — big
            if (windowHandle != IntPtr.Zero)
            {
                hIcon = ReadWindowIcon(windowHandle, NativeMethods.ICON_BIG);

                // 2. WM_GETICON — small
                if (hIcon == IntPtr.Zero)
                {
                    hIcon = ReadWindowIcon(windowHandle, NativeMethods.ICON_SMALL);
                }

                // 3. Class icon
                if (hIcon == IntPtr.Zero)
                {
                    hIcon = NativeMethods.GetClassLongPtr(windowHandle, NativeMethods.GCLP_HICON);
                }
            }

            // 4. Shell file info from executable path
            if (hIcon == IntPtr.Zero && !string.IsNullOrWhiteSpace(executablePath))
            {
                hIcon = ExtractFromExecutable(executablePath);
                ownsIcon = hIcon != IntPtr.Zero;
            }

            if (hIcon != IntPtr.Zero)
            {
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon, Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(32, 32));
                    source.Freeze();
                    return source;
                }
                catch { /* fall through to placeholder */ }
                finally
                {
                    // Window/class icon handles are shared; shell-extracted handles are owned.
                    if (ownsIcon)
                        NativeMethods.DestroyIcon(hIcon);
                }
            }

            return _fallback;
        }

        private static IntPtr ReadWindowIcon(IntPtr handle, int size)
        {
            // Abort for a hung or exiting window; never wait indefinitely for WM_GETICON.
            return NativeMethods.SendMessageTimeout(handle, NativeMethods.WM_GETICON,
                new IntPtr(size), IntPtr.Zero, 0x2 | 0x20, 150, out var icon) != IntPtr.Zero
                ? icon : IntPtr.Zero;
        }

        private static IntPtr ExtractFromExecutable(string path)
        {
            var shfi = new NativeMethods.SHFILEINFO();
            var result = NativeMethods.SHGetFileInfo(
                path, 0, ref shfi,
                (uint)Marshal.SizeOf(shfi),
                NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);
            return result == IntPtr.Zero ? IntPtr.Zero : shfi.hIcon;
        }
    }
}
