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
using System.Windows.Input;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class FlipPanelExample
    {
        public FlipPanelExample()
        {
            InitializeComponent();
        }

        private void FlipHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            FlipPanelCoin.Direction = FlipDirection.Horizontal;
            FlipPanelCoin.Flip();
        }

        private void FlipVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            FlipPanelCoin.Direction = FlipDirection.Vertical;
            FlipPanelCoin.Flip();
        }

        private void FlipPanelCoin_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (FlipPanelCoin.Direction == FlipDirection.Vertical)
            {
                FlipPanelCoin.Direction = FlipDirection.Horizontal;
                FlipPanelCoin.Flip();
                return;
            }

            if (FlipPanelCoin.Direction == FlipDirection.Horizontal)
            {
                FlipPanelCoin.Direction = FlipDirection.Vertical;
                FlipPanelCoin.Flip();
                return;
            }
        }
    }
}