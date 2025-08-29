/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Concurrent;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Cysharp.Text;

namespace Mosaic.UI.Wpf.Cache
{
    /// <summary>
    /// Image cache consisting of images from various resources.
    /// </summary>
    public static class ImageCache
    {
        /// <summary>
        /// Image cache consisting of images from various resources.
        /// </summary>
        private static readonly ConcurrentDictionary<string, BitmapImage> Cache = new();

        /// <summary>
        /// Retrieves information about an object in the file system, such as a file, folder, directory, or drive root.
        /// </summary>
        /// <param name="pszPath">A string that specifies the path and file name.</param>
        /// <param name="dwFileAttributes">A combination of one or more file attribute flags.</param>
        /// <param name="psfi">A reference to a SHFILEINFO structure to receive the file information.</param>
        /// <param name="cbFileInfo">The size, in bytes, of the SHFILEINFO structure pointed to by psfi.</param>
        /// <param name="uFlags">Flags that specify the file information to retrieve.</param>
        /// <returns>
        /// Returns a handle to the icon that represents the file. If the function fails, the return value is zero and the psfi structure returned is undefined.
        /// </returns>
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern nint SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags
        );

        /// <summary>
        /// Contains information about a file object.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            /// <summary>
            /// A handle to the icon that represents the file.
            /// </summary>
            public nint hIcon;

            /// <summary>
            /// The index of the icon image within the system image list.
            /// </summary>
            public int iIcon;

            /// <summary>
            /// An array of values that indicates the attributes of the file object.
            /// </summary>
            public uint dwAttributes;

            /// <summary>
            /// A string that contains the name of the file as it appears in the file system.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            /// <summary>
            /// A string that describes the type of file.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        /// <summary>
        /// Retrieves the icon handle from the icon resource. It must be destroyed using the DestroyIcon function.
        /// </summary>
        private const uint SHGFI_ICON = 0x000000100;

        /// <summary>
        /// Indicates that the function should not attempt to access the file specified by pszPath.
        /// </summary>
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        /// <summary>
        /// Indicates a normal file that has no other attributes set.
        /// </summary>
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        /// <summary>
        /// Destroys an icon and frees any memory the icon occupied.
        /// </summary>
        /// <param name="hIcon">A handle to the icon to be destroyed.</param>
        /// <returns>
        /// If the function succeeds, the return value is true.
        /// If the function fails, the return value is false.
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(nint hIcon);

        /// <summary>
        /// Gets an image from the image cache.  This corresponds to an asset's filename
        /// in the Assets folder of the application that is set to be a type of Resource.
        /// </summary>
        /// <param name="resourceFilename"></param>
        public static BitmapImage GetImage(string? resourceFilename)
        {
            if (string.IsNullOrWhiteSpace(resourceFilename))
            {
                return GetImage("run-command-96.png");
            }

            if (Cache.TryGetValue(resourceFilename, out var image))
            {
                return image;
            }

            var bm = new BitmapImage(new Uri($"pack://application:,,/Assets/{resourceFilename}", UriKind.Absolute));
            Cache[resourceFilename] = bm;
            return bm;
        }

        /// <summary>
        /// Returns the asset URI as a string from the current application.
        /// </summary>
        /// <param name="resourceFilename"></param>
        public static string GetImageAsString(string? resourceFilename)
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                if (string.IsNullOrWhiteSpace(resourceFilename))
                {
                    return GetImageAsString("run-command-96.png");
                }

                sb.Append($"pack://application:,,/Assets/{resourceFilename}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Loads an image from a specified file path and adds it to the image cache.
        /// </summary>
        /// <param name="filePath">The full path to the image file.</param>
        /// <returns>A BitmapImage object loaded from the specified file path.</returns>
        public static BitmapImage? GetImageFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return GetImage("run-command-96.png");
            }

            if (Cache.TryGetValue(filePath, out var cachedImage))
            {
                return cachedImage;
            }

            if (!File.Exists(filePath))
            {
                return GetImage("run-command-96.png");
            }

            var bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Load the image data and cache it to avoid file locks
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Optional: Make the BitmapImage thread safe

            // Add the loaded BitmapImage to the cache
            Cache[filePath] = bitmapImage;

            return bitmapImage;
        }

        /// <summary>
        /// Gets an icon from the image cache.  This corresponds to an asset's filename
        /// the extracted icon for the specified executable.
        /// </summary>
        /// <param name="path">The full path to the executable</param>
        public static BitmapImage? GetIconFromExePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (!File.Exists(path))
            {
                return null;
            }

            if (Cache.TryGetValue(path, out var image))
            {
                return image;
            }

            using (var icon = Icon.ExtractAssociatedIcon(path))
            {
                if (icon == null)
                {
                    return null;
                }

                var bm = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                Cache[path] = ConvertBitmapSourceToBitmapImage(bm);
                return Cache[path];
            }
        }

        /// <summary>
        /// Gets the associated shell icon from Windows.
        /// </summary>
        /// <param name="extension"></param>
        public static ImageSource? GetFileTypeImageSource(string? extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return null;
            }

            if (!extension.StartsWith('.'))
            {
                extension = $".{extension}";
            }

            if (Cache.TryGetValue(extension, out var image))
            {
                return image;
            }

            var shinfo = new SHFILEINFO();
            var hImg = SHGetFileInfo(
                extension,
                FILE_ATTRIBUTE_NORMAL,
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_USEFILEATTRIBUTES
            );

            if (shinfo.hIcon == nint.Zero)
            {
                return null;
            }

            var bmSource = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            DestroyIcon(shinfo.hIcon);

            bmSource.Freeze();

            Cache[extension] = ConvertBitmapSourceToBitmapImage(bmSource);
            return Cache[extension];
        }

        /// <summary>
        /// Converts a <see cref="BitmapSource"/> to a <see cref="BitmapImage"/>.
        /// </summary>
        /// <param name="bitmapSource"></param>
        /// <returns></returns>
        public static BitmapImage ConvertBitmapSourceToBitmapImage(BitmapSource bitmapSource)
        {
            var bitmapImage = new BitmapImage();

            using (var ms = new MemoryStream())
            {
                // You can change the encoder to any other encoder supporting your image format
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);

                ms.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Optional: Call Freeze to make the BitmapImage thread safe
            }

            return bitmapImage;
        }
    }
}