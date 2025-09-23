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
using Argus.Memory;
using Mosaic.UI.Wpf.Themes;

namespace WpfDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateMaximizeIcon();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TestSideMenu.SelectByIndex(0);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }

            UpdateMaximizeIcon();
        }

        private void UpdateMaximizeIcon()
        {
            if (MaxRestoreButton == null)
            {
                return;
            }

            MaxRestoreButton.Content = WindowState == WindowState.Maximized ? "\xE923" : "\xE922";
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            UpdateMaximizeIcon();
        }

        private void ButtonToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            theme.ToggleTheme();
        }
    }
}