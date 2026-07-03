/*
 * MosaicTextEditor
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Diagnostics;
using Argus.Memory;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;
using MosaicTextEditor.Common;
using System.Windows;

namespace MosaicTextEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        public static Stopwatch StartupTime = new();

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            StartupTime.Start();

            base.OnStartup(e);

            var appSettings = AppServices.GetRequiredService<AppSettings>();

            var appViewModel = AppServices.GetRequiredService<AppViewModel>();
            appViewModel.AppSettings = appSettings;
            appViewModel.StartupPath = e.Args.FirstOrDefault();

            var themeManager = AppServices.GetRequiredService<ThemeManager>();
            themeManager.Theme = appSettings.Theme;
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            // Call the shared exit.
            base.OnExit(e);
        }
    }

}
