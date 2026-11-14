/*
 * LanChat
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using LanChat.Common;
using Mosaic.UI.Wpf;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using Argus.Memory;
using LanChat.Network;
using Mosaic.UI.Wpf.Themes;

namespace LanChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(e);

            var vm = AppServices.GetRequiredService<AppViewModel>();
            var appSettings = AppServices.GetRequiredService<AppSettings>();
            vm.AppSettings = appSettings;

            //var theme = AppServices.GetRequiredService<ThemeManager>();
            //theme.Theme = vm.AppSettings.Theme;

            var server = new ChatServer(new ChatServerOptions
            {
                Port = appSettings.ServerPort is > 0 and <= 65535 ? appSettings.ServerPort : 4000,
                Name = Environment.MachineName,
            });

            //server.TextReceived += (s, e) =>
            //{
            //    Console.WriteLine($"[TEXT from {e.ClientId}]: {e.Text}");
            //};

            //server.EnvelopeReceived += (s, e) =>
            //{
            //    Console.WriteLine($"[ENV from {e.ClientId}]: {e.Envelope.TypeName} -> {e.Envelope.Json}");
            //};

            server.ClientConnected += (o, args) =>
            {
                vm.UsersConnected++;
            };

            server.ClientDisconnected += (o, args) =>
            {
                vm.UsersConnected--;
            };

            // Put our server into the DI service collection so we can access it later.
            AppServices.AddSingleton(server);
            vm.ChatServer = server;

            if (appSettings.StartServerOnStartup)
            {
                try
                {
                    await server.StartAsync();
                }
                catch (Exception ex)
                {
                    // Don't block startup: the chat client works without a local server (e.g. the port is in use by another instance).
                    vm.StatusText = $"Could not start server on port {server.Port}: {ex.Message}";
                }
            }
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            // Call the shared exit.
            base.OnExit(e);
        }
    }

}
