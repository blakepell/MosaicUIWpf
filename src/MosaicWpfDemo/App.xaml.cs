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
using Mosaic.UI.Wpf;

namespace WpfDemo
{
    public partial class App : MosaicApp
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            ChangeTheme(Theme);
        }
    }
}
