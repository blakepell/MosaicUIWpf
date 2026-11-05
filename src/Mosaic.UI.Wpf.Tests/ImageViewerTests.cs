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
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class ImageViewerTests
    {
        /// <summary>
        /// Runs the test body on an STA thread, which WPF controls require.
        /// </summary>
        private static void RunSta(Action action)
        {
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// Applies the control's shipped style and realizes its template at the given size.
        /// </summary>
        private static ImageViewer Realize(ImageViewer viewer, double width = 400, double height = 300)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/ImageViewer/ImageViewer.xaml")
            };

            viewer.Style = (Style)dictionary[typeof(ImageViewer)];
            viewer.ApplyTemplate();
            viewer.Measure(new Size(width, height));
            viewer.Arrange(new Rect(0, 0, width, height));
            viewer.UpdateLayout();

            return viewer;
        }

        /// <summary>
        /// Writes a solid PNG of the given pixel size to a temp file and returns its path.
        /// </summary>
        private static string CreateTempPng(int width, int height)
        {
            var pixels = new byte[width * height * 4];
            Array.Fill(pixels, (byte)0x80);

            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            string path = Path.Combine(Path.GetTempPath(), $"mosaic-imageviewer-{Guid.NewGuid():N}.png");

            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }

            return path;
        }

        [Fact]
        public void Loading_A_File_Reports_Metadata_And_Releases_The_File()
        {
            RunSta(() =>
            {
                string path = CreateTempPng(800, 400);

                try
                {
                    var viewer = new ImageViewer { ImagePath = path };

                    Assert.True(viewer.HasImage);
                    Assert.Equal(800, viewer.PixelWidth);
                    Assert.Equal(400, viewer.PixelHeight);
                    Assert.Equal(new FileInfo(path).Length, viewer.FileSize);
                    Assert.Equal("800 × 400 px", viewer.ResolutionText);
                    Assert.NotEqual("—", viewer.FileSizeText);
                }
                finally
                {
                    // Would throw if the viewer kept a lock on the file.
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void Large_Image_Is_Fitted_Into_The_Viewport()
        {
            RunSta(() =>
            {
                string path = CreateTempPng(2000, 1000);

                try
                {
                    var viewer = Realize(new ImageViewer { ImagePath = path });
                    var scrollViewer = (ScrollViewer)viewer.Template.FindName("PART_ScrollViewer", viewer);
                    var image = (Image)viewer.Template.FindName("PART_Image", viewer);

                    Assert.True(viewer.IsZoomToFit);
                    Assert.True(viewer.Zoom < 1.0);
                    Assert.True(image.ActualWidth <= scrollViewer.ActualWidth);
                    Assert.True(image.ActualHeight <= scrollViewer.ActualHeight);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void Small_Image_Is_Not_Enlarged_When_Fitting()
        {
            RunSta(() =>
            {
                string path = CreateTempPng(50, 40);

                try
                {
                    var viewer = Realize(new ImageViewer { ImagePath = path });

                    Assert.True(viewer.IsZoomToFit);
                    Assert.Equal(1.0, viewer.Zoom, 3);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void Manual_Zoom_Cancels_Fit_And_ZoomToFit_Restores_It()
        {
            RunSta(() =>
            {
                string path = CreateTempPng(2000, 1000);

                try
                {
                    var viewer = Realize(new ImageViewer { ImagePath = path });
                    double fitted = viewer.Zoom;

                    viewer.ZoomIn();
                    Assert.False(viewer.IsZoomToFit);
                    Assert.True(viewer.Zoom > fitted);

                    viewer.ZoomToFit();
                    Assert.True(viewer.IsZoomToFit);
                    Assert.Equal(fitted, viewer.Zoom, 6);
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [Fact]
        public void Zoom_Is_Clamped_To_Limits()
        {
            RunSta(() =>
            {
                var viewer = new ImageViewer { MinZoom = 0.25, MaxZoom = 4 };

                viewer.Zoom = 100;
                Assert.Equal(4, viewer.Zoom);

                viewer.Zoom = 0.01;
                Assert.Equal(0.25, viewer.Zoom);
            });
        }

        [Fact]
        public void Rotation_Is_Normalized_To_Quarter_Turns()
        {
            RunSta(() =>
            {
                var viewer = new ImageViewer { Rotation = -90 };
                Assert.Equal(270, viewer.Rotation);

                viewer.RotateRight();
                Assert.Equal(0, viewer.Rotation);

                viewer.Rotation = 100;
                Assert.Equal(90, viewer.Rotation);
            });
        }

        [Fact]
        public void Missing_File_Raises_ImageFailed_And_Clears_The_Image()
        {
            RunSta(() =>
            {
                var viewer = new ImageViewer();
                ImageViewerFailedEventArgs? failed = null;
                viewer.ImageFailed += (_, e) => failed = e;

                string missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");
                viewer.ImagePath = missing;

                Assert.NotNull(failed);
                Assert.Equal(missing, failed!.Path);
                Assert.False(viewer.HasImage);
                Assert.Null(viewer.Source);
            });
        }
    }
}
