/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mosaic.UI.Wpf.Controls;
using Xunit;
using static Mosaic.UI.Wpf.Controls.TrayIconNative.NativeMethods;

namespace Mosaic.UI.Wpf.Tests
{
    public partial class TrayIconTests
    {
        private const string FrameResource = "pack://application:,,,/Mosaic.UI.Wpf.Tests;component/Assets/TrayFrames.ico";

        /// <summary>
        /// Verifies CreateIconIndirect's input contract independently of WPF format conversion.
        /// </summary>
        [Fact]
        public void NativeIcon_StraightColorInputProducesExpectedAlphaBlend()
        {
            RunSta(() =>
            {
                var straight = new byte[16 * 16 * 4];
                var premul = new byte[straight.Length];
                for (int i = 0; i < straight.Length; i += 4)
                {
                    straight[i] = 32; straight[i + 1] = 128; straight[i + 2] = 240; straight[i + 3] = 64;
                    premul[i] = 8; premul[i + 1] = 32; premul[i + 2] = 60; premul[i + 3] = 64;
                }
                using var a = TrayIconImageConverter.CreateIcon(straight, 16);
                using var b = TrayIconImageConverter.CreateIcon(premul, 16);
                Assert.NotNull(a);
                Assert.NotNull(b);
                Assert.Equal(new byte[] { 26, 50, 78, 255 }, DrawNative(a!, 16, 24));
                // Deliberately supply premultiplied input as a control: Windows multiplies it again.
                Assert.Equal(new byte[] { 20, 26, 33, 255 }, DrawNative(b!, 16, 24));
            });
        }

        /// <summary>
        /// Verifies frame identity using a fixture with red 16px, green 24px and blue 32px frames.
        /// </summary>
        [Theory]
        [InlineData(16, 2)]
        [InlineData(20, 1)]
        [InlineData(24, 1)]
        [InlineData(28, 0)]
        [InlineData(32, 0)]
        [InlineData(48, 0)]
        [InlineData(12, 2)]
        public void Converter_SelectsIcoFrameByColor(int size, int channel)
        {
            RunSta(() =>
            {
                byte[] ico = ReadFrameResource();
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "TrayFrames.ico");
                using var stream = new MemoryStream(ico);
                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                long position = stream.Position;
                AssertFrameColor(frame, size, channel);
                Assert.Equal(position, stream.Position);
                Assert.True(stream.CanRead);

                var uri = new Uri(path);
                AssertFrameColor(new BitmapImage(uri), size, channel);

                var relative = new BitmapImage();
                relative.BeginInit();
                ((IUriContext)relative).BaseUri = new Uri(Path.GetDirectoryName(path)! + Path.DirectorySeparatorChar);
                relative.UriSource = new Uri(Path.GetFileName(path), UriKind.Relative);
                relative.CacheOption = BitmapCacheOption.OnLoad;
                relative.EndInit();
                AssertFrameColor(relative, size, channel);

                AssertFrameColor(new BitmapImage(new Uri(FrameResource)), size, channel);
                var packRelative = new BitmapImage();
                packRelative.BeginInit();
                ((IUriContext)packRelative).BaseUri = new Uri("pack://application:,,,/Mosaic.UI.Wpf.Tests;component/Assets/");
                packRelative.UriSource = new Uri("TrayFrames.ico", UriKind.Relative);
                packRelative.EndInit();
                AssertFrameColor(packRelative, size, channel);
            });
        }

        /// <summary>
        /// Verifies fallback rendering without consuming caller-owned streams.
        /// </summary>
        [Fact]
        public void Converter_UnavailableIcoRetainsBitmap_AndLeavesCallerStreamAlone()
        {
            RunSta(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), $"mosaic-{Guid.NewGuid()}.ico");
                File.WriteAllBytes(path, ReadFrameResource());
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                File.Delete(path);
                var original = Pixel(image, image.PixelWidth / 2, image.PixelHeight / 2);
                Assert.Equal(original, Pixel(TrayIconImageConverter.Rasterize(image, 24)!, 12, 12));

