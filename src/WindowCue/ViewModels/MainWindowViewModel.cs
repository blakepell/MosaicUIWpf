/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowCue.Interop;
using WindowCue.Models;
using WindowCue.Services;

namespace WindowCue.ViewModels
{
    /// <summary>
    /// Root ViewModel for the WindowCue toolbar window.
    /// Manages the collection of pinned windows, docking state, and toolbar commands.
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly WindowEnumerationService _enumService;
        private readonly WindowFocusService _focusService;
        private readonly IconExtractionService _iconService;
        private readonly ScreenDockingService _dockingService;
        private readonly DialogService _dialogService;
        private readonly BrowserTabService _browserTabService;
        private readonly DispatcherTimer _processMonitorTimer;
        private bool _isMonitoring;
        private int _tabActivationVersion;

        /// <summary>The ordered collection of pinned toolbar items.</summary>
        public ObservableCollection<ToolbarItemViewModel> Items { get; } = new();

        /// <summary>Current dock edge used to re-apply docking when the edge changes.</summary>
        [ObservableProperty]
        private DockEdge _dockEdge = DockEdge.Left;

        /// <summary>
        /// True while pinned items are being restored from persisted settings on a background task.
        /// Bind a loading indicator's visibility to this property.
        /// </summary>
        [ObservableProperty]
        private bool _isRestoringPinnedItems;

        /// <summary>Device name of the monitor the toolbar is docked to.</summary>
        [ObservableProperty]
        private string? _monitorDeviceName;

        public MainWindowViewModel(
            WindowEnumerationService enumService,
            WindowFocusService focusService,
            IconExtractionService iconService,
            ScreenDockingService dockingService,
            DialogService dialogService,
            BrowserTabService browserTabService)
        {
            _enumService = enumService;
            _focusService = focusService;
            _iconService = iconService;
            _dockingService = dockingService;
            _dialogService = dialogService;
            _browserTabService = browserTabService;

            _processMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _processMonitorTimer.Tick += OnProcessMonitorTick;
            _processMonitorTimer.Start();
        }

        /// <summary>Stops scheduling inspections when the toolbar closes.</summary>
        public void StopMonitoring() => _processMonitorTimer.Stop();

        // ── Add ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the Select Window/Tab dialog and adds the chosen item to the toolbar.
        /// Regular windows are handled through the existing HWND path; browser tabs are
        /// handled through the <see cref="BrowserTabService"/> path.
        /// </summary>
        [RelayCommand]
        private async Task AddWindowAsync()
        {
            var window = Application.Current.MainWindow;
            if (window == null)
            {
                return;
            }

            var results = await _dialogService.ShowSelectWindowDialogAsync(window);
            if (results == null || results.Count == 0)
            {
                return;
            }

            foreach (var result in results)
            {
                ToolbarItemViewModel vm;

                if (result.BrowserTab != null)
                {
                    // Browser tab path
                    var icon = await _iconService.ExtractIconAsync(result.BrowserTab.WindowHandle, result.BrowserTab.ExecutablePath);
                    result.BrowserTab.Icon = icon;
                    vm = ToolbarItemViewModel.FromBrowserTab(result.BrowserTab);
                }
                else if (result.Window != null)
                {
                    // Regular desktop window path
                    var icon = await _iconService.ExtractIconAsync(result.Window.Handle, result.Window.ExecutablePath);
                    result.Window.Icon = icon;
                    vm = ToolbarItemViewModel.FromWindowInfo(result.Window);
                }
                else
                {
                    continue;
                }

                Items.Add(vm);
            }
        }

