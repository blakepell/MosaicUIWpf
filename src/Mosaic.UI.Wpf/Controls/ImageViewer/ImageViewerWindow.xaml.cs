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

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A themed, resizable window that hosts an <see cref="ImageViewer"/> so a single image can be
    /// inspected at any zoom level. Used by <see cref="MarkdownViewer"/> to preview a clicked image.
    /// </summary>
    internal partial class ImageViewerWindow : Window
    {
        /// <summary>
        /// The fraction of the work area the window may grow to when it is sized to its image.
        /// </summary>
        private const double MaxWorkAreaFraction = 0.85;

        /// <summary>
        /// The approximate height of the title bar and the viewer's status bar, added to the image
        /// height when the window is sized to its image.
        /// </summary>
        private const double ChromeHeight = 80;

        /// <summary>
        /// The horizontal allowance added to the image width when the window is sized to its image.
        /// </summary>
        private const double ChromeWidth = 24;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewerWindow"/> class.
        /// </summary>
        public ImageViewerWindow()
        {
            InitializeComponent();

            this.Loaded += (_, _) => Viewer.Focus();
            this.PreviewKeyDown += OnPreviewKeyDown;
        }

        /// <summary>
        /// Shows an image in a new <see cref="ImageViewerWindow"/>.
        /// </summary>
        /// <param name="source">The image to show.</param>
        /// <param name="title">The window title; <c>"Image"</c> when empty.</param>
        /// <param name="owner">The window that owns the preview, if any.</param>
        /// <param name="modal">
        /// <c>true</c> to show the window as a modal dialog; <c>false</c> to show it alongside the owner.
        /// </param>
        public static void ShowImage(ImageSource source, string? title, Window? owner, bool modal)
        {
            var window = new ImageViewerWindow
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Image" : title
            };

            // A local file is loaded by path so the status bar can report its size and Copy Path works;
            // anything else (a download or an inline data URI) is shown from the bitmap already decoded.
            if (source is BitmapImage { UriSource: { IsAbsoluteUri: true, IsFile: true } uri } && File.Exists(uri.LocalPath))
            {
                window.Viewer.ImagePath = uri.LocalPath;
            }
            else
            {
                window.Viewer.Source = source;
            }

            if (owner is { IsLoaded: true })
            {
                window.Owner = owner;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // A modal preview blocks its owner, so it stays out of the taskbar and cannot be minimized.
            window.TitleBar.ShowMinimizeButton = !modal;
            window.ShowInTaskbar = !modal;
            window.SizeToImage(source, owner);

            if (modal)
            {
                window.ShowDialog();
            }
            else
            {
                window.Show();
            }
        }

        /// <summary>
        /// Sizes the window so the image is shown at 100% when it fits within most of the work area,
        /// and to most of the work area (with the viewer fitting the image) when it does not.
        /// </summary>
        private void SizeToImage(ImageSource source, Window? owner)
        {
            if (source is not BitmapSource { IsDownloading: false, PixelWidth: > 0, PixelHeight: > 0 } bitmap)
            {
                return;
            }

            // The viewer's 100% is one image pixel per physical screen pixel, so convert to DIPs.
            var dpi = VisualTreeHelper.GetDpi(owner ?? (Visual)this);
            double imageWidth = bitmap.PixelWidth / dpi.DpiScaleX;
            double imageHeight = bitmap.PixelHeight / dpi.DpiScaleY;

            var workArea = SystemParameters.WorkArea;
            double maxWidth = workArea.Width * MaxWorkAreaFraction;
            double maxHeight = workArea.Height * MaxWorkAreaFraction;

            this.Width = Math.Clamp(imageWidth + ChromeWidth, MinWidth, Math.Max(MinWidth, maxWidth));
            this.Height = Math.Clamp(imageHeight + ChromeHeight, MinHeight, Math.Max(MinHeight, maxHeight));
        }

        /// <summary>
        /// Closes the window when the user presses <see cref="Key.Escape"/>.
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }
    }
}
