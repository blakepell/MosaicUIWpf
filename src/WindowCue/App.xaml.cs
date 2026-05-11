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
using Mosaic.UI.Wpf;
using WindowCue.Common;
using WindowCue.Services;
using WindowCue.ViewModels;
using System.Windows;

namespace WindowCue
{
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            MosaicApp<AppSettings, AppViewModel>.AppFolder = @"Apps\WindowCue";
            MosaicApp<AppSettings, AppViewModel>.ProgId    = "WindowCue";

            base.OnStartup(e);

            // Register services
            var enumService    = new WindowEnumerationService();
            var focusService   = new WindowFocusService();
            var iconService    = new IconExtractionService();
            var dockingService = new ScreenDockingService();
            var dialogService  = new DialogService(enumService, iconService);
            var appBarService  = new AppBarService();

            AppServices.AddSingleton(enumService);
            AppServices.AddSingleton(focusService);
            AppServices.AddSingleton(iconService);
            AppServices.AddSingleton(dockingService);
            AppServices.AddSingleton(dialogService);
            AppServices.AddSingleton(appBarService);

            // Create and register the main ViewModel
            var mainVm = new MainWindowViewModel(
                enumService, focusService, iconService, dockingService, dialogService);
            AppServices.AddSingleton(mainVm);
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            // Persist pinned items back to AppSettings before MosaicApp saves them
            var mainVm   = AppServices.GetService<MainWindowViewModel>();
            var settings = AppServices.GetService<AppSettings>();

            if (mainVm != null && settings != null)
            {
                settings.PinnedItems       = mainVm.ToPersistedItems();
                settings.DockEdge          = mainVm.DockEdge.ToString();
                settings.MonitorDeviceName = mainVm.MonitorDeviceName;
            }

            // Release the shell AppBar reservation before the process exits so
            // the monitor work area is restored immediately.
            AppServices.GetService<AppBarService>()?.Unregister();

            base.OnExit(e);
        }
    }
}

