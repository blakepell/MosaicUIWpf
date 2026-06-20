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
using System.Windows.Controls;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class SplitPanelExample
    {
        public SplitPanelExample()
        {
            InitializeComponent();
        }

        private void OrientationToggle_Toggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Splitter.Orientation = e.NewValue ? Orientation.Horizontal : Orientation.Vertical;
        }

        private void Top_Click(object sender, RoutedEventArgs e) => Splitter.SplitterPosition = 0.0;

        private void Even_Click(object sender, RoutedEventArgs e) => Splitter.SplitterPosition = 0.5;

        private void Bottom_Click(object sender, RoutedEventArgs e) => Splitter.SplitterPosition = 1.0;
    }
}
