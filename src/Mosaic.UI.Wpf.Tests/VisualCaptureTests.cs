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
using Microsoft.Xaml.Behaviors;
using Mosaic.UI.Wpf.Behaviors;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Controls;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    public class VisualCaptureTests
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
        /// Builds a 200x100 host containing a ScrollViewer whose content is 300 wide and 400 tall, so
        /// it scrolls in both directions, and lays it out.
        /// </summary>
        private static (Grid Host, ScrollViewer Viewer) CreateScrollingHost()
        {
            var content = new StackPanel { Width = 300 };

            for (int i = 0; i < 20; i++)
            {
                content.Children.Add(new Border { Height = 20, Background = Brushes.SteelBlue });
            }

            var viewer = new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var host = new Grid { Width = 200, Height = 100 };
            host.Children.Add(viewer);
            host.Measure(new Size(200, 100));
            host.Arrange(new Rect(0, 0, 200, 100));
            host.UpdateLayout();

            return (host, viewer);
        }

        [Fact]
        public void Render_Captures_The_Full_Scroll_Extent_In_Both_Directions()
        {
            RunSta(() =>
            {
                var (_, viewer) = CreateScrollingHost();

                Assert.True(viewer.ExtentHeight > viewer.ViewportHeight);
                Assert.True(viewer.ExtentWidth > viewer.ViewportWidth);

                var bitmap = VisualCapture.Render(viewer, new VisualCaptureOptions { Dpi = 96 });

                Assert.Equal(300, bitmap.PixelWidth);
                Assert.Equal(400, bitmap.PixelHeight);
            });
        }

        /// <summary>
        /// Reads a single pixel out of a rendered bitmap.
        /// </summary>
        private static Color GetPixel(RenderTargetBitmap bitmap, int x, int y)
        {
            var pixel = new byte[4];
            bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);

            // Pbgra32: the channels are premultiplied by alpha.
            if (pixel[3] == 0)
            {
                return Colors.Transparent;
            }

            return Color.FromArgb(pixel[3],
                (byte)(pixel[2] * 255 / pixel[3]),
                (byte)(pixel[1] * 255 / pixel[3]),
                (byte)(pixel[0] * 255 / pixel[3]));
        }

        [Fact]
        public void Render_Draws_The_Element_Itself_And_Not_Just_The_Background()
        {
            RunSta(() =>
            {
                // A non-scrolling element that was laid out once and never invalidated again is the case
                // that used to come back blank when the element was drawn through a VisualBrush.
                var target = new Border { Width = 40, Height = 20, Background = Brushes.Red };
                var host = new Grid { Width = 40, Height = 20 };
                host.Children.Add(target);
                host.Measure(new Size(40, 20));
                host.Arrange(new Rect(0, 0, 40, 20));
                host.UpdateLayout();

                var bitmap = VisualCapture.Render(target, new VisualCaptureOptions { Dpi = 96, Background = Brushes.Lime });

                Assert.Equal(Colors.Red, GetPixel(bitmap, 20, 10));
            });
        }

        [Fact]
        public void Render_Ignores_The_Offset_The_Element_Has_Within_Its_Parent()
        {
            RunSta(() =>
            {
                var target = new Border
                {
                    Width = 40,
                    Height = 20,
                    Margin = new Thickness(30, 15, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Background = Brushes.Red
                };

                var host = new Grid { Width = 200, Height = 100 };
                host.Children.Add(target);
                host.Measure(new Size(200, 100));
                host.Arrange(new Rect(0, 0, 200, 100));
                host.UpdateLayout();

                var bitmap = VisualCapture.Render(target, new VisualCaptureOptions { Dpi = 96 });

                Assert.Equal(40, bitmap.PixelWidth);
                Assert.Equal(20, bitmap.PixelHeight);

                // Every corner is the element; the margin must not have shifted the image.
                Assert.Equal(Colors.Red, GetPixel(bitmap, 0, 0));
                Assert.Equal(Colors.Red, GetPixel(bitmap, 39, 19));
            });
        }

        [Fact]
        public void Render_Realizes_Every_Item_Of_A_Virtualizing_ListBox()
        {
            RunSta(() =>
            {
                var listBox = new ListBox();

                for (int i = 0; i < 50; i++)
                {
                    listBox.Items.Add($"Item {i}");
                }

                var host = new Grid { Width = 200, Height = 100 };
                host.Children.Add(listBox);
                host.Measure(new Size(200, 100));
                host.Arrange(new Rect(0, 0, 200, 100));
                host.UpdateLayout();

                Assert.Equal(100, listBox.ActualHeight);

                var bitmap = VisualCapture.Render(listBox, new VisualCaptureOptions { Dpi = 96 });

                // 50 rows of default-sized items are far taller than the 100px viewport.
                Assert.True(bitmap.PixelHeight > 500, $"Expected the full list, got {bitmap.PixelHeight}px.");
                Assert.Equal(200, bitmap.PixelWidth);
                Assert.Equal(100, listBox.ActualHeight);
            });
        }

        [Fact]
        public void Render_Restores_The_Original_Layout_After_Capturing()
        {
            RunSta(() =>
            {
                var (_, viewer) = CreateScrollingHost();

                VisualCapture.Render(viewer, new VisualCaptureOptions { Dpi = 96 });

                Assert.Equal(200, viewer.ActualWidth);
                Assert.Equal(100, viewer.ActualHeight);
                Assert.Equal(ScrollBarVisibility.Auto, viewer.HorizontalScrollBarVisibility);
                Assert.Equal(ScrollBarVisibility.Auto, viewer.VerticalScrollBarVisibility);
                Assert.True(viewer.ExtentHeight > viewer.ViewportHeight);
            });
        }

        [Fact]
        public void Render_Uses_The_Viewport_When_Scroll_Extent_Capture_Is_Disabled()
        {
            RunSta(() =>
            {
                var (_, viewer) = CreateScrollingHost();

                var bitmap = VisualCapture.Render(viewer, new VisualCaptureOptions { Dpi = 96, CaptureScrollExtent = false });

                Assert.Equal(200, bitmap.PixelWidth);
                Assert.Equal(100, bitmap.PixelHeight);
            });
        }

        [Fact]
        public void Render_Clears_An_Explicit_Height_On_The_Scroll_Viewer_Then_Puts_It_Back()
        {
            RunSta(() =>
            {
                var (_, viewer) = CreateScrollingHost();
                viewer.Height = 100;
                viewer.UpdateLayout();

                var bitmap = VisualCapture.Render(viewer, new VisualCaptureOptions { Dpi = 96 });

                Assert.Equal(400, bitmap.PixelHeight);
                Assert.Equal(100, viewer.Height);
                Assert.Equal(100, viewer.ActualHeight);
            });
        }

        [Fact]
        public void Render_Scales_Pixels_With_Dpi()
        {
            RunSta(() =>
            {
                var (_, viewer) = CreateScrollingHost();

                var bitmap = VisualCapture.Render(viewer, new VisualCaptureOptions { Dpi = 192, CaptureScrollExtent = false });

                Assert.Equal(400, bitmap.PixelWidth);
                Assert.Equal(200, bitmap.PixelHeight);
            });
        }

        [Fact]
        public void Render_Throws_For_An_Element_With_No_Size()
        {
            RunSta(() =>
            {
                Assert.Throws<InvalidOperationException>(() => VisualCapture.Render(new Border()));
            });
        }

        [Fact]
        public void SaveToFile_Writes_An_Encoder_Matching_The_Extension()
        {
            RunSta(() =>
            {
                var (_, viewer) = CreateScrollingHost();
                var directory = Path.Combine(Path.GetTempPath(), "MosaicVisualCaptureTests", Guid.NewGuid().ToString("N"));
                var png = Path.Combine(directory, "capture.png");
                var jpg = Path.Combine(directory, "capture.jpg");

                try
                {
                    VisualCapture.SaveToFile(viewer, png, new VisualCaptureOptions { Dpi = 96 });
                    VisualCapture.SaveToFile(viewer, jpg, new VisualCaptureOptions { Dpi = 96 });

                    var pngBytes = File.ReadAllBytes(png);
                    var jpgBytes = File.ReadAllBytes(jpg);

                    // PNG signature and JPEG SOI marker.
                    Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, pngBytes.Take(4).ToArray());
                    Assert.Equal(new byte[] { 0xFF, 0xD8 }, jpgBytes.Take(2).ToArray());
                }
                finally
                {
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                }
            });
        }

        [Fact]
        public void CreateEncoder_Falls_Back_To_Png()
        {
            Assert.IsType<System.Windows.Media.Imaging.PngBitmapEncoder>(VisualCapture.CreateEncoder(".webp"));
            Assert.IsType<System.Windows.Media.Imaging.PngBitmapEncoder>(VisualCapture.CreateEncoder(null));
            Assert.IsType<System.Windows.Media.Imaging.JpegBitmapEncoder>(VisualCapture.CreateEncoder(".JPEG"));
            Assert.IsType<System.Windows.Media.Imaging.TiffBitmapEncoder>(VisualCapture.CreateEncoder(".tif"));
        }

        [Fact]
        public void Button_Resolves_TargetName_Through_The_Name_Scope()
        {
            RunSta(() =>
            {
                var scope = new Grid();
                NameScope.SetNameScope(scope, new NameScope());

                var target = new Border { Name = "Chart" };
                scope.RegisterName("Chart", target);

                var button = new VisualCaptureButton { TargetName = "Chart" };
                scope.Children.Add(target);
                scope.Children.Add(button);

                Assert.Same(target, button.ResolveTarget());

                var explicitTarget = new Border();
                button.Target = explicitTarget;
                Assert.Same(explicitTarget, button.ResolveTarget());
            });
        }

        [Fact]
        public void Button_Raises_CaptureFailed_When_The_Target_Cannot_Be_Found()
        {
            RunSta(() =>
            {
                var button = new VisualCaptureButton { TargetName = "Missing" };
                VisualCapturedEventArgs? failure = null;
                bool captured = false;

                button.CaptureFailed += (_, e) => failure = e;
                button.Captured += (_, _) => captured = true;

                Assert.False(button.Capture());
                Assert.NotNull(failure);
                Assert.False(failure!.Succeeded);
                Assert.IsType<InvalidOperationException>(failure.Error);
                Assert.False(captured);
            });
        }

        [Fact]
        public void Button_Saves_To_FilePath_And_Raises_Captured()
        {
            RunSta(() =>
            {
                var (host, viewer) = CreateScrollingHost();
                var path = Path.Combine(Path.GetTempPath(), "MosaicVisualCaptureTests", Guid.NewGuid().ToString("N") + ".png");

                var button = new VisualCaptureButton { Target = viewer, Mode = VisualCaptureMode.SaveToFile, FilePath = path };
                VisualCapturedEventArgs? result = null;
                button.Captured += (_, e) => result = e;

                try
                {
                    Assert.True(button.Capture());
                    Assert.NotNull(result);
                    Assert.True(result!.Succeeded);
                    Assert.Equal(path, result.FilePath);
                    Assert.Equal(VisualCaptureMode.SaveToFile, result.Mode);
                    Assert.Same(viewer, result.Target);
                    Assert.True(File.Exists(path));
                }
                finally
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            });
        }

        [Fact]
        public void Behavior_Creates_A_Context_Menu_When_None_Exists()
        {
            RunSta(() =>
            {
                var element = new Border();
                var behavior = new VisualCaptureContextMenuBehavior();

                Interaction.GetBehaviors(element).Add(behavior);

                Assert.NotNull(element.ContextMenu);
                var items = element.ContextMenu!.Items.OfType<MenuItem>().ToList();
                Assert.Equal(2, items.Count);
                Assert.Equal("Copy as Image", items[0].Header);
                Assert.Equal("Save as Image...", items[1].Header);

                // A menu the behavior created has nothing above the separator, so it stays hidden.
                var separator = element.ContextMenu.Items.OfType<Separator>().Single();
                Assert.Equal(Visibility.Collapsed, separator.Visibility);

                Interaction.GetBehaviors(element).Remove(behavior);
                Assert.Null(element.ContextMenu);
            });
        }

        [Fact]
        public void Behavior_Appends_To_An_Existing_Context_Menu_And_Removes_Only_Its_Own_Items()
        {
            RunSta(() =>
            {
                var existing = new MenuItem { Header = "Existing" };
                var menu = new ContextMenu();
                menu.Items.Add(existing);

                var element = new Border { ContextMenu = menu };
                var behavior = new VisualCaptureContextMenuBehavior { CopyHeader = "Copy Picture", ShowSave = false };

                Interaction.GetBehaviors(element).Add(behavior);

                Assert.Same(menu, element.ContextMenu);
                Assert.Equal(4, menu.Items.Count);
                Assert.Same(existing, menu.Items[0]);
                Assert.IsType<Separator>(menu.Items[1]);
                Assert.Equal(Visibility.Visible, ((Separator)menu.Items[1]).Visibility);
                Assert.Equal("Copy Picture", ((MenuItem)menu.Items[2]).Header);
                Assert.Equal(Visibility.Collapsed, ((MenuItem)menu.Items[3]).Visibility);

                Interaction.GetBehaviors(element).Remove(behavior);

                Assert.Same(menu, element.ContextMenu);
                Assert.Single(menu.Items);
                Assert.Same(existing, menu.Items[0]);
            });
        }

        [Fact]
        public void Behavior_Defaults_To_Capturing_The_Attached_Element()
        {
            RunSta(() =>
            {
                var (host, viewer) = CreateScrollingHost();
                var behavior = new VisualCaptureContextMenuBehavior();
                Interaction.GetBehaviors(viewer).Add(behavior);

                Assert.Same(viewer, behavior.ResolveTarget());

                behavior.Target = host;
                Assert.Same(host, behavior.ResolveTarget());
            });
        }
    }
}
