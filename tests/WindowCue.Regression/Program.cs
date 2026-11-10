/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using WindowCue;
using WindowCue.Interop;
using WindowCue.Models;
using WindowCue.Services;
using WindowCue.ViewModels;

/// <summary>Dependency-free Windows regression checks for stalled native work and target closure.</summary>
internal static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeMethods.RECT rect);

    [STAThread]
    private static int Main()
    {
        int exitCode = 0;
        var dispatcher = Dispatcher.CurrentDispatcher;
        dispatcher.InvokeAsync(async () =>
        {
            try { await RunAsync(); }
            catch (Exception ex) { Console.Error.WriteLine(ex); exitCode = 1; }
            finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
        });
        Dispatcher.Run();
        return exitCode;
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        Console.WriteLine($"PASS {name}");
    }

    private static async Task RunAsync()
    {
        int uiThread = Environment.CurrentManagedThreadId;
        var runner = new BackgroundOperationRunner();
        var thread = await runner.RunAsync(() =>
            (Id: Environment.CurrentManagedThreadId, Apartment: Thread.CurrentThread.GetApartmentState()));
        Check(thread.Completed && thread.Value.Id != uiThread && thread.Value.Apartment == ApartmentState.MTA,
            "Native work runs on a separate MTA thread");

        int ticks = 0;
        var heartbeat = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        heartbeat.Tick += (_, _) => ticks++;
        heartbeat.Start();
        using var release = new ManualResetEventSlim();
        int calls = 0;
        try
        {
            var stalled = runner.RunAsync(() => { Interlocked.Increment(ref calls); release.Wait(); return 1; },
                TimeSpan.FromMilliseconds(100));
            await Task.Delay(150);
            Check(!(await stalled).Completed && ticks > 2, "Dispatcher remains responsive while native work is stalled");
            bool rejectedAll = true;
            for (int i = 0; i < 50; i++)
                rejectedAll &= !(await runner.RunAsync(() => { Interlocked.Increment(ref calls); return 2; })).Completed;
            Check(rejectedAll, "Stalled lane rejects all 50 retries without queuing them");
            Check(calls == 1, "Timeouts and repeated clicks do not spawn additional hung workers");
        }
        finally { release.Set(); }
        var watch = Stopwatch.StartNew();
        BackgroundResult<int> recovered;
        do
        {
            await Task.Delay(10);
            recovered = await runner.RunAsync(() => 3);
        } while (!recovered.Completed && watch.Elapsed < TimeSpan.FromSeconds(2));
        Check(recovered.Completed && recovered.Value == 3, "Lane recovers after native work actually exits");
        Check(!(await runner.RunAsync<int>(() => throw new IOException("Simulated provider failure"))).Completed,
            "Provider exceptions return an inconclusive result");
        Check((await runner.RunAsync(() => true)).Completed, "Provider exceptions release the lane");

        using var releaseLate = new ManualResetEventSlim();
        int sideEffects = 0;
        try
        {
            var late = await runner.RunAsync(token =>
            {
                releaseLate.Wait();
                token.ThrowIfCancellationRequested();
                Interlocked.Increment(ref sideEffects);
                return true;
            }, TimeSpan.FromMilliseconds(60));
            Check(!late.Completed, "Activation wait is bounded");
        }
        finally { releaseLate.Set(); }
        await Task.Delay(50);
        Check(sideEffects == 0, "Expired inspection does not proceed to activation");

        var enumeration = new WindowEnumerationService();
        var icons = new IconExtractionService();
        var browser = new BrowserTabService();
        using var monitorRelease = new ManualResetEventSlim();
        using var pickerRelease = new ManualResetEventSlim();
        try
        {
            await enumeration.Work.RunAsync(() => { monitorRelease.Wait(); return 0; }, TimeSpan.FromMilliseconds(50));
            await browser.Work.RunAsync(() => { monitorRelease.Wait(); return 0; }, TimeSpan.FromMilliseconds(50));
            using var picker = new SelectWindowDialogViewModel(enumeration, icons, browser,
                () => [new WindowInfo { Title = "Presentation window", ProcessName = "slides" }],
                () => { pickerRelease.Wait(); return [new BrowserTabInfo { Title = "Presentation tab" }]; });
            var loading = picker.LoadWindowsAsync();
            await Task.Delay(150);
            Check(picker.FilteredWindows.Count == 1 && picker.IsLoading,
                "Picker lists desktop windows while browser discovery and monitoring are stalled");
            int resets = 0;
            picker.FilteredWindows.CollectionChanged += (_, e) =>
                resets += e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset ? 1 : 0;
            int before = ticks;
            await Task.Delay(3100);
            Check(!loading.IsCompleted && ticks > before,
                "Slow or debugger-paused picker discovery survives the three-second activation deadline");
            pickerRelease.Set();
            await loading;
            Check(picker.FilteredWindows.Count == 2 && resets == 0 && picker.LoadStatus.Length == 0,
                "Late browser results append without clearing existing picker selections");
        }
        finally { monitorRelease.Set(); pickerRelease.Set(); }
        await Task.Delay(50);

        using var closedPickerRelease = new ManualResetEventSlim();
        try
        {
            var picker = new SelectWindowDialogViewModel(enumeration, icons, browser,
                () => { closedPickerRelease.Wait(); return [new WindowInfo { Title = "Late window" }]; },
                () => []);
            var loading = picker.LoadWindowsAsync();
            picker.Dispose();
            await loading.WaitAsync(TimeSpan.FromSeconds(1));
            Check(picker.FilteredWindows.Count == 0 && !picker.IsLoading,
                "Closing the picker cancels its wait without waiting for a stuck provider");
        }
        finally { closedPickerRelease.Set(); }
        await Task.Delay(50);
        using (var picker = new SelectWindowDialogViewModel(enumeration, icons, browser,
            () => [new WindowInfo { Title = "Available window" }],
            () => throw new IOException("Simulated browser picker failure")))
        {
            await picker.LoadWindowsAsync();
            Check(picker.FilteredWindows.Count == 1 && picker.LoadStatus.Length > 0,
                "Failed browser discovery leaves windows visible and explains the incomplete scan");
        }
        var vm = new MainWindowViewModel(enumeration, new WindowFocusService(), icons,
            new ScreenDockingService(), new DialogService(enumeration, icons, browser), browser);
        vm.StopMonitoring();
        var missingTab = new ToolbarItemViewModel
        {
            TargetType = PinnedTargetType.BrowserTab,
            BrowserProcessName = "WindowCue_Regression_Absent_Browser",
            TabTitle = "Closed presentation", IsAvailable = true
        };
        vm.Items.Add(missingTab);
        var window = new MainWindow();
        const BindingFlags privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(MainWindow).GetField("_vm", privateInstance)!.SetValue(window, vm);
        var rebuild = typeof(MainWindow).GetMethod("RebuildSideMenuItems", privateInstance)!;
        missingTab.Icon = await icons.ExtractIconAsync(IntPtr.Zero, null);
        for (int i = 0; i < 10; i++)
            rebuild.Invoke(window, null);
        var handlers = (System.Collections.IDictionary)typeof(MainWindow)
            .GetField("_itemHandlers", privateInstance)!.GetValue(window)!;
        Check(handlers.Count == 1, "Repeated toolbar rebuilds retain one icon subscription per pin");
        bool wrongThread = false;
        missingTab.PropertyChanged += (_, _) => wrongThread |= Environment.CurrentManagedThreadId != uiThread;
        await vm.MonitorTargetsAsync();
        Check(!missingTab.IsAvailable && !wrongThread, "Confirmed browser closure grays the pin on the dispatcher");

        using var browserRelease = new ManualResetEventSlim();
        try
        {
            await browser.Work.RunAsync(() => { browserRelease.Wait(); return 0; }, TimeSpan.FromMilliseconds(50));
            // Start a fresh pin for the inconclusive-provider scenario.
            missingTab.TabClosed = false;
            missingTab.TabRuntimeId = [9, 8, 7];
            missingTab.TabProcessStartTimeUtcTicks = 100;
            missingTab.IsAvailable = true;
            await vm.MonitorTargetsAsync();
            Check(missingTab.IsAvailable, "Busy browser inspection never marks a live or dormant tab closed");
            await vm.FocusItemCommand.ExecuteAsync(missingTab);
            Check(!vm.FocusItemCommand.IsRunning && !missingTab.IsAvailable && missingTab.TabClosed,
                "Presentation clicks can inspect their target even while the periodic inspector is stuck");
        }
        finally { browserRelease.Set(); }

        var missingWindow = new ToolbarItemViewModel
        {
            ProcessName = "WindowCue_Regression_Absent_Process", WindowTitle = "Absent target",
            IsAvailable = true
        };
        vm.Items.Clear();
        vm.Items.Add(missingWindow);
        rebuild.Invoke(window, null);
        Check(handlers.Count == 1 && !handlers.Contains(missingTab), "Removed pins detach their icon subscriptions");
        using var windowsRelease = new ManualResetEventSlim();
        try
        {
            await enumeration.Work.RunAsync(() => { windowsRelease.Wait(); return 0; }, TimeSpan.FromMilliseconds(50));
            int before = ticks;
            await vm.MonitorTargetsAsync();
            await Task.Delay(60);
            Check(missingWindow.IsAvailable && ticks > before, "Stalled process reconciliation leaves the dispatcher responsive");
        }
        finally { windowsRelease.Set(); }
        await Task.Delay(30);
        await vm.MonitorTargetsAsync();
        Check(!missingWindow.IsAvailable, "A missing desktop target is reconciled off the UI thread");

        var sleeping = new BrowserTabInfo { Title = "Presentation - Sleeping", RuntimeId = [1, 2, 3] };
        Check(browser.MatchTab([sleeping], "Presentation", null, [1, 2, 3]) == sleeping,
            "Sleeping tab identity survives renderer/title changes");
        var replacement = new BrowserTabInfo { Title = sleeping.Title, Url = "https://same.example",
            RuntimeId = [4, 5, 6], ProcessStartTimeUtcTicks = 200 };
        Check(browser.MatchTab([replacement], sleeping.Title, replacement.Url, [1, 2, 3]) == null,
            "Closed tab cannot attach to a replacement with the same title or URL");
        Check(browser.MatchTab([replacement], "Presentation", null) == null,
            "Legacy title-only pins cannot attach to a new browser tab");
        Check(browser.MatchTab([replacement], replacement.Title, replacement.Url, [4, 5, 6],
            processStartTimeUtcTicks: 100) == null, "Reused runtime IDs in a restarted browser are rejected");
        sleeping.ProcessStartTimeUtcTicks = 100;
        sleeping.WindowHandle = (IntPtr)999;
        Check(browser.MatchTab([sleeping], "Old title", null, [1, 2, 3], (IntPtr)123, 100) == sleeping,
            "Original sleeping tab can move windows without changing its pin identity");
        missingTab.TabClosed = true;
        missingTab.IsAvailable = false;
        typeof(MainWindowViewModel).GetMethod("ApplyTabProbe", privateInstance | BindingFlags.Static)!
            .Invoke(null, [missingTab, sleeping, true]);
        Check(!missingTab.IsAvailable, "Confirmed closed pin stays gray even if an identity is later reused");
        vm.Items.Clear();
        var originalPin = ToolbarItemViewModel.FromBrowserTab(sleeping);
        vm.Items.Add(originalPin);
        var saved = System.Text.Json.JsonSerializer.Deserialize<List<PinnedItemData>>(
            System.Text.Json.JsonSerializer.Serialize(vm.ToPersistedItems()))![0];
        Check(saved.TabRuntimeId!.SequenceEqual(sleeping.RuntimeId!) && saved.TabProcessStartTimeUtcTicks == 100,
            "Persistence retains original tab and browser-lifetime identities");
        Check(window.Title.Length == 0, "Toolbar title is empty");
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
        window.Height = 600;
        window.Width = ScreenDockingService.ToolbarWidth;
        window.UpdateLayout();
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window);
        Check(GetWindowRect(hwnd, out var bounds) &&
            Math.Abs((bounds.Right - bounds.Left) / dpi.DpiScaleX - ScreenDockingService.ToolbarWidth) < 1,
            $"Native toolbar width matches one button (requested {window.Width}, actual {(bounds.Right - bounds.Left) / dpi.DpiScaleX})");
        window.Close();
        heartbeat.Stop();
        Console.WriteLine("All WindowCue responsiveness regressions passed.");
    }
}
