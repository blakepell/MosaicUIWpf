/*
 * MosaicTemplateApp
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using Argus.Memory;
using Mosaic.UI.Wpf.Themes;

namespace MosaicTemplateApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TestSideMenu.SelectByIndex(0);
        }

        private void ButtonToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            theme.ToggleTheme();
        }
    }
}
