/*
 * ChromaSwap
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using ChromaSwap.Common;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;
using System.Windows;
using System.Windows.Threading;

namespace ChromaSwap
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        static App()
        {
            AppFolder = @"Apps\ChromaSwap";
            ProgId = "Mosaic_ChromaSwap";
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(e);

            var appSettings = AppServices.GetRequiredService<AppSettings>();

            var appViewModel = AppServices.GetRequiredService<AppViewModel>();
            appViewModel.AppSettings = appSettings;

            var themeManager = AppServices.GetRequiredService<ThemeManager>();
            themeManager.Theme = appSettings.Theme;
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            base.OnExit(e);
        }

        /// <summary>
        /// Last chance handler so an unexpected error surfaces to the user instead of
        /// terminating the application.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event data.</param>
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Mosaic.UI.Wpf.Controls.MessageBox.Show(e.Exception.Message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
