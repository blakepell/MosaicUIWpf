/*
 * LanChat
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using LanChat.Common;
using LanChat.Views;
using Mosaic.UI.Wpf.Themes;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Controls;
using System.Windows;
using System.Windows.Input;

namespace LanChat
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = AppServices.GetRequiredService<AppViewModel>();

            // When the window regains focus, put the cursor back in the chat command box.
            this.Activated += (_, _) => AppServices.GetService<ChatView>()?.FocusCommandBox();

            // Typing anywhere in the window (outside another text input) goes to the chat command box,
            // but only while the chat view is the visible one.
            this.PreviewTextInput += (_, e) =>
            {
                if (AppServices.GetService<ChatView>()?.TryRouteTextInput(e.Text) == true)
                {
                    e.Handled = true;
                }
            };
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TestSideMenu.SelectByIndex(0);

            var vm = AppServices.GetRequiredService<AppViewModel>();

            // This is a little hacky, the chat view should be loaded via the SelectedByIndex(0) call above.
            if (vm.AppSettings is { ConnectToLocalhostOnStartup: true, StartServerOnStartup: true } && !string.IsNullOrWhiteSpace(vm.AppSettings.Username))
            {
                var chatView = AppServices.GetService<ChatView>();
                chatView?.LoginClick();
            }
            else if (vm.AppSettings.AutoConnectWhenServerStarts)
            {
                // The server may have started before the chat view existed to see it.
                AppServices.GetService<ChatView>()?.AutoConnectToLocalServer();
            }

            var ips = Argus.Network.Utilities.GetLocalIpAddresses();

            foreach (var ipAddress in ips)
            {
                if (ipAddress.ToString().StartsWith("192"))
                {
                    vm.LanIpAddress = ipAddress.ToString();
                    return;
                }
                else if (ipAddress.ToString().StartsWith("172"))
                {
                    vm.LanIpAddress = ipAddress.ToString();
                    return;
                }
            }

            vm.LanIpAddress = "127.0.0.1";
        }

        private static readonly MosaicThemeMode[] ThemeCycle =
        {
            MosaicThemeMode.Light,
            MosaicThemeMode.Dark,
            MosaicThemeMode.Black,
            MosaicThemeMode.Blue
        };

        private void ButtonToggleTheme_OnClick(object sender, RoutedEventArgs e)
        {
            var theme = AppServices.GetRequiredService<ThemeManager>();
            var appSettings = AppServices.GetRequiredService<AppSettings>();

            // Advance to the next theme; an unlisted theme (e.g. HighContrast) restarts the cycle.
            var index = Array.IndexOf(ThemeCycle, theme.Theme);
            var next = ThemeCycle[(index + 1) % ThemeCycle.Length];

            theme.Theme = next;
            appSettings.Theme = next;
        }

        private void ShieldLanIp_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.CopyToClipboard(AppServices.GetRequiredService<AppViewModel>().LanIpAddress, "LAN IP address");
        }

        private void ShieldPort_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var port = AppServices.GetRequiredService<AppViewModel>().ChatServer?.Port;
            this.CopyToClipboard(port?.ToString(), "Port");
        }

        /// <summary>
        /// Copies text to the clipboard and shows a toast describing what was copied.
        /// </summary>
        private void CopyToClipboard(string? text, string description)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(text);
                ToastManager.ForElement(this)?.Show("Copied", $"{description} \"{text}\" copied to the clipboard.", ToastSeverity.Success, TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                // The clipboard can be locked by another process.
                var vm = AppServices.GetRequiredService<AppViewModel>();
                vm.StatusText = $"Unable to copy to clipboard: {ex.Message}";
            }
        }
    }
}