        // ── Focus ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Focuses (and restores if minimized) the target window or browser tab.
        /// Browser-tab items are activated via <see cref="BrowserTabService"/>; regular
        /// window items use the existing HWND / PID focus path.
        /// </summary>
        [RelayCommand]
        private async Task FocusItemAsync(ToolbarItemViewModel? item)
        {
            if (item == null)
            {
                return;
            }

            // ── Browser-tab path ─────────────────────────────────────────────
            if (item.TargetType == PinnedTargetType.BrowserTab)
            {
                _tabActivationVersion++;
                if (item.TabClosed || item.TabRuntimeId == null || item.TabProcessStartTimeUtcTicks == 0)
                {
                    item.IsAvailable = false;
                    item.UnavailableReason = "Original tab is closed or its identity was not saved. Pin the desired tab again.";
                    return;
                }
                // Capture immutable input before leaving the dispatcher; workers never touch VMs.
                var browser = item.BrowserProcessName;
                var handle = item.WindowHandle;
                var title = item.TabTitle;
                var url = item.TabUrl;
                var runtimeId = item.TabRuntimeId;
                var started = item.TabProcessStartTimeUtcTicks;
                var result = await _browserTabService.ActivationWork.RunAsync(token =>
                {
                    // Try the known window first for quick switching; only search other windows
                    // if the original tab may have moved. Never foreground an unmatched browser.
                    var tabs = _browserTabService.GetOpenTabs(browser, out bool complete, handle);
                    var tab = _browserTabService.MatchTab(tabs, title, url, runtimeId, handle, started);
                    if (tab == null && handle != IntPtr.Zero)
                    {
                        tabs = _browserTabService.GetOpenTabs(browser, out complete);
                        tab = _browserTabService.MatchTab(tabs, title, url, runtimeId, handle, started);
                    }
                    token.ThrowIfCancellationRequested();
                    bool focused = tab != null && _browserTabService.FocusTab(tab);
                    return (tab, complete, focused);
                });
                if (!Items.Contains(item))
                    return;
                if (!result.Completed)
                {
                    item.UnavailableReason = "Browser is busy or not responding. Click to retry.";
                    return;
                }
                var probe = result.Value;
                ApplyTabProbe(item, probe.tab, probe.complete);
                if (probe.tab != null && !probe.focused)
                    item.UnavailableReason = "Tab is still open. Click again to retry activation.";

                return;
            }

            // Native focus, process lookup and window enumeration all stay off the dispatcher.
            var windowHandle = item.WindowHandle;
            var pid = item.ProcessId;
            var path = item.ExecutablePath;
            var processName = item.ProcessName;
            var windowTitle = item.WindowTitle;
            var windowResult = await _enumService.Work.RunAsync(token =>
            {
                if (_focusService.FocusWindow(windowHandle))
                    return (Handle: windowHandle, Match: (WindowInfo?)null);
                var freshHandle = _focusService.FindWindowForProcess(pid);
                if (freshHandle != IntPtr.Zero && _focusService.FocusWindow(freshHandle))
                    return (Handle: freshHandle, Match: (WindowInfo?)null);
                var match = _enumService.TryRebind(path, processName, windowTitle);
                token.ThrowIfCancellationRequested();
                if (match != null)
                    _focusService.FocusWindow(match.Handle);
                return (Handle: match?.Handle ?? IntPtr.Zero, Match: match);
            });
            if (!Items.Contains(item) || !windowResult.Completed)
                return;
            if (windowResult.Value.Match is { } matched)
                ApplyWindowMatch(item, matched);
            else
            {
                item.WindowHandle = windowResult.Value.Handle;
                item.IsAvailable = windowResult.Value.Handle != IntPtr.Zero;
                item.UnavailableReason = item.IsAvailable ? null : "Window is no longer available.";
            }
        }

        /// <summary>Applies a plain window snapshot on the dispatcher without native calls.</summary>
        private static void ApplyWindowMatch(ToolbarItemViewModel item, WindowInfo match)
        {
            item.WindowHandle = match.Handle;
            item.ProcessId = match.ProcessId;
            item.WindowTitle = match.Title;
            item.ProcessName = match.ProcessName;
            item.ExecutablePath = match.ExecutablePath;
            item.IsAvailable = true;
            item.UnavailableReason = null;
        }

        // ── Process monitor ───────────────────────────────────────────────────

        /// <summary>Applies presence separately from activation success.</summary>
        private static void ApplyTabProbe(ToolbarItemViewModel item, BrowserTabInfo? tab, bool complete)
        {
            if (item.TabClosed)
            {
                item.IsAvailable = false;
                return;
            }
            if (tab != null)
            {
                item.WindowHandle = tab.WindowHandle;
                item.ProcessId = tab.ProcessId;
                item.TabRuntimeId = tab.RuntimeId;
                item.TabProcessStartTimeUtcTicks = tab.ProcessStartTimeUtcTicks;
                item.TabTitle = tab.Title;
                item.TabUrl = tab.Url ?? item.TabUrl;
                item.ExecutablePath = tab.ExecutablePath ?? item.ExecutablePath;
                item.IsAvailable = true;
                item.UnavailableReason = null;
            }
            else
            {
                // Inaccessible/minimized/rebuilding UI is uncertainty, not proof of closure.
                if (complete)
                {
                    item.TabClosed = true;
                    item.IsAvailable = false;
                }
                item.UnavailableReason = complete ? "Browser tab is no longer open." :
                    "Browser tabs could not be inspected. Click to retry.";
            }
        }

