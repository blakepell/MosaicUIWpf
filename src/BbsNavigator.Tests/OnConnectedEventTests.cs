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

    /// <summary>
    /// Verifies incoming lines reach trigger scripts with their originating terminal reference.
    /// </summary>
    [Fact]
    public void ReceivedLineRunsTriggerAndSendsCapturedTextThroughTerm()
    {
        RunStaAsync(async () =>
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var profile = new BbsProfile
            {
                Name = "Trigger connection",
                Host = "127.0.0.1",
                Port = ((IPEndPoint)listener.LocalEndpoint).Port,
                AutoReconnect = false,
                TerminalEncoding = BbsEncoding.Utf8
            };
            var trigger = new BbsNavigator.Models.Trigger
            {
                Name = "Collect item",
                Pattern = @"^You see (.+)\.$",
                Command = "term.SendLine(\"get %1\"); term.Status(\"%0\");",
                DisableAfterTriggered = true
            };
            profile.Triggers.Add(trigger);
            await using var terminal = new BbsTerminalView(profile, new AppSettings());
            await terminal.ConnectAsync();
            using var peer = await listener.AcceptTcpClientAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var stream = peer.GetStream();
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("\x1b[3"), deadline.Token);
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("2mYou see sword.\x1b[0m"), deadline.Token);
            await Task.Delay(50, deadline.Token);
            Assert.Equal(0, trigger.Count);
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("\r\n"), deadline.Token);
            var received = new System.Text.StringBuilder();
            var buffer = new byte[1024];
            while (!received.ToString().Contains("get sword\r", StringComparison.Ordinal))
            {
                int count = await stream.ReadAsync(buffer, deadline.Token);
                Assert.True(count > 0);
                received.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, count));
            }

            Assert.Equal(1, trigger.Count);
            Assert.False(trigger.Enabled);
        });
    }

    /// <summary>
    /// Verifies trigger windows load their script editor and list bindings on the dispatcher.
    /// </summary>
    [Fact]
    public void TriggerWindowsLoadWithScriptCompletionAndObservableItems()
    {
        RunStaAsync(() =>
        {
            var profile = new BbsProfile();
            var trigger = new BbsNavigator.Models.Trigger { Name = "Example", Pattern = "(item)", Command = "term.Echo(\"%1\");" };
            profile.Triggers.Add(trigger);
            var list = new TriggerListWindow(profile);
            var editor = new TriggerEditorWindow((BbsNavigator.Models.Trigger)trigger.Clone());
            var items = Assert.IsType<System.Windows.Controls.ListView>(list.FindName("TriggerList"));
            Assert.Same(profile.Triggers, items.ItemsSource);
            var scriptEditor = Assert.IsType<ScriptEditorControl>(editor.FindName("ScriptEditor"));
            Assert.NotNull(scriptEditor.Environment);
            Assert.Equal(trigger.Command, scriptEditor.Text);
            editor.Close();
            list.Close();
            return Task.CompletedTask;
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
