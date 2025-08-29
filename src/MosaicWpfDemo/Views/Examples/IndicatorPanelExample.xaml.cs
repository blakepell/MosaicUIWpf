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
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class IndicatorPanelExample
    {
        public IndicatorPanelExample()
        {
            InitializeComponent();
        }

        private void ButtonChangeAccent_OnClick(object sender, RoutedEventArgs e)
        {
            if (IndicatorPanel.AccentBrush == Brushes.CornflowerBlue)
            {
                IndicatorPanel.AccentBrush = Brushes.Green;
                return;
            }

            if (IndicatorPanel.AccentBrush == Brushes.Green)
            {
                IndicatorPanel.AccentBrush = Brushes.Orange;
                return;
            }

            if (IndicatorPanel.AccentBrush == Brushes.Orange)
            {
                IndicatorPanel.AccentBrush = Brushes.Red;
                return;
            }

            if (IndicatorPanel.AccentBrush == Brushes.Red)
            {
                IndicatorPanel.AccentBrush = Brushes.Transparent;
                return;
            }

            if (IndicatorPanel.AccentBrush == Brushes.Transparent)
            {
                IndicatorPanel.AccentBrush = Brushes.CornflowerBlue;
                return;
            }
        }
    }
}