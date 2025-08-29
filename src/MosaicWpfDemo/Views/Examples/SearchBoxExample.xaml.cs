/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class SearchBoxExample
    {
        public SearchBoxExample()
        {
            InitializeComponent();
        }

        private void SearchBox_OnSearchExecuted(object? sender, string? searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return;
            }

            MessageBox.Show($"You pressed enter on a search for: {SearchBox.Text}", "Search Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}