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

            // 1. WM_GETICON — big
            if (windowHandle != IntPtr.Zero)
            {
                hIcon = NativeMethods.SendMessage(windowHandle,
                    NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_BIG), IntPtr.Zero);

                // 2. WM_GETICON — small
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.SendMessage(windowHandle,
                        NativeMethods.WM_GETICON, new IntPtr(NativeMethods.ICON_SMALL), IntPtr.Zero);

                // 3. Class icon
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.GetClassLongPtr(windowHandle, NativeMethods.GCLP_HICON);
            }

            // 4. Shell file info from executable path
            if (hIcon == IntPtr.Zero && !string.IsNullOrWhiteSpace(executablePath))
                hIcon = ExtractFromExecutable(executablePath);

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
            }

            return _fallback;
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
