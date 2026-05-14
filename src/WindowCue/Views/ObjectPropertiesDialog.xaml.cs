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

namespace WindowCue.Views
{
    public partial class ObjectPropertiesDialog
    {
        /// <summary>
        /// Gets or sets the selected object for the dialog.
        /// </summary>
        public object? SelectedObject { get; set; }

        public ObjectPropertiesDialog(object obj)
        {
            InitializeComponent();
            SelectedObject = obj;
            ItemPropertyGrid.Object = obj;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

    }
}
