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
using BbsNavigator.Common;
using BbsNavigator.Models;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;
using System.Windows;

namespace BbsNavigator
{
    /// <summary>
    /// Provides application startup, persistence, and theme initialization.
    /// </summary>
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        static App()
        {
            AppFolder = @"Apps\BBSNavigator";
            ProgId = "Mosaic_BBSNavigator";
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = AppServices.GetRequiredService<AppSettings>();

            if (settings.BbsProfiles.Count == 0)
            {
                settings.BbsProfiles.Add(new BbsProfile
                {
                    Name = "20 For Beers",
                    Host = "20forbeers.com",
                    Port = 1337,
                    Description = "Public Telnet BBS"
                });
                settings.BbsProfiles.Add(new BbsProfile
                {
                    Name = "The Cave BBS",
                    Host = "cavebbs.homeip.net",
                    Port = 23,
                    Description = "Classic multi-node BBS"
                });
                settings.BbsProfiles.Add(new BbsProfile
                {
                    Name = "Dark & Shattered Lands",
                    Host = "dsl-mud.org",
                    Port = 4000,
                    LocalEcho = true,
                    Description = "Dragonlance text based multiplayer game."
                });
            }

            var viewModel = AppServices.GetRequiredService<AppViewModel>();
            viewModel.AppSettings = settings;
            AppServices.GetRequiredService<ThemeManager>().Theme = settings.Theme;
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
