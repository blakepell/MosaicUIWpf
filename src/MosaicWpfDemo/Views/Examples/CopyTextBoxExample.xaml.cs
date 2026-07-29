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
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class CopyTextBoxExample
    {
        public CopyTextBoxExample()
        {
            InitializeComponent();
        }

        private void OnTextCopied(object sender, TextCopiedEventArgs e)
        {
            StatusTextBlock.Text = e.Successful
                ? $"Copied {e.Text.Length:N0} character(s) to the clipboard: \"{e.Text}\""
                : $"The copy failed: {e.Exception?.Message}";
        }

        private void OnShowToastClick(object sender, RoutedEventArgs e)
        {
            ToastCopyTextBox.ShowToast = ShowToastCheckBox.IsChecked == true;
        }
    }
}