                using var stream = new MemoryStream(ReadFrameResource());
                var streamed = new BitmapImage();
                streamed.BeginInit();
                streamed.StreamSource = stream;
                streamed.CacheOption = BitmapCacheOption.OnLoad;
                streamed.EndInit();
                stream.Position = 7;
                Assert.NotNull(TrayIconImageConverter.Rasterize(streamed, 20));
                Assert.Equal(7, stream.Position);
                Assert.True(stream.CanRead);
            });
        }

        /// <summary>
        /// Verifies raster pixels and native compositing for each source path and physical tray size.
        /// </summary>
        [Theory]
        [InlineData(16)]
        [InlineData(20)]
        [InlineData(24)]
        [InlineData(32)]
        public void Converter_PremultipliesExactlyOnce_AndDrawIconExCompositesCorrectly(int size)
        {
            RunSta(() =>
            {
                // Straight BGRA (32,128,240,64) -> premultiplied (8,32,60,64).
                var straight = CreateBitmap(size, 64);
                var premultiplied = new FormatConvertedBitmap(straight, PixelFormats.Pbgra32, null, 0);
                var vector = new DrawingImage(new GeometryDrawing(
                    new SolidColorBrush(Color.FromArgb(64, 240, 128, 32)), null,
                    new RectangleGeometry(new Rect(0, 0, 10, 5))));

                using var pngStream = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(straight));
                encoder.Save(pngStream);
                pngStream.Position = 0;
                var png = new BitmapImage();
                png.BeginInit();
                png.StreamSource = pngStream;
                png.CacheOption = BitmapCacheOption.OnLoad;
                png.EndInit();

                ImageSource[] sources = [straight, premultiplied, CreateBitmap(48, 64),
                    new FormatConvertedBitmap(CreateBitmap(48, 64), PixelFormats.Pbgra32, null, 0),
                    new CroppedBitmap(CreateBitmap(48, 64), new Int32Rect(0, 0, 48, 24)), png, vector];
                foreach (var source in sources)
                {
                    var raster = TrayIconImageConverter.Rasterize(source, size)!;
                    Assert.Equal(PixelFormats.Pbgra32, raster.Format);
                    var center = Pixel(raster, size / 2, size / 2);
                    Assert.Equal(new byte[] { 8, 32, 60, 64 }, center);
                    using var icon = TrayIconImageConverter.Convert(source, size);
                    Assert.NotNull(icon);
                    foreach (byte background in new byte[] { 24, 240 })
                    {
                        var rendered = DrawNative(icon!, size, background);
                        for (int c = 0; c < 3; c++)
                        {
                            int expected = center[c] + (int)Math.Round(background * (1 - 64 / 255.0));
                            Assert.True(Math.Abs(rendered[c] - expected) <= 1,
                                $"{source.GetType().Name}: BGRA {string.Join(',', center)} rendered {string.Join(',', rendered)}; expected {expected} for channel {c}");
                        }
                    }
                }

                Assert.Equal(new byte[] { 32, 128, 240, 255 },
                    Pixel(TrayIconImageConverter.Rasterize(CreateBitmap(size, 255), size)!, 0, 0));
                Assert.Equal(new byte[4], Pixel(TrayIconImageConverter.Rasterize(CreateBitmap(size, 0), size)!, 0, 0));
            });
        }

        /// <summary>
        /// Verifies that ordinary URI-backed PNG pixels remain usable without their original file.
        /// </summary>
        [Fact]
        public void Converter_PngUriRetainsLoadedPixelsAfterFileIsRemoved()
        {
            RunSta(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), $"mosaic-{Guid.NewGuid()}.png");
                try
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(CreateBitmap(16, 64)));
                    using (var file = File.Create(path)) encoder.Save(file);
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(path);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    File.Delete(path);
                    Assert.Equal(new byte[] { 8, 32, 60, 64 },
                        Pixel(TrayIconImageConverter.Rasterize(image, 20)!, 10, 10));
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        /// <summary>
        /// Verifies native color orientation and alpha extremes against light and dark backgrounds.
        /// </summary>
        [Fact]
        public void NativeIcon_PreservesOrientation_OpaqueColors_AndTransparentPixels()
        {
            RunSta(() =>
            {
                const int size = 16;
                var pixels = new byte[size * size * 4];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int i = (y * size + x) * 4;
                        pixels[i] = 32;
                        pixels[i + 1] = 128;
                        pixels[i + 2] = 240;
                        pixels[i + 3] = y < size / 2 ? (byte)255 : (byte)0;
                    }
                }

                using var icon = TrayIconImageConverter.CreateIcon(pixels, size);
                Assert.NotNull(icon);
                foreach (byte background in new byte[] { 24, 240 })
                {
                    Assert.Equal(new byte[] { 32, 128, 240, 255 }, DrawNative(icon!, size, background, 2));
                    Assert.Equal(new byte[] { background, background, background, 255 }, DrawNative(icon!, size, background, 14));
                }
            });
        }

        private static byte[] ReadFrameResource()
        {
            // Unit tests have no Application startup to register WPF's pack URI parser.
            _ = System.Windows.Navigation.BaseUriHelper.GetBaseUri(new DependencyObject());
            using var resource = Application.GetResourceStream(new Uri(FrameResource))!.Stream;
            using var copy = new MemoryStream();
            resource.CopyTo(copy);
            return copy.ToArray();
        }

        private static void AssertFrameColor(ImageSource source, int size, int channel)
        {
            var bitmap = TrayIconImageConverter.Rasterize(source, size)!;
            Assert.Equal(size, bitmap.PixelWidth);
            Assert.Equal(size, bitmap.PixelHeight);
            byte[] expected = [0, 0, 0, 255];
            expected[channel] = 255;
            Assert.Equal(expected, Pixel(bitmap, size / 2, size / 2));
        }

        private static byte[] Pixel(BitmapSource bitmap, int x, int y)
        {
            var bgra = bitmap.Format == PixelFormats.Pbgra32 || bitmap.Format == PixelFormats.Bgra32
                ? bitmap : new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            var pixel = new byte[4];
            bgra.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
            return pixel;
        }

        /// <summary>
        /// Draws onto a top-down DIB and reads native RGB after GDI has completed its alpha blend.
        /// </summary>
        private static unsafe byte[] DrawNative(TrayIconHandle icon, int size, byte background, int? sampleY = null)
        {
            var header = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER), biWidth = size, biHeight = -size,
                biPlanes = 1, biBitCount = 32
            };
            IntPtr bitmap = CreateDIBSection(IntPtr.Zero, &header, 0, out var bits, IntPtr.Zero, 0);
            Assert.NotEqual(IntPtr.Zero, bitmap);
            IntPtr dc = CreateCompatibleDC(IntPtr.Zero);
            IntPtr previous = IntPtr.Zero;
            try
            {
                Assert.NotEqual(IntPtr.Zero, dc);
                previous = SelectObject(dc, bitmap);
                var pixels = new Span<byte>((void*)bits, size * size * 4);
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = pixels[i + 1] = pixels[i + 2] = background;
                    pixels[i + 3] = 255;
                }

                Assert.True(DrawIconEx(dc, 0, 0, icon.DangerousGetHandle(), size, size, 0, IntPtr.Zero, 3));
                Assert.True(GdiFlush());
                return pixels.Slice(((sampleY ?? size / 2) * size + size / 2) * 4, 4).ToArray();
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(dc, previous);
                if (dc != IntPtr.Zero) DeleteDC(dc);
                DeleteObject(bitmap);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr dc, IntPtr bitmap);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr dc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GdiFlush();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DrawIconEx(IntPtr dc, int x, int y, IntPtr icon, int width, int height,
            uint step, IntPtr brush, uint flags);
    }
}
