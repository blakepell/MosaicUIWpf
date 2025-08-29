/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Diagnostics;
using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class AvatarExample
    {
        public AvatarExample()
        {
            InitializeComponent();
        }

        private void Avatar_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Process.Start("explorer.exe", "https://www.blakepell.com/about");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
