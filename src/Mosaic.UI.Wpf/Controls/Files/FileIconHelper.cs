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

using System.Collections.Concurrent;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Resolves the small operating-system shell icon for a file type via the Win32 shell API and
    /// converts it to a frozen, cacheable <see cref="ImageSource"/>. Icons are cached by file
    /// extension so that listing a folder of many same-typed files only incurs a single shell call.
    /// </summary>
    internal static class FileIconHelper
    {
        // Extensions whose icon is file-specific rather than type-specific (executables and shortcuts
        // can carry their own embedded icon), so those are not cached by extension.
        private static readonly HashSet<string> PerFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".lnk", ".ico", ".cur", ".ani"
        };

        private static readonly ConcurrentDictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the small shell icon for the supplied file path. The icon is resolved from the file's
        /// type association (so the file does not need to exist for most extensions) and cached by
        /// extension where the icon is not file-specific.
        /// </summary>
        /// <param name="filePath">The full path of the file.</param>
        /// <returns>A frozen <see cref="ImageSource"/>, or <c>null</c> if an icon could not be resolved.</returns>
        public static ImageSource? GetIcon(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            if (!PerFileExtensions.Contains(extension) && !string.IsNullOrEmpty(extension))
            {
                return Cache.GetOrAdd(extension, _ => LoadIcon(filePath, useFileAttributes: true));
            }

            // Per-file or extension-less: query the actual file, do not cache.
            return LoadIcon(filePath, useFileAttributes: !PerFileExtensions.Contains(extension));
        }

        /// <summary>
        /// Calls <c>SHGetFileInfo</c> to obtain the small icon handle for a file, converts it to a frozen
        /// <see cref="BitmapSource"/>, and destroys the native handle.
        /// </summary>
        private static ImageSource? LoadIcon(string filePath, bool useFileAttributes)
        {
            uint flags = SHGFI_ICON | SHGFI_SMALLICON;
            uint attributes = 0;

            if (useFileAttributes)
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                attributes = FILE_ATTRIBUTE_NORMAL;
            }

            var info = new SHFILEINFO();
            IntPtr result = SHGetFileInfo(filePath, attributes, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), flags);

            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var source = Imaging.CreateBitmapSourceFromHIcon(
                    info.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                source.Freeze();
                return source;
            }
            catch
            {
                return null;
            }
            finally
            {
                DestroyIcon(info.hIcon);
            }
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
