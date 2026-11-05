/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ImageViewerExample
    {
        public ImageViewerExample()
        {
            InitializeComponent();
        }

        private void OpenImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.ico;*.jxr;*.wdp|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                Viewer.LoadImage(dialog.FileName);
            }
        }

        private void Viewer_ImageFailed(object? sender, ImageViewerFailedEventArgs e)
        {
            System.Windows.MessageBox.Show($"The image could not be loaded:\n\n{e.Exception.Message}", "Image Viewer",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Viewer_OperationFailed(object? sender, ImageViewerFailedEventArgs e)
        {
            string action = e.Operation switch
            {
                ImageViewerOperation.Save => "save the image",
                ImageViewerOperation.CopyImage => "copy the image",
                ImageViewerOperation.CopyPath => "copy the path",
                _ => "complete the operation"
            };

            System.Windows.MessageBox.Show($"Unable to {action}:\n\n{e.Exception.Message}", "Image Viewer",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
