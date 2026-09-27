/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using BbsNavigator.Common;
using BbsNavigator.Models;
using BbsNavigator.Views;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Scripting;
using Mosaic.UI.Wpf.Themes;
using Xunit;

namespace BbsNavigator.Tests;

public class OnConnectedEventTests
{
    static OnConnectedEventTests()
    {
        var style = new Style(typeof(BbsTerminalView));
        style.Resources[MosaicTheme.ControlBorderBrush] = Brushes.Gray;
        style.Resources[MosaicTheme.SuccessBrush] = Brushes.Green;
        style.Resources[MosaicTheme.WarningBrush] = Brushes.Orange;
        style.Resources[MosaicTheme.ErrorBrush] = Brushes.Red;
        style.Seal();
        FrameworkElement.StyleProperty.OverrideMetadata(typeof(BbsTerminalView), new FrameworkPropertyMetadata(style));
    }
    [Theory]
    [InlineData(null, 0, 0)]
    [InlineData("", 0, 0)]
    [InlineData("  \r\n  ", 0, 0)]
    [InlineData("probe.Add(term.Name);", 1, 0)]
    [InlineData("throw new Error('connection script failed');", 0, 1)]
    [InlineData("let = ;", 0, 1)]
    public void SuccessfulConnectionRunsScriptWithItsTerminalAndContainsFailures(string? code, int calls, int errors)
    {
        RunStaAsync(async () =>
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var profile = new BbsProfile
            {
                Name = "Event test",
                Host = "127.0.0.1",
                Port = ((IPEndPoint)listener.LocalEndpoint).Port,
                AutoReconnect = false,
                TerminalEncoding = BbsEncoding.Utf8,
                OnConnectedEvent = code
            };
            var probe = new List<string>();
            await using var terminal = new BbsTerminalView(profile, new AppSettings());
            terminal.ConfigureScriptEnvironment = environment => environment.RegisterObject("probe", probe);
            var host = new AdornerDecorator { Child = terminal };
            host.Measure(new Size(800, 600));
            host.Arrange(new Rect(0, 0, 800, 600));
            var toasts = ToastManager.ForElement(terminal);
            Assert.NotNull(toasts);

            await terminal.ConnectAsync();
            using var peer = await listener.AcceptTcpClientAsync();

            Assert.Equal(BbsConnectionState.Connected, profile.ConnectionState);
            Assert.Equal(1, profile.ConnectionCount);
            Assert.Equal(calls, probe.Count);
            Assert.All(probe, name => Assert.Equal(profile.Name, name));
            Assert.Equal(errors, toasts.ActiveCount);
            toasts.DismissAll();
        });
    }

    [Fact]
    public void EditorSavesEventToProfileAndJsonPreservesIt()
    {
        RunStaAsync(async () =>
        {
            var profile = new BbsProfile();
            var editor = new ScriptEditorControl
            {
                SaveObject = profile,
                SaveToProperty = nameof(BbsProfile.OnConnectedEvent),
                Text = "term.Echo('Connected');"
            };

            Assert.Null(profile.OnConnectedEvent);
            await editor.SaveAsync();
            Assert.Equal(editor.Text, profile.OnConnectedEvent);
            var restored = JsonSerializer.Deserialize<BbsProfile>(JsonSerializer.Serialize(profile));
            Assert.Equal(profile.OnConnectedEvent, restored!.OnConnectedEvent);

            editor.Text = string.Empty;
            await editor.SaveAsync();
            Assert.Equal(string.Empty, profile.OnConnectedEvent);
        });
    }

    private static void RunStaAsync(Func<Task> action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                }
            }));
            Dispatcher.Run();
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The connection event test did not finish.");
        if (failure != null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
