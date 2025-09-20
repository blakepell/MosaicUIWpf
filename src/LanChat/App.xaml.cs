/*
 * LanChat
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using LanChat.Common;
using Mosaic.UI.Wpf;
using System.Windows;
using Argus.Memory;
using LanChat.Network;

namespace LanChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : MosaicApp<AppSettings, AppViewModel>
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(e);

            ChangeTheme("Dark");

            var vm = AppServices.GetRequiredService<AppViewModel>();

            var server = new ChatServer(new ChatServerOptions
            {
                Port = 4000,
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
        }

        private void App_OnExit(object sender, ExitEventArgs e)
        {
            // Call the shared exit.
            base.OnExit(e);
        }
    }

}
