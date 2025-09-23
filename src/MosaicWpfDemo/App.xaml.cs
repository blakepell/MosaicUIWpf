/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf;
using System.Windows;
using MosaicWpfDemo.Common;

namespace WpfDemo
{
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            //ChangeTheme(ThemeMode.Dark);
        }
    }
}
