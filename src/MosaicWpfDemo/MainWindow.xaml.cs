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
using Argus.Memory;
using Mosaic.UI.Wpf.Themes;
using MosaicWpfDemo.Common;

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
#if DEBUG
            // Makes it easier to search when debugging.
            TestSideMenu.FocusSearchBoxOnLoad = true;
            TestSideMenu.SelectByIndex(1);
#else
            TestSideMenu.MenuItems[1].Visibility = Visibility.Collapsed;
            TestSideMenu.SelectByIndex(0);
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
    }
}
