/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using Mosaic.UI.Wpf.Themes;
using MosaicWpfDemo.Common;
using System.Windows;

namespace WpfDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TestSideMenu.FocusSearchBoxOnLoad = true;
            TestSideMenu.SelectByIndex(1);
#if !DEBUG
            // Hide debugging tools in release builds without depending on menu order.
            foreach (var item in TestSideMenu.MenuItems)
            {
                if (item.Tag is "DebugOnly")
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }
#endif
        }

        private void ButtonToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            theme.CycleTheme();
            
            if (AppServices.IsRegistered<AppSettings>())
            {
                var appSettings = AppServices.GetRequiredService<AppSettings>();
                appSettings.Theme = theme.Theme;
            }
        }

        private void SideMenuItem_Click(object sender, Mosaic.UI.Wpf.Controls.SideMenuItemClickEventArgs e)
        {
            MessageBox.Show($"You clicked: {e.Item.Text}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
