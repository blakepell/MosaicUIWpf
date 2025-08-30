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
    public partial class InfoCardExample
    {
        public InfoCardExample()
        {
            InitializeComponent();
        }

        private void ButtonChangeAccent_OnClick(object sender, RoutedEventArgs e)
        {
            if (InfoCard.AccentBrush == Brushes.CornflowerBlue)
            {
                InfoCard.AccentBrush = Brushes.Green;
                return;
            }

            if (InfoCard.AccentBrush == Brushes.Green)
            {
                InfoCard.AccentBrush = Brushes.Orange;
                return;
            }

            if (InfoCard.AccentBrush == Brushes.Orange)
            {
                InfoCard.AccentBrush = Brushes.Red;
                return;
            }

            if (InfoCard.AccentBrush == Brushes.Red)
            {
                InfoCard.AccentBrush = Brushes.Transparent;
                return;
            }

            if (InfoCard.AccentBrush == Brushes.Transparent)
            {
                InfoCard.AccentBrush = Brushes.CornflowerBlue;
                return;
            }
        }
    }
}