/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using static Mosaic.UI.Wpf.Controls.TrayIconNative.NativeMethods;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts WPF <see cref="ImageSource"/> instances into native <c>HICON</c> handles suitable for the
    /// notification area, without System.Drawing or Windows Forms.
    /// </summary>
    /// <remarks>
    /// Bitmaps whose pixel size already matches the requested icon size are copied directly (no resampling, so
    /// no blur). Other bitmaps are scaled with WIC; vector <see cref="DrawingImage"/> sources are rendered through
    /// <see cref="RenderTargetBitmap"/>. Both are centered in a square. Rasterization uses premultiplied BGRA;
    /// the CreateIconIndirect input uses straight BGRA for correct Windows DrawIconEx compositing.
    /// The monochrome mask is derived from the alpha channel.
    /// </remarks>
    internal static class TrayIconImageConverter
    {
        /// <summary>
        /// Returns the small-icon size, in physical pixels, the shell uses for the notification area.
        /// </summary>
        /// <remarks>The notification area lives on the primary taskbar, so the primary monitor's DPI is used.</remarks>
        public static int GetTrayIconSize()
        {
            uint dpi = GetDpiForPoint(default, MONITOR_DEFAULTTOPRIMARY);
            int size = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
            return size > 0 ? size : 16;
        }

        /// <summary>
        /// Converts <paramref name="source"/> to an owned icon handle of <paramref name="size"/> × <paramref name="size"/> pixels.
        /// </summary>
        /// <param name="source">The image to convert.</param>
        /// <param name="size">The square icon size in physical pixels.</param>
        /// <returns>An owned icon handle, or <see langword="null"/> if the source cannot be rendered.</returns>
        public static TrayIconHandle? Convert(ImageSource source, int size)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            try
            {
                var bitmap = Rasterize(source, size);

                if (bitmap == null)
                {
                    return null;
                }

                // Supplying Pbgra32 to the CreateIconIndirect path multiplies alpha twice during native rendering
                // (verified by DrawIconEx compositing tests), despite the blend API using premultiplied RGB.
                var nativeBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
                var pixels = new byte[size * size * 4];
                nativeBitmap.CopyPixels(pixels, size * 4, 0);
                return CreateIcon(pixels, size);
            }
            catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException or IOException or COMException or UnauthorizedAccessException)
            {
                Trace.WriteLine($"TrayIcon: unable to convert image source to an icon: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads the executable's own icon at <paramref name="size"/>, falling back to the shared system application icon.
        /// </summary>
        public static unsafe TrayIconHandle GetDefaultIcon(int size)
        {
            string? path = Environment.ProcessPath;

            if (!string.IsNullOrEmpty(path))
            {
                IntPtr small = IntPtr.Zero;

                // nIconSize: LOWORD = large size, HIWORD = small size. Only the small icon is requested.
                uint packed = (uint)(size & 0xFFFF) | ((uint)(size & 0xFFFF) << 16);

                if (SHDefExtractIconW(path, 0, 0, null, &small, packed) == 0 && small != IntPtr.Zero)
                {
                    return new TrayIconHandle(small, ownsHandle: true);
                }
            }

            // Shared icon owned by the system: must never be passed to DestroyIcon.
            return new TrayIconHandle(LoadIconW(IntPtr.Zero, IDI_APPLICATION), ownsHandle: false);
        }

        /// <summary>
        /// Produces a <paramref name="size"/>-square premultiplied Pbgra32 bitmap from the source.
        /// </summary>
        internal static BitmapSource? Rasterize(ImageSource source, int size)
        {
            // Leave download completion/retry to TrayIcon; do not start another request while loading.
            if (source is BitmapSource { IsDownloading: true })
            {
                return null;
            }

            if (source is BitmapFrame frame)
            {
                source = SelectFrame(frame, size);
            }
            else if (source is BitmapImage image)
            {
                source = RecoverIconFrame(image, size);
            }

            if (source is BitmapSource bitmap)
            {
                if (bitmap.IsDownloading)
                {
                    return null;
                }

                if (bitmap.PixelWidth == size && bitmap.PixelHeight == size)
                {
                    return bitmap.Format == PixelFormats.Pbgra32 ? bitmap : new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);
                }

                return ScaleBitmap(bitmap, size);
            }

            return RenderVector(source, size);
        }

        /// <summary>
        /// Scales a bitmap with WIC (no <see cref="RenderTargetBitmap"/>, whose software renderer holds GDI objects
        /// until finalization) and centers it in a transparent square.
        /// </summary>
        private static BitmapSource? ScaleBitmap(BitmapSource bitmap, int size)
        {
            int pixelWidth = bitmap.PixelWidth;
            int pixelHeight = bitmap.PixelHeight;

            if (pixelWidth <= 0 || pixelHeight <= 0)
            {
                return null;
            }

            double scale = (double)size / Math.Max(pixelWidth, pixelHeight);

            // Keep premultiplied pixels throughout scaling; convert once at the native boundary.
            BitmapSource premultiplied = bitmap.Format == PixelFormats.Pbgra32 ? bitmap : new FormatConvertedBitmap(bitmap, PixelFormats.Pbgra32, null, 0);
            var scaled = new TransformedBitmap(premultiplied, new ScaleTransform(scale, scale));

            int width = Math.Min(size, scaled.PixelWidth);
            int height = Math.Min(size, scaled.PixelHeight);

            if (width == size && height == size)
            {
                return scaled;
            }

            var scaledPixels = new byte[scaled.PixelWidth * scaled.PixelHeight * 4];
            scaled.CopyPixels(scaledPixels, scaled.PixelWidth * 4, 0);

            var pixels = new byte[size * size * 4];
            int offsetX = (size - width) / 2;
            int offsetY = (size - height) / 2;

            for (int y = 0; y < height; y++)
            {
                Buffer.BlockCopy(scaledPixels, y * scaled.PixelWidth * 4, pixels, (((y + offsetY) * size) + offsetX) * 4, width * 4);
            }

            return BitmapSource.Create(size, size, 96, 96, PixelFormats.Pbgra32, null, pixels, size * 4);
        }

        /// <summary>
        /// Renders a non-bitmap source (e.g. a <see cref="DrawingImage"/>) uniformly scaled and centered.
        /// </summary>
        private static BitmapSource? RenderVector(ImageSource source, int size)
        {
            double width = source.Width;
            double height = source.Height;

            if (!(width > 0) || !(height > 0) || double.IsInfinity(width) || double.IsInfinity(height))
            {
                return null;
            }

            double scale = Math.Min(size / width, size / height);
            double drawWidth = width * scale;
            double drawHeight = height * scale;
            var rect = new Rect((size - drawWidth) / 2, (size - drawHeight) / 2, drawWidth, drawHeight);

            var visual = new DrawingVisual();
            RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

            using (var dc = visual.RenderOpen())
            {
                dc.DrawImage(source, rect);
            }

            // 96 DPI: one DIP == one physical pixel, so the render is exactly size × size pixels.
            var target = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            target.Render(visual);

            return target;
        }

        /// <summary>
        /// Recovers ICO frames hidden by BitmapImage's single-frame view, retaining the original on failure.
        /// </summary>
        private static BitmapSource RecoverIconFrame(BitmapImage image, int size)
        {
            // Do not inspect/reposition caller-owned StreamSource streams, or reopen ordinary PNG/JPEG images.
            if (image.StreamSource != null || image.UriSource == null)
            {
                return image;
            }

            try
            {
                Uri uri = image.UriSource;
                if (!uri.IsAbsoluteUri)
                {
                    Uri baseUri = ((IUriContext)image).BaseUri ?? BaseUriHelper.GetBaseUri(image);
                    uri = new Uri(baseUri, uri);
                }

                if (!uri.AbsolutePath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    return image;
                }

                // The WPF URI decoder supports application/site-of-origin pack resources as well as files.
                // OnLoad releases decoder-owned file/resource streams once all frames have been read.
                var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                if (!decoder.IsDownloading && decoder.Frames.Count > 0)
                {
                    return SelectFrame(decoder.Frames[0], size);
                }
            }
            catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException or ArgumentException
                or IOException or COMException or UnauthorizedAccessException or System.Net.WebException or System.Security.SecurityException)
            {
                Trace.WriteLine($"TrayIcon: unable to recover ICO frames, using the supplied bitmap: {ex.Message}");
            }

            return image;
        }

        /// <summary>
        /// For multi-frame sources (e.g. .ico files) picks the smallest frame at least <paramref name="size"/>
        /// pixels wide, or the largest frame if none is big enough.
        /// </summary>
        private static BitmapSource SelectFrame(BitmapFrame frame, int size)
        {
            var frames = frame.Decoder?.Frames;

            if (frames == null || frames.Count <= 1)
            {
                return frame;
            }

            BitmapFrame? best = null;
            BitmapFrame? largest = null;

            foreach (var candidate in frames)
            {
                if (largest == null || candidate.PixelWidth > largest.PixelWidth)
                {
                    largest = candidate;
                }

                if (candidate.PixelWidth >= size && (best == null || candidate.PixelWidth < best.PixelWidth))
                {
                    best = candidate;
                }
            }

            return best ?? largest ?? frame;
        }

        /// <summary>
        /// Builds an alpha icon from top-down straight BGRA <paramref name="pixels"/> for correct native alpha blending.
        /// </summary>
        internal static unsafe TrayIconHandle? CreateIcon(ReadOnlySpan<byte> pixels, int size)
        {
            int stride = size * 4;

            var header = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = size,
                biHeight = -size, // top-down, matching the source rows
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0 // BI_RGB
            };

            IntPtr color = CreateDIBSection(IntPtr.Zero, &header, 0, out IntPtr bits, IntPtr.Zero, 0);

            if (color == IntPtr.Zero || bits == IntPtr.Zero)
            {
                if (color != IntPtr.Zero)
                {
                    DeleteObject(color);
                }

                return null;
            }

            // Monochrome mask: rows are WORD aligned; a set bit marks a fully transparent pixel.
            int maskStride = ((size + 15) / 16) * 2;
            var mask = new byte[maskStride * size];
            IntPtr maskBitmap = IntPtr.Zero;

            try
            {
                var destination = new Span<byte>((void*)bits, stride * size);

                for (int y = 0; y < size; y++)
                {
                    var sourceRow = pixels.Slice(y * stride, stride);
                    sourceRow.CopyTo(destination.Slice(y * stride, stride));

                    for (int x = 0; x < size; x++)
                    {
                        if (sourceRow[(x * 4) + 3] == 0)
                        {
                            // Native alpha blending and mask rendering both require transparent black here.
                            destination.Slice((y * stride) + (x * 4), 4).Clear();
                            mask[(y * maskStride) + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                        }
                    }
                }

                fixed (byte* maskBits = mask)
                {
                    maskBitmap = CreateBitmap(size, size, 1, 1, maskBits);
                }

                if (maskBitmap == IntPtr.Zero)
                {
                    return null;
                }

                var info = new ICONINFO
                {
                    fIcon = 1,
                    hbmMask = maskBitmap,
                    hbmColor = color
                };

                IntPtr icon = CreateIconIndirect(&info);
                return icon == IntPtr.Zero ? null : new TrayIconHandle(icon, ownsHandle: true);
            }
            finally
            {
                // CreateIconIndirect copies both bitmaps, so the temporaries are always released here.
                DeleteObject(color);

                if (maskBitmap != IntPtr.Zero)
                {
                    DeleteObject(maskBitmap);
                }
            }
        }
    }
}