        /// <summary>
        /// Reconciles pinned targets without activating them. Browser snapshots are shared per
        /// browser and inspected on a worker thread; overlapping timer ticks are skipped.
        /// </summary>
        private async void OnProcessMonitorTick(object? sender, EventArgs e) => await MonitorTargetsAsync();

        /// <summary>Inspects a single snapshot; only result application touches observable state.</summary>
        internal async Task MonitorTargetsAsync()
        {
            if (_isMonitoring || IsRestoringPinnedItems || FocusItemCommand.IsRunning)
            {
                return;
            }

            _isMonitoring = true;
            try
            {
                var items = Items.ToList();
                foreach (var group in items.Where(i => i.TargetType == PinnedTargetType.BrowserTab)
                    .Where(i => !i.TabClosed)
                    .GroupBy(i => i.BrowserProcessName))
                {
                    var browser = group.Key;
                    int activationVersion = _tabActivationVersion;
                    var result = await _browserTabService.Work.RunAsync(() =>
                    {
                        var tabs = _browserTabService.GetOpenTabs(browser, out bool complete);
                        return (tabs, complete);
                    });
                    if (!result.Completed || activationVersion != _tabActivationVersion)
                        continue;
                    var snapshot = result.Value;
                    foreach (var item in group)
                    {
                        if (!Items.Contains(item) || FocusItemCommand.IsRunning)
                        {
                            continue;
                        }

                        ApplyTabProbe(item, _browserTabService.MatchTab(snapshot.tabs,
                            item.TabTitle, item.TabUrl, item.TabRuntimeId, item.WindowHandle, item.TabProcessStartTimeUtcTicks), snapshot.complete);
                    }
                }
                var windows = items.Where(i => i.TargetType == PinnedTargetType.Window)
                    .Select(i => (Item: i, Handle: i.WindowHandle, Path: i.ExecutablePath,
                        Process: i.ProcessName, Title: i.WindowTitle)).ToList();
                if (windows.Count == 0)
                    return;
                var windowResult = await _enumService.Work.RunAsync(() =>
                {
                    var missing = windows.Where(w => !_focusService.IsWindowAlive(w.Handle)).ToList();
                    // Enumerate at most once per tick, even if many pinned targets closed together.
                    var live = missing.Count > 0 ? _enumService.GetVisibleWindows(includeCommandLine: false) : [];
                    return windows.Select(w => (w.Item, Alive: !missing.Contains(w),
                        Match: missing.Contains(w) ? _enumService.FindBestMatch(live, w.Path, w.Process, w.Title, out _) : null))
                        .ToList();
                });
                if (!windowResult.Completed)
                    return;
                foreach (var status in windowResult.Value!)
                {
                    if (!Items.Contains(status.Item) || FocusItemCommand.IsRunning)
                        continue;
                    if (status.Match is { } match)
                        ApplyWindowMatch(status.Item, match);
                    else
                    {
                        status.Item.IsAvailable = status.Alive;
                        status.Item.UnavailableReason = status.Alive ? null : "Window is no longer available.";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WindowCue target inspection failed: {ex}");
            }
            finally
            {
                _isMonitoring = false;
            }
        }

        // ── Remove ────────────────────────────────────────────────────────────

        /// <summary>Removes a pinned item from the toolbar.</summary>
        [RelayCommand]
        private void RemoveItem(ToolbarItemViewModel? item)
        {
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        // ── Rename ────────────────────────────────────────────────────────────

        /// <summary>Opens the rename dialog and applies the new label.</summary>
        [RelayCommand]
        private void RenameItem(ToolbarItemViewModel? item)
        {
            if (item == null)
            {
                return;
            }

            var window = Application.Current.MainWindow;
            if (window == null)
            {
                return;
            }

            string? newLabel = _dialogService.ShowRenameDialog(item.Label, window);
            if (newLabel != null)
            {
                item.Label = newLabel;
            }
        }

        // ── Properties ───────────────────────────────────────────────────────

        /// <summary>Opens the properties dialog for the item.</summary>
        [RelayCommand]
        private void ShowItemProperties(ToolbarItemViewModel? item)
        {
            if (item == null)
            {
                return;
            }

            var window = Application.Current.MainWindow;
            if (window == null)
            {
                return;
            }

            _dialogService.ShowPropertiesDialog(item, window);
        }

        // ── Pin Foreground Window (hotkey) ────────────────────────────────────

        /// <summary>
        /// Pins the currently focused desktop window to the toolbar if it is not already pinned.
        /// Intended to be triggered by a system-wide hotkey.
        /// </summary>
        [RelayCommand]
        private async Task PinForegroundWindowAsync()
        {
            // Capture the foreground handle before scheduling work so the hotkey pins the
            // window that was active when pressed, even if focus changes during inspection.
            var foreground = NativeMethods.GetForegroundWindow();
            var inspection = await _enumService.Work.RunAsync(() =>
            {
                var hWnd = foreground;

                if (hWnd == IntPtr.Zero || !NativeMethods.IsWindow(hWnd) || !NativeMethods.IsWindowVisible(hWnd))
                {
                    return (WindowInfo?)null;
                }

                // Skip WindowCue's own windows.
                NativeMethods.GetWindowThreadProcessId(hWnd, out int pid);
                if (pid == 0 || pid == Environment.ProcessId)
                {
                    return (WindowInfo?)null;
                }

                // Build WindowInfo from the live handle.
                int titleLen = NativeMethods.GetWindowTextLength(hWnd);
                var sb = new StringBuilder(titleLen + 1);
                NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                if (string.IsNullOrWhiteSpace(title))
                {
                    return (WindowInfo?)null;
                }

                string processName = string.Empty;
                string? executablePath = null;

                try
                {
                    using var proc = Process.GetProcessById(pid);
                    processName = proc.ProcessName;
                    try { executablePath = proc.MainModule?.FileName; } catch { /* access denied */ }
                }
                catch
                {
                    return (WindowInfo?)null;
                }

                if (WindowEnumerationService.IgnoreList.Contains(processName))
                {
                    return (WindowInfo?)null;
                }

                return new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessId = pid,
                    ProcessName = processName,
                    ExecutablePath = executablePath
                };

            });
            var info = inspection.Value;
            if (!inspection.Completed || info == null || Items.Any(i => i.ProcessId == info.ProcessId))
                return;
            var icon = await _iconService.ExtractIconAsync(info.Handle, info.ExecutablePath);
            info.Icon = icon;

            var vm = ToolbarItemViewModel.FromWindowInfo(info);
            Items.Add(vm);
        }

        // ── Dock ──────────────────────────────────────────────────────────────

        /// <summary>Changes the dock edge and repositions the toolbar window.</summary>
        [RelayCommand]
        private void SetDockEdge(string edgeName)
        {
            if (!Enum.TryParse<DockEdge>(edgeName, out var edge))
            {
                return;
            }

            DockEdge = edge;
            var window = Application.Current.MainWindow;
            if (window != null)
            {
                _dockingService.Dock(window, DockEdge, MonitorDeviceName);
            }
        }

        // ── Persistence helpers ───────────────────────────────────────────────

        /// <summary>
        /// Converts current items to serialization-friendly <see cref="PinnedItemData"/> records.
        /// </summary>
        public List<PinnedItemData> ToPersistedItems() =>
            Items.Select(i => new PinnedItemData
            {
                TargetType = i.TargetType,
                ProcessId = i.ProcessId,
                Label = i.Label,
                ProcessName = i.ProcessName,
                ExecutablePath = i.ExecutablePath,
                WindowTitle = i.WindowTitle,
                TabRuntimeId = i.TabRuntimeId,
                TabProcessStartTimeUtcTicks = i.TabProcessStartTimeUtcTicks,
                TabClosed = i.TabClosed,
                TabTitle = i.TabTitle,
                TabUrl = i.TabUrl,
                BrowserProcessName = i.BrowserProcessName
            }).ToList();

        /// <summary>
        /// Populates the toolbar from persisted data, attempting to rebind each item to a
        /// currently-running window. Items that cannot be rebound are shown as unavailable.
        /// </summary>
        public async Task RestoreFromPersistedItemsAsync(IEnumerable<PinnedItemData> saved)
        {
            IsRestoringPinnedItems = true;
            try
            {
                foreach (var data in saved)
                {
                    // ── Browser-tab restore path ──────────────────────────────────
                    if (data.TargetType == PinnedTargetType.BrowserTab)
                    {
                        var result = await _browserTabService.Work.RunAsync(() =>
                        {
                            if (data.TabClosed || data.TabRuntimeId == null || data.TabProcessStartTimeUtcTicks == 0)
                                return (tab: (BrowserTabInfo?)null, complete: true);
                            var tabs = _browserTabService.GetOpenTabs(data.BrowserProcessName, out bool complete);
                            return (tab: _browserTabService.MatchTab(tabs, data.TabTitle, data.TabUrl,
                                data.TabRuntimeId, processStartTimeUtcTicks: data.TabProcessStartTimeUtcTicks), complete);
                        });
                        var probe = result.Completed ? result.Value : (tab: (BrowserTabInfo?)null, complete: false);
                        var tab = probe.tab;

                        ToolbarItemViewModel tabVm;
                        if (tab != null)
                        {
                            tab.Icon = await _iconService.ExtractIconAsync(tab.WindowHandle, tab.ExecutablePath);
                            tabVm = ToolbarItemViewModel.FromBrowserTab(tab, data.Label);
                        }
                        else
                        {
                            tabVm = new ToolbarItemViewModel
                            {
                                Label = data.Label,
                                ProcessName = data.BrowserProcessName,
                                TargetType = PinnedTargetType.BrowserTab,
                                TabRuntimeId = data.TabRuntimeId,
                                TabProcessStartTimeUtcTicks = data.TabProcessStartTimeUtcTicks,
                                TabClosed = data.TabClosed,
                                TabTitle = data.TabTitle,
                                TabUrl = data.TabUrl,
                                BrowserProcessName = data.BrowserProcessName,
                                IsAvailable = !probe.complete,
                                UnavailableReason = "Browser tab is not currently open."
                            };
                            var tabIcon = await TryExtractIconWithoutHandleAsync(data.ExecutablePath, data.BrowserProcessName);
                            if (tabIcon != null)
                            {
                                tabVm.Icon = tabIcon;
                            }
                        }

                        tabVm.TabUrl ??= data.TabUrl;
                        ApplyTabProbe(tabVm, tab, probe.complete);
                        Items.Add(tabVm);
                        continue;
                    }

                    // ── Regular window restore path ───────────────────────────────
                    var inspection = await _enumService.Work.RunAsync(() =>
                        _enumService.TryRebind(data.ExecutablePath, data.ProcessName, data.WindowTitle));
                    var match = inspection.Value;

                    ToolbarItemViewModel vm;
                    if (match != null)
                    {
                        match.Icon = await _iconService.ExtractIconAsync(match.Handle, match.ExecutablePath);
                        vm = ToolbarItemViewModel.FromWindowInfo(match, data.Label);
                    }
                    else
                    {
                        // Show as unavailable placeholder so the user can remove or wait
                        vm = new ToolbarItemViewModel
                        {
                            Label = data.Label,
                            ProcessName = data.ProcessName,
                            ExecutablePath = data.ExecutablePath,
                            WindowTitle = data.WindowTitle,
                            IsAvailable = !inspection.Completed,
                            UnavailableReason = inspection.Completed ? "Window is no longer available." : "Window inspection timed out. Click to retry."
                        };

                        // Attempt to extract an icon even though the window is unavailable.
                        var icon = await TryExtractIconWithoutHandleAsync(data.ExecutablePath, data.ProcessName);
                        if (icon != null)
                        {
                            vm.Icon = icon;
                        }
                    }

                    Items.Add(vm);
                }
            }
            finally
            {
                IsRestoringPinnedItems = false;
            }
        }
        /// <summary>
        /// Asynchronously attempts to obtain an icon without a live window handle.
        /// The blocking OS calls (<see cref="Process.GetProcessesByName"/> and
        /// <see cref="System.Diagnostics.ProcessModule.FileName"/> access) run on the
        /// background workers so the UI thread is never blocked.
        /// Tries the saved executable path first; if that is missing or inaccessible,
        /// looks for a running process whose name matches <paramref name="processName"/>
        /// and uses its main module's file path.
        /// Returns <see langword="null"/> when no icon can be found.
        /// </summary>
        private async Task<System.Windows.Media.ImageSource?> TryExtractIconWithoutHandleAsync(
            string? executablePath, string? processName)
        {
            // 1. Try saved executable path directly — icon extraction may read disk, run off-thread.
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                try
                {
                    var icon = await _iconService.ExtractIconAsync(IntPtr.Zero, executablePath);
                    if (icon != null)
                    {
                        return icon;
                    }
                }
                catch { /* fall through */ }
            }

            // 2. Find a running process with the same name and grab its executable.
            // Process.GetProcessesByName and MainModule access are blocking OS calls.
            if (!string.IsNullOrWhiteSpace(processName))
            {
                try
                {
                    var result = await _enumService.Work.RunAsync(() =>
                    {
                        var processes = Process.GetProcessesByName(processName);
                        foreach (var proc in processes)
                        {
                            try
                            {
                                var path = proc.MainModule?.FileName;
                                if (!string.IsNullOrWhiteSpace(path))
                                {
                                    return _iconService.ExtractIcon(IntPtr.Zero, path);
                                }
                            }
                            catch { /* 32/64-bit or permission mismatch — skip */ }
                            finally
                            {
                                proc.Dispose();
                            }
                        }
                        return null;
                    });
                    return result.Value;
                }
                catch { /* fall through */ }
            }

            return null;
        }
    }
}
