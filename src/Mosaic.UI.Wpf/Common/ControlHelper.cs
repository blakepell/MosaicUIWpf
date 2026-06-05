using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Provides helper methods for common control operations.
    /// </summary>
    public class ControlsHelper : DependencyObject
    {
        private static Win32.DeskTopSize _size;

        /// <summary>
        /// Shakes the active window to provide a visual notification.
        /// </summary>
        /// <param name="window">The window to shake, or <see langword="null" /> to use the active window.</param>
        public static void WindowShake(Window? window = null)
        {
            if (window == null)
            {
                if (Application.Current != null && Application.Current.Windows.Count > 0)
                {
                    window = Application.Current.Windows.OfType<Window>().FirstOrDefault(o => o.IsActive);
                }
            }

            if (window == null)
            {
                return;
            }

            if (window.WindowState != WindowState.Normal)
            {
                return;
            }

            var doubleAnimation = new DoubleAnimation
            {
                From = window.Left,
                To = window.Left + 15,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
                FillBehavior = FillBehavior.Stop
            };
            window.BeginAnimation(Window.LeftProperty, doubleAnimation);
        }


        /// <summary>
        /// Creates a resized bitmap image from a source image.
        /// </summary>
        /// <param name="source">The source image.</param>
        /// <param name="width">The target width.</param>
        /// <param name="height">The target height.</param>
        /// <param name="margin">The margin to apply inside the target bounds.</param>
        /// <returns>The resized bitmap frame.</returns>
        public static BitmapFrame CreateResizedImage(ImageSource source, int width, int height, int margin)
        {
            var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawDrawing(group);
            }

            var resizedImage = new RenderTargetBitmap(
                width, height,
                96, 96,
                PixelFormats.Default);
            resizedImage.Render(drawingVisual);
            return BitmapFrame.Create(resizedImage);
        }


        /// <summary>
        /// Captures the desktop screen as a bitmap.
        /// </summary>
        /// <returns>A bitmap source for the captured desktop image, or <see langword="null" /> if capture fails.</returns>
        public static BitmapSource? Capture()
        {
            IntPtr hBitmap;
            var hDC = Win32.GetDC(Win32.GetDesktopWindow());
            var hMemDC = Win32.CreateCompatibleDC(hDC);
            _size.cx = Win32.GetSystemMetrics(0);
            _size.cy = Win32.GetSystemMetrics(1);
            hBitmap = Win32.CreateCompatibleBitmap(hDC, _size.cx, _size.cy);
            if (hBitmap != IntPtr.Zero)
            {
                var hOld = Win32.SelectObject(hMemDC, hBitmap);
                Win32.BitBlt(hMemDC, 0, 0, _size.cx, _size.cy, hDC, 0, 0,
                    Win32.TernaryRasterOperations.SRCCOPY);
                Win32.SelectObject(hMemDC, hOld);
                Win32.DeleteDC(hMemDC);
                Win32.ReleaseDC(Win32.GetDesktopWindow(), hDC);
                var bsource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                Win32.DeleteObject(hBitmap);
                GC.Collect();
                return bsource;
            }
            return null;
        }

        /// <summary>
        /// Gets the adorner layer for a visual element.
        /// </summary>
        /// <param name="visual">The visual to inspect, or <see langword="null" />.</param>
        /// <returns>The matching adorner layer, or <see langword="null" /> if no layer exists.</returns>
        public static AdornerLayer? GetAdornerLayer(Visual? visual)
        {
            if (visual == null)
            {
                return null;
            }

            if (visual is AdornerDecorator decorator)
            {
                return decorator.AdornerLayer;
            }

            if (visual is ScrollContentPresenter presenter)
            {
                return presenter.AdornerLayer;
            }

            if (visual is Window window)
            {
                var visualContent = window.Content as Visual;
                return AdornerLayer.GetAdornerLayer(visualContent ?? visual);
            }
            return AdornerLayer.GetAdornerLayer(visual);
        }

        /// <summary>
        /// Gets the current default window.
        /// </summary>
        /// <returns>The active or first available window, or <see langword="null" /> if no window exists.</returns>
        public static Window? GetDefaultWindow()
        {
            Window? window = null;
            if (Application.Current != null && Application.Current.Windows.Count > 0)
            {
                window = Application.Current.Windows.OfType<Window>().FirstOrDefault(o => o.IsActive);
                if (window == null)
                {
                    window = Enumerable.FirstOrDefault(Application.Current.Windows.OfType<Window>());
                }
            }
            return window;
        }

        /// <summary>
        /// Gets the padding value from a framework element.
        /// </summary>
        /// <param name="element">The element to inspect, or <see langword="null" />.</param>
        /// <returns>The padding value, or an empty thickness when no padding is available.</returns>
        public static Thickness GetPadding(FrameworkElement? element)
        {
            if (element == null)
            {
                return new Thickness();
            }

            Type elementType = element.GetType();
            PropertyInfo? paddingProperty = elementType.GetProperty("Padding");
            if (paddingProperty != null)
            {
                return (Thickness)paddingProperty.GetValue(element, null)!;
            }

            return new Thickness();
        }

        /// <summary>
        /// Finds the first visual child of a given type.
        /// </summary>
        /// <typeparam name="T">The type of child to locate.</typeparam>
        /// <param name="parent">The parent visual to search.</param>
        /// <returns>The first matching child, or <see langword="null" />.</returns>
        public static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the first visual parent of a given type.
        /// </summary>
        /// <typeparam name="T">The type of parent to locate.</typeparam>
        /// <param name="child">The child visual to search from.</param>
        /// <returns>The first matching parent, or <see langword="null" />.</returns>
        public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent == null)
            {
                return null;
            }

            if (parent is T tParent)
            {
                return tParent;
            }

            return FindParent<T>(parent);
        }

        /// <summary>
        /// Creates a cloned UI element from XAML serialization.
        /// </summary>
        /// <param name="Content">The content to clone, or <see langword="null" />.</param>
        /// <returns>The cloned UI element, or <see langword="null" /> if cloning is not possible.</returns>
        public static UIElement? GetXmlReader(object? Content)
        {
            try
            {
                if (Content is string contentString)
                {
                    Content = new TextBlock { Text = contentString };
                }

                var originalContent = Content as UIElement;
                if (originalContent == null)
                {
                    return null;
                }

                string contentXaml = XamlWriter.Save(originalContent);
                using (StringReader stringReader = new StringReader(contentXaml))
                {
                    using (XmlReader xmlReader = XmlReader.Create(stringReader))
                    {
                        object clonedContent = XamlReader.Load(xmlReader);

                        if (clonedContent is UIElement clonedElement)
                        {
                            return clonedElement;
                        }
                    }
                }
                return null;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}