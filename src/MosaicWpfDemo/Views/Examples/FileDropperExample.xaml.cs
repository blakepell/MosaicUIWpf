/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class FileDropperExample
    {
        public FileDropperExample()
        {
            InitializeComponent();
        }

        private void FileDropper_OnFileDrop(object sender, FileDropEventArgs e)
        {
            DroppedFilesTextBox.Text = string.Join(Environment.NewLine, e.Files);
        }
    }
}
