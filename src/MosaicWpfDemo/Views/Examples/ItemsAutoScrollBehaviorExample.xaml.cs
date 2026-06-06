/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ItemsAutoScrollBehaviorExample
    {
        public ObservableCollection<string> Items { get; } = [];

        private int _nextItemNumber = 1;

        public ItemsAutoScrollBehaviorExample()
        {
            InitializeComponent();
            DataContext = this;

            Items.Add("Item 1");
            _nextItemNumber = 2;
        }

        private void AddItemButton_OnClick(object sender, RoutedEventArgs e)
        {
            Items.Add($"Item {_nextItemNumber++}");
        }

        private async void AddTenItems_OnClick(object sender, RoutedEventArgs e)
        {
            int ceiling = _nextItemNumber + 10;

            for (int i = _nextItemNumber; i < ceiling; i++)
            {
                Items.Add($"Item {_nextItemNumber++}");
                await Task.Delay(100);
            }
        }
    }
}
