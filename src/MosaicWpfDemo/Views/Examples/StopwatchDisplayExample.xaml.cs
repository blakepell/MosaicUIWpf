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
    public partial class StopwatchDisplayExample
    {
        public StopwatchDisplayExample()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        private void ButtonStart_OnClick(object sender, RoutedEventArgs e)
        {
            StopwatchDisplay.Start();
        }

        private void ButtonStop_OnClick(object sender, RoutedEventArgs e)
        {
            StopwatchDisplay.Stop();
        }

        private void ButtonHideMilliseconds_OnClick(object sender, RoutedEventArgs e)
        {
            StopwatchDisplay.ShowMilliseconds = false;
            ButtonHideMilliseconds.Visibility = Visibility.Collapsed;
            ButtonShowMilliseconds.Visibility = Visibility.Visible;
        }

        private void ButtonShowMilliseconds_OnClick(object sender, RoutedEventArgs e)
        {
            StopwatchDisplay.ShowMilliseconds = true;
            ButtonHideMilliseconds.Visibility = Visibility.Visible;
            ButtonShowMilliseconds.Visibility = Visibility.Collapsed;
        }

        private void ButtonReset_OnClick(object sender, RoutedEventArgs e)
        {
            StopwatchDisplay.Reset();
        }
    }
}