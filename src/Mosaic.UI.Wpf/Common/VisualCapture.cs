/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media.Imaging;

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Renders a live <see cref="UIElement"/> to a bitmap and copies it to the clipboard or saves it to
    /// an image file.  When the element scrolls (it is, or contains, a <see cref="ScrollViewer"/>) the
    /// element is temporarily laid out at its full scroll extent so the entire vertical and horizontal
    /// content is captured rather than only the visible viewport.
    /// </summary>
    /// <remarks>
    /// <code><![CDATA[
    /// // Copy the full contents of a DataGrid, including rows that are scrolled out of view.
    /// VisualCapture.CopyToClipboard(MyDataGrid);
    ///
    /// // Save a panel to a PNG chosen by the user.
    /// string? path = VisualCapture.SaveToFile(MyPanel);
    ///
    /// // Save directly to a known path (format inferred from the extension).
    /// VisualCapture.SaveToFile(MyPanel, @"C:\Temp\panel.jpg");
    /// ]]></code>
    /// </remarks>
    public static class VisualCapture
    {
        /// <summary>
        /// The number of times a clipboard write is retried before the failure is surfaced.  The
        /// clipboard is a shared OS resource another process may briefly hold open.
        /// </summary>
        private const int ClipboardRetryCount = 3;

        /// <summary>
        /// The file filter used by the save dialog shown from <see cref="SaveToFile(UIElement, VisualCaptureOptions?)"/>.
        /// </summary>
        public const string SaveFileFilter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg;*.jpeg)|*.jpg;*.jpeg|Bitmap Image (*.bmp)|*.bmp|GIF Image (*.gif)|*.gif|TIFF Image (*.tif;*.tiff)|*.tif;*.tiff";

        /// <summary>
        /// Renders the element to a bitmap.
        /// </summary>
        /// <param name="element">The element to render.  It must be loaded and have a non-zero size.</param>
        /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
        /// <returns>The rendered bitmap.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The element has not been laid out or has a zero size.</exception>
        public static RenderTargetBitmap Render(UIElement element, VisualCaptureOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(element);
            options ??= new VisualCaptureOptions();

            element.UpdateLayout();

            var scrollViewers = options.CaptureScrollExtent ? FindOutermostScrollViewers(element) : new List<ScrollViewer>();
            var expansion = GetExpansion(scrollViewers);

            if (!expansion.Horizontal && !expansion.Vertical)
            {
                return RenderCore(element, element.RenderSize, options);
            }

            // Lay the element out at its full extent, render it, then hand layout back to the parent.
            var original = element.RenderSize;
            var constraint = new Size(
                expansion.Horizontal ? double.PositiveInfinity : original.Width,
                expansion.Vertical ? double.PositiveInfinity : original.Height);

            var restore = HideVisibleScrollBars(scrollViewers, expansion);
            var sizes = SuspendExplicitSizes(element, scrollViewers, expansion);

            try
            {
                element.Measure(constraint);

                var desired = element.DesiredSize;
                var arranged = new Size(
                    expansion.Horizontal ? Math.Max(desired.Width, original.Width) : original.Width,
                    expansion.Vertical ? Math.Max(desired.Height, original.Height) : original.Height);

                // Measure/Arrange lay out the subtree synchronously.  UpdateLayout must not be called
                // here: the parent was invalidated by the desired size change and would immediately
                // re-measure the element back to its on-screen size before it was rendered.
                element.Arrange(new Rect(arranged));

                return RenderCore(element, element.RenderSize, options);
            }
            finally
            {
                foreach (var (viewer, horizontal, vertical) in restore)
                {
                    viewer.HorizontalScrollBarVisibility = horizontal;
                    viewer.VerticalScrollBarVisibility = vertical;
                }

                foreach (var (target, property, value) in sizes)
                {
                    target.SetValue(property, value);
                }

                // Invalidating the parent as well makes it re-measure the element with the real
                // constraint; invalidating only the element would re-run its measure with the
                // unconstrained size that was just used for the capture.
                element.InvalidateMeasure();
                element.InvalidateArrange();

                if (VisualTreeHelper.GetParent(element) is UIElement parent)
                {
                    parent.InvalidateMeasure();
                    parent.InvalidateArrange();
                }

                element.UpdateLayout();
            }
        }

        /// <summary>
        /// Renders the element and places the image on the clipboard.  Both a device independent bitmap
        /// and a PNG stream are written so applications that understand PNG keep any transparency.
        /// </summary>
        /// <param name="element">The element to capture.</param>
        /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
        /// <exception cref="InvalidOperationException">The element has a zero size.</exception>
        /// <exception cref="ExternalException">The clipboard could not be opened after several attempts.</exception>
        public static void CopyToClipboard(UIElement element, VisualCaptureOptions? options = null)
        {
            var bitmap = Render(element, options);

            var png = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(png);
            png.Position = 0;

            var data = new DataObject();
            data.SetImage(bitmap);
            data.SetData("PNG", png, false);

            Exception? last = null;

            for (int attempt = 0; attempt < ClipboardRetryCount; attempt++)
            {
                try
                {
                    Clipboard.SetDataObject(data, true);
                    return;
                }
                catch (ExternalException ex)
                {
                    last = ex;
                    Thread.Sleep(50);
                }
            }

            if (last != null)
            {
                throw last;
            }
        }

        /// <summary>
        /// Attempts to render the element and place the image on the clipboard.
        /// </summary>
        /// <param name="element">The element to capture.</param>
        /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
        /// <param name="error">The exception that prevented the copy, or <see langword="null"/> on success.</param>
        /// <returns><see langword="true"/> when the image was placed on the clipboard.</returns>
        public static bool TryCopyToClipboard(UIElement element, VisualCaptureOptions? options, out Exception? error)
        {
            try
            {
                CopyToClipboard(element, options);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        /// <summary>
        /// Prompts the user for a file name and saves the rendered element to it.  The image format is
        /// chosen from the file extension the user picks.
        /// </summary>
        /// <param name="element">The element to capture.</param>
        /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
        /// <returns>The path the image was written to, or <see langword="null"/> when the user cancelled.</returns>
        public static string? SaveToFile(UIElement element, VisualCaptureOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(element);

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Image",
                Filter = SaveFileFilter,
                DefaultExt = ".png",
                AddExtension = true,
                FileName = options?.DefaultFileName ?? string.Empty,
                InitialDirectory = options?.InitialDirectory ?? string.Empty
            };

            if (dialog.ShowDialog(Window.GetWindow(element)) != true)
            {
                return null;
            }

            SaveToFile(element, dialog.FileName, options);
            return dialog.FileName;
        }

        /// <summary>
        /// Renders the element and writes it to the specified file.  The image format is inferred from
        /// the file extension (.png, .jpg/.jpeg, .bmp, .gif, .tif/.tiff); anything else is saved as PNG.
        /// </summary>
        /// <param name="element">The element to capture.</param>
        /// <param name="path">The path of the file to create or overwrite.</param>
        /// <param name="options">Rendering options, or <see langword="null"/> for the defaults.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">The element has a zero size.</exception>
        public static void SaveToFile(UIElement element, string path, VisualCaptureOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(element);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var bitmap = Render(element, options);
            var encoder = CreateEncoder(Path.GetExtension(path), options?.JpegQuality ?? VisualCaptureOptions.DefaultJpegQuality);
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(stream);
        }

        /// <summary>
        /// Creates the bitmap encoder that matches a file extension.
        /// </summary>
        /// <param name="extension">The file extension including the leading period.</param>
        /// <param name="jpegQuality">The quality level to use for JPEG output.</param>
        /// <returns>A new encoder.</returns>
        public static BitmapEncoder CreateEncoder(string? extension, int jpegQuality = VisualCaptureOptions.DefaultJpegQuality)
        {
            switch (extension?.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    return new JpegBitmapEncoder { QualityLevel = Math.Clamp(jpegQuality, 1, 100) };
                case ".bmp":
                    return new BmpBitmapEncoder();
                case ".gif":
                    return new GifBitmapEncoder();
                case ".tif":
                case ".tiff":
                    return new TiffBitmapEncoder();
                default:
                    return new PngBitmapEncoder();
            }
        }

        /// <summary>
        /// Renders the element at the given logical size into a <see cref="RenderTargetBitmap"/>.  The
        /// element is rendered directly (a <see cref="VisualBrush"/> is not used: its contents are
        /// realized lazily and an element that has not been invalidated since it was last drawn comes out
        /// blank).  Because <see cref="RenderTargetBitmap.Render"/> honours the offset the element has
        /// within its parent (margins, alignment), that offset is rendered as well and then cropped away.
        /// </summary>
        private static RenderTargetBitmap RenderCore(UIElement element, Size size, VisualCaptureOptions options)
        {
            if (size.Width <= 0 || size.Height <= 0 || double.IsNaN(size.Width) || double.IsNaN(size.Height))
            {
                throw new InvalidOperationException("The element must be loaded and have a non-zero size before it can be captured.");
            }

            double dpiX;
            double dpiY;

            if (options.Dpi.HasValue)
            {
                dpiX = dpiY = options.Dpi.Value;
            }
            else
            {
                var dpi = VisualTreeHelper.GetDpi(element);
                dpiX = dpi.PixelsPerInchX;
                dpiY = dpi.PixelsPerInchY;
            }

            int pixelWidth = Math.Max(1, (int)Math.Ceiling(size.Width * dpiX / 96.0));
            int pixelHeight = Math.Max(1, (int)Math.Ceiling(size.Height * dpiY / 96.0));

            var offset = VisualTreeHelper.GetOffset(element);
            int offsetX = Math.Max(0, (int)Math.Round(offset.X * dpiX / 96.0));
            int offsetY = Math.Max(0, (int)Math.Round(offset.Y * dpiY / 96.0));

            var rendered = new RenderTargetBitmap(offsetX + pixelWidth, offsetY + pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);
            rendered.Render(element);

            if (offsetX == 0 && offsetY == 0 && options.Background == null)
            {
                rendered.Freeze();
                return rendered;
            }

            BitmapSource source = offsetX == 0 && offsetY == 0
                ? rendered
                : new CroppedBitmap(rendered, new Int32Rect(offsetX, offsetY, pixelWidth, pixelHeight));

            var bounds = new Rect(size);
            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                if (options.Background != null)
                {
                    context.DrawRectangle(options.Background, null, bounds);
                }

                context.DrawImage(source, bounds);
            }

            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();

            return bitmap;
        }

        /// <summary>
        /// Returns the element itself if it is a <see cref="ScrollViewer"/>, otherwise the outermost
        /// <see cref="ScrollViewer"/> descendants in the visual tree.  Scroll viewers nested inside another
        /// scroll viewer are not returned; they expand naturally when their parent is measured unconstrained.
        /// </summary>
        private static List<ScrollViewer> FindOutermostScrollViewers(UIElement element)
        {
            var result = new List<ScrollViewer>();

            if (element is ScrollViewer root)
            {
                result.Add(root);
                return result;
            }

            var stack = new Stack<DependencyObject>();
            stack.Push(element);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                int count = VisualTreeHelper.GetChildrenCount(current);

                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);

                    if (child is ScrollViewer viewer)
                    {
                        if (viewer.Visibility == Visibility.Visible)
                        {
                            result.Add(viewer);
                        }
                    }
                    else
                    {
                        stack.Push(child);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines in which directions the element must grow so that every scroll viewer's content fits.
        /// A direction expands only when the scroll viewer allows scrolling in that direction and its
        /// extent exceeds the viewport.
        /// </summary>
        private static (bool Horizontal, bool Vertical) GetExpansion(List<ScrollViewer> scrollViewers)
        {
            bool horizontal = false;
            bool vertical = false;

            foreach (var viewer in scrollViewers)
            {
                if (viewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled
                    && viewer.ExtentWidth > viewer.ViewportWidth + 0.5)
                {
                    horizontal = true;
                }

                if (viewer.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled
                    && viewer.ExtentHeight > viewer.ViewportHeight + 0.5)
                {
                    vertical = true;
                }
            }

            return (horizontal, vertical);
        }

        /// <summary>
        /// An explicit Width/Height (or MaxWidth/MaxHeight) set directly on the element being captured or
        /// on one of its scroll viewers would stop it from growing to its content, so those local values
        /// are temporarily cleared in the directions that expand.  Only plain local values are touched;
        /// bound or styled values are left alone.  Returns the values to restore.
        /// </summary>
        private static List<(FrameworkElement Element, DependencyProperty Property, object Value)> SuspendExplicitSizes(
            UIElement element, List<ScrollViewer> scrollViewers, (bool Horizontal, bool Vertical) expansion)
        {
            var restore = new List<(FrameworkElement, DependencyProperty, object)>();
            var targets = new List<FrameworkElement>();

            if (element is FrameworkElement fe)
            {
                targets.Add(fe);
            }

            foreach (var viewer in scrollViewers)
            {
                if (!ReferenceEquals(viewer, element))
                {
                    targets.Add(viewer);
                }
            }

            foreach (var target in targets)
            {
                if (expansion.Horizontal)
                {
                    Suspend(target, FrameworkElement.WidthProperty, double.NaN, restore);
                    Suspend(target, FrameworkElement.MaxWidthProperty, double.PositiveInfinity, restore);
                }

                if (expansion.Vertical)
                {
                    Suspend(target, FrameworkElement.HeightProperty, double.NaN, restore);
                    Suspend(target, FrameworkElement.MaxHeightProperty, double.PositiveInfinity, restore);
                }
            }

            return restore;

            static void Suspend(FrameworkElement target, DependencyProperty property, double replacement, List<(FrameworkElement, DependencyProperty, object)> restore)
            {
                var local = target.ReadLocalValue(property);

                if (local is double)
                {
                    restore.Add((target, property, local));
                    target.SetValue(property, replacement);
                }
            }
        }

        /// <summary>
        /// Scroll bars whose visibility is <see cref="ScrollBarVisibility.Visible"/> would still render as
        /// an empty track once the content fits, so they are switched to Hidden for the duration of the
        /// capture.  Returns the original values so they can be restored.
        /// </summary>
        private static List<(ScrollViewer Viewer, ScrollBarVisibility Horizontal, ScrollBarVisibility Vertical)> HideVisibleScrollBars(
            List<ScrollViewer> scrollViewers, (bool Horizontal, bool Vertical) expansion)
        {
            var restore = new List<(ScrollViewer, ScrollBarVisibility, ScrollBarVisibility)>();

            foreach (var viewer in scrollViewers)
            {
                var horizontal = viewer.HorizontalScrollBarVisibility;
                var vertical = viewer.VerticalScrollBarVisibility;

                bool changeHorizontal = expansion.Horizontal && horizontal == ScrollBarVisibility.Visible;
                bool changeVertical = expansion.Vertical && vertical == ScrollBarVisibility.Visible;

                if (!changeHorizontal && !changeVertical)
                {
                    continue;
                }

                restore.Add((viewer, horizontal, vertical));

                if (changeHorizontal)
                {
                    viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                }

                if (changeVertical)
                {
                    viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                }
            }

            return restore;
        }
    }

    /// <summary>
    /// Options that control how <see cref="VisualCapture"/> renders an element.
    /// </summary>
    public class VisualCaptureOptions
    {
        /// <summary>
        /// The JPEG quality used when no explicit <see cref="JpegQuality"/> is supplied.
        /// </summary>
        public const int DefaultJpegQuality = 90;

        /// <summary>
        /// Gets or sets whether an element that scrolls is expanded so its entire horizontal and vertical
        /// scroll extent is captured.  Defaults to <see langword="true"/>.
        /// </summary>
        public bool CaptureScrollExtent { get; set; } = true;

        /// <summary>
        /// Gets or sets a brush painted behind the element.  <see langword="null"/> (the default) leaves
        /// areas the element does not paint transparent.
        /// </summary>
        public Brush? Background { get; set; }

        /// <summary>
        /// Gets or sets the DPI to render at.  <see langword="null"/> (the default) renders at the DPI of
        /// the monitor the element is displayed on so the image matches what the user sees.
        /// </summary>
        public double? Dpi { get; set; }

        /// <summary>
        /// Gets or sets the JPEG quality (1-100) used when saving to a .jpg/.jpeg file.
        /// </summary>
        public int JpegQuality { get; set; } = DefaultJpegQuality;

        /// <summary>
        /// Gets or sets the file name pre-populated in the save dialog.
        /// </summary>
        public string? DefaultFileName { get; set; }

        /// <summary>
        /// Gets or sets the directory the save dialog opens in.
        /// </summary>
        public string? InitialDirectory { get; set; }
    }
}
