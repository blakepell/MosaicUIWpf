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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace BbsNavigator
{
    /// <summary>
    /// Provides application startup, persistence, and theme initialization.
    /// </summary>
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        private const int ProfileBatchSize = 25;
        private IReadOnlyList<BbsProfile> _deferredProfiles = Array.Empty<BbsProfile>();
        private int _nextDeferredProfile;
        private Task? _profileLoadTask;

        static App()
        {
            AppFolder = @"Apps\BBSNavigator";
            ProgId = "Mosaic_BBSNavigator";

            // CP437 (classic DOS/ANSI art) lives in the code pages provider, which is not
            // registered by default on modern .NET.
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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
                    TerminalEncoding = BbsEncoding.Utf8,
                    Description = "Dragonlance text based multiplayer game."
                });
            }

            // Building hundreds of TreeView containers while the window is being created blocks
            // input. Keep the already-deserialized profiles aside and publish them in small,
            // background-priority batches after the window is visible.
            _deferredProfiles = settings.BbsProfiles.ToArray();
            settings.BbsProfiles = new ObservableCollection<BbsProfile>();

            var viewModel = AppServices.GetRequiredService<AppViewModel>();
            viewModel.AppSettings = settings;
            AppServices.GetRequiredService<ThemeManager>().Theme = settings.Theme;
        }

        /// <summary>
        /// Publishes persisted BBS profiles to the bound collection without monopolizing the UI
        /// thread. Input and menu work run ahead of each background-priority batch.
        /// </summary>
        public Task LoadBbsProfilesAsync(AppSettings settings)
        {
            return _profileLoadTask ??= LoadBbsProfilesCoreAsync(settings);
        }

        private async Task LoadBbsProfilesCoreAsync(AppSettings settings)
        {
            while (_nextDeferredProfile < _deferredProfiles.Count)
            {
                await Dispatcher.Yield(DispatcherPriority.Background);

                int end = Math.Min(_nextDeferredProfile + ProfileBatchSize, _deferredProfiles.Count);
                while (_nextDeferredProfile < end)
                {
                    settings.BbsProfiles.Add(_deferredProfiles[_nextDeferredProfile++]);
                }
            }
        }

        /// <summary>
        /// Ensures profiles that were still queued during shutdown are included in the settings
        /// file written by the base application host.
        /// </summary>
        private void CompleteDeferredProfileLoad()
        {
            var settings = AppServices.GetRequiredService<AppSettings>();

            while (_nextDeferredProfile < _deferredProfiles.Count)
            {
                settings.BbsProfiles.Add(_deferredProfiles[_nextDeferredProfile++]);
            }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            CompleteDeferredProfileLoad();
            base.OnExit(e);
        }
    }
}
