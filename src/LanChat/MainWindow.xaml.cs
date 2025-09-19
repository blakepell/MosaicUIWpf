/*
 * LanChat
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
using LanChat.Common;

namespace LanChat
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateMaximizeIcon();
            this.DataContext = AppServices.GetRequiredService<AppViewModel>();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TestSideMenu.SelectByIndex(0);

            var vm = AppServices.GetRequiredService<AppViewModel>();

            var ips = Argus.Network.Utilities.GetLocalIpAddresses();

            foreach (var ipAddress in ips)
            {
                if (ipAddress.ToString().StartsWith("192"))
                {
                    vm.LanIpAddress = ipAddress.ToString();
                    return;
                }
                else if (ipAddress.ToString().StartsWith("172"))
                {
                    vm.LanIpAddress = ipAddress.ToString();
                    return;
                }
            }

            vm.LanIpAddress = "127.0.0.1";
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
            App.ToggleTheme();
        }
    }
}