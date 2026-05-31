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
                    var icon = _iconService.ExtractIcon(result.BrowserTab.WindowHandle, result.BrowserTab.ExecutablePath);
                    result.BrowserTab.Icon = icon;
                    vm = ToolbarItemViewModel.FromBrowserTab(result.BrowserTab);
                }
                else if (result.Window != null)
                {
                    // Regular desktop window path
                    var icon = _iconService.ExtractIcon(result.Window.Handle, result.Window.ExecutablePath);
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
        private void FocusItem(ToolbarItemViewModel? item)
        {
            if (item == null)
            {
                return;
            }

            // ── Browser-tab path ─────────────────────────────────────────────
            if (item.TargetType == PinnedTargetType.BrowserTab)
            {
                bool ok = _browserTabService.FocusTab(
                    item.BrowserProcessName, item.TabTitle, item.TabUrl);

                item.IsAvailable = ok;
                item.UnavailableReason = ok ? null : "Browser tab is no longer open.";
                return;
            }

            // ── Regular window path ──────────────────────────────────────────
            // Try the stored handle first
            if (_focusService.FocusWindow(item.WindowHandle))
            {
                item.IsAvailable = true;
                item.UnavailableReason = null;
                return;
            }

            // Handle is stale — try to find a fresh handle by the original PID first.
            var freshHandle = _focusService.FindWindowForProcess(item.ProcessId);
            if (freshHandle != IntPtr.Zero)
            {
                item.WindowHandle = freshHandle;
                if (_focusService.FocusWindow(freshHandle))
                {
                    item.IsAvailable = true;
                    item.UnavailableReason = null;
                    return;
                }
            }

            // The original process is gone or its window is unreachable. The application may
            // have been re-launched under a new PID (e.g. Visual Studio re-opened on the same
            // solution), so try to re-bind to the best-matching live window by executable /
            // process identity and fuzzy window-title similarity.
            if (TryRebindItem(item))
            {
                return;
            }

            // The original process is gone or its window is unreachable, and no live window
            // could be matched even by partial title similarity. Leave the item in place but
            // mark it inactive (grayed out) rather than removing it — the user can re-bind by
            // clicking again later, or prune it with "Remove Inactive".
            bool processAlive;
            try
            {
                using var proc = Process.GetProcessById(item.ProcessId);
                processAlive = !proc.HasExited;
            }
            catch
            {
                processAlive = false;
            }

            item.IsAvailable = false;
            item.UnavailableReason = processAlive
                ? "Window is no longer available."
                : "Application is not currently running.";
        }

        /// <summary>
        /// Attempts to re-bind a pinned window item to the best-matching live window using
        /// executable / process identity and fuzzy window-title similarity. On success the
        /// item's handle, PID, title and identity fields are refreshed and it is marked
        /// available. When <paramref name="focus"/> is <see langword="true"/> the matched
        /// window is also brought to the foreground.
        /// </summary>
        /// <returns><see langword="true"/> if a matching window was found and the item re-bound.</returns>
        private bool TryRebindItem(ToolbarItemViewModel item, bool focus = true)
        {
            if (item.TargetType != PinnedTargetType.Window)
            {
                return false;
            }

            var match = _enumService.TryRebind(item.ExecutablePath, item.ProcessName, item.WindowTitle);
            if (match == null)
            {
                return false;
            }

            if (focus)
            {
                _focusService.FocusWindow(match.Handle);
            }

            item.WindowHandle = match.Handle;
            item.ProcessId = match.ProcessId;
            item.WindowTitle = match.Title;
            item.ProcessName = match.ProcessName;
            item.ExecutablePath = match.ExecutablePath;

            if (item.Icon == null)
            {
                item.Icon = _iconService.ExtractIcon(match.Handle, match.ExecutablePath);
            }

            item.IsAvailable = true;
            item.UnavailableReason = null;
            return true;
        }

        // ── Process monitor ───────────────────────────────────────────────────

        /// <summary>
        /// Fires every 5 seconds to reconcile pinned window items whose process has exited.
        /// Each dead item is first re-bound to a matching live window (handling apps relaunched
        /// under a new PID, including partial title matches). Items with no matching window are
        /// marked inactive (grayed out) rather than removed, so they auto-reactivate if the app
        /// returns and can be pruned via "Remove Inactive". The liveness checks run on the
        /// thread pool to avoid blocking the UI thread. Browser-tab items are skipped — their
        /// lifecycle is managed by the browser.
        /// </summary>
        private async void OnProcessMonitorTick(object? sender, EventArgs e)
        {
            var windowItems = Items
                .Where(i => i.TargetType == PinnedTargetType.Window)
                .ToList();

            if (windowItems.Count == 0)
            {
                return;
            }

            var deadItems = await Task.Run(() =>
            {
                var dead = new List<ToolbarItemViewModel>();
                foreach (var item in windowItems)
                {
                    bool alive;
                    try
                    {
                        using var proc = Process.GetProcessById(item.ProcessId);
                        alive = !proc.HasExited;
                    }
                    catch
                    {
                        alive = false;
                    }

                    if (!alive)
                    {
                        dead.Add(item);
                    }
                }
                return dead;
            });

            foreach (var dead in deadItems)
            {
                // The original process exited, but the application may have been relaunched
                // under a new PID. Try to re-bind to a matching live window (including partial
                // title matches) before giving up.
                if (TryRebindItem(dead, focus: false))
                {
                    continue;
                }

                // No live window matched — mark the item inactive (grayed out) instead of
                // removing it. The user can prune these via "Remove Inactive".
                dead.IsAvailable = false;
                dead.UnavailableReason = "Application is not currently running.";
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
            var hWnd = NativeMethods.GetForegroundWindow();

            if (hWnd == IntPtr.Zero || !NativeMethods.IsWindow(hWnd) || !NativeMethods.IsWindowVisible(hWnd))
            {
                return;
            }

            // Skip WindowCue's own windows.
            NativeMethods.GetWindowThreadProcessId(hWnd, out int pid);
            if (pid == 0 || pid == Environment.ProcessId)
            {
                return;
            }

            // Skip if already pinned (match by ProcessId).
            if (Items.Any(i => i.ProcessId == pid))
            {
                return;
            }

            // Build WindowInfo from the live handle.
            int titleLen = NativeMethods.GetWindowTextLength(hWnd);
            var sb = new StringBuilder(titleLen + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();

            if (string.IsNullOrWhiteSpace(title))
            {
                return;
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
                return;
            }

            if (WindowEnumerationService.IgnoreList.Contains(processName))
            {
                return;
            }

            var info = new WindowInfo
            {
                Handle = hWnd,
                Title = title,
                ProcessId = pid,
                ProcessName = processName,
                ExecutablePath = executablePath
            };

            var icon = await Task.Run(() => _iconService.ExtractIcon(hWnd, executablePath));
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
                        var tab = await Task.Run(() =>
                            _browserTabService.TryRebind(
                                data.BrowserProcessName, data.TabTitle, data.TabUrl));

                        ToolbarItemViewModel tabVm;
                        if (tab != null)
                        {
                            tab.Icon = _iconService.ExtractIcon(tab.WindowHandle, tab.ExecutablePath);
                            tabVm = ToolbarItemViewModel.FromBrowserTab(tab, data.Label);
                        }
                        else
                        {
                            tabVm = new ToolbarItemViewModel
                            {
                                Label = data.Label,
                                ProcessName = data.BrowserProcessName,
                                TargetType = PinnedTargetType.BrowserTab,
                                TabTitle = data.TabTitle,
                                TabUrl = data.TabUrl,
                                BrowserProcessName = data.BrowserProcessName,
                                IsAvailable = false,
                                UnavailableReason = "Browser tab is not currently open."
                            };
                            var tabIcon = await TryExtractIconWithoutHandleAsync(data.ExecutablePath, data.BrowserProcessName);
                            if (tabIcon != null)
                            {
                                tabVm.Icon = tabIcon;
                            }
                        }

                        Items.Add(tabVm);
                        continue;
                    }

                    // ── Regular window restore path ───────────────────────────────
                    var match = await Task.Run(() =>
                        _enumService.TryRebind(data.ExecutablePath, data.ProcessName, data.WindowTitle));

                    ToolbarItemViewModel vm;
                    if (match != null)
                    {
                        match.Icon = _iconService.ExtractIcon(match.Handle, match.ExecutablePath);
                        vm = ToolbarItemViewModel.FromWindowInfo(match, data.Label);
                    }
                    else
                    {
                        // If the process is completely gone, skip the placeholder.
                        // Only add an unavailable placeholder when a matching process is
                        // actually running (window may be hidden / not yet shown).
                        bool processRunning = await Task.Run(() =>
                            Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(data.ProcessName ?? ""))
                                   .Any(p => { try { return !p.HasExited; } finally { p.Dispose(); } }));

                        if (!processRunning)
                        {
                            continue;
                        }

                        // Show as unavailable placeholder so the user can remove or wait
                        vm = new ToolbarItemViewModel
                        {
                            Label = data.Label,
                            ProcessName = data.ProcessName,
                            ExecutablePath = data.ExecutablePath,
                            WindowTitle = data.WindowTitle,
                            IsAvailable = false,
                            UnavailableReason = "Application is not currently running."
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
        /// thread pool so the UI thread is never blocked.
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
                    var icon = await Task.Run(() => _iconService.ExtractIcon(IntPtr.Zero, executablePath));
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
                    return await Task.Run(() =>
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
                }
                catch { /* fall through */ }
            }

            return null;
        }
    }
}
