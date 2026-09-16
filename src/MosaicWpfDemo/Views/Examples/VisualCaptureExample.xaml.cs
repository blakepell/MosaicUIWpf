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
using System.Windows.Media;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class VisualCaptureExample
    {
        public VisualCaptureExample()
        {
            InitializeComponent();

            for (int i = 1; i <= 40; i++)
            {
                CaptureListBox.Items.Add($"Row {i:00} - this line is intentionally long enough to force a horizontal scroll bar so both directions are captured.");
            }
        }

        private void OnCaptured(object? sender, VisualCapturedEventArgs e)
        {
            StatusTextBlock.Text = e.Mode == VisualCaptureMode.CopyToClipboard
                ? "Image copied to the clipboard.  Paste it into any image-aware application to see the full scroll extent."
                : $"Image saved to {e.FilePath}";
        }

        private void OnCaptureFailed(object? sender, VisualCapturedEventArgs e)
        {
            StatusTextBlock.Text = $"Capture failed: {e.Error?.Message}";
        }

        private void OnCopyWholePageClick(object sender, RoutedEventArgs e)
        {
            if (VisualCapture.TryCopyToClipboard(this, new VisualCaptureOptions { Background = Brushes.White }, out var error))
            {
                StatusTextBlock.Text = "The entire example page was copied to the clipboard via VisualCapture.CopyToClipboard.";
            }
            else
            {
                StatusTextBlock.Text = $"Capture failed: {error?.Message}";
            }
        }
    }
}
