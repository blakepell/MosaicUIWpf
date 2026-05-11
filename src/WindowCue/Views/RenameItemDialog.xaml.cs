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
using System.Windows.Input;

namespace WindowCue.Views
{
    public partial class RenameItemDialog
    {
        /// <summary>
        /// Gets the label the user confirmed, or the original value if cancelled.
        /// </summary>
        public string NewLabel { get; set; }

        public RenameItemDialog(string currentLabel)
        {
            InitializeComponent();
            NewLabel = currentLabel;
            Loaded += (_, _) =>
            {
                LabelTextBox.SelectAll();
                LabelTextBox.Focus();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            NewLabel = LabelTextBox.Text.Trim();
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void LabelTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
            }
        }
    }
}
