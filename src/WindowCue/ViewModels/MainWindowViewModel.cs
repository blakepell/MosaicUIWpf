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
using System.Windows;
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

        /// <summary>The ordered collection of pinned toolbar items.</summary>
        public ObservableCollection<ToolbarItemViewModel> Items { get; } = new();

        /// <summary>Current dock edge used to re-apply docking when the edge changes.</summary>
        [ObservableProperty]
        private DockEdge _dockEdge = DockEdge.Left;

        /// <summary>Device name of the monitor the toolbar is docked to.</summary>
        [ObservableProperty]
        private string? _monitorDeviceName;

        public MainWindowViewModel(
            WindowEnumerationService enumService,
            WindowFocusService focusService,
            IconExtractionService iconService,
            ScreenDockingService dockingService,
            DialogService dialogService)
        {
            _enumService = enumService;
            _focusService = focusService;
            _iconService = iconService;
            _dockingService = dockingService;
            _dialogService = dialogService;
        }

        // ── Add ──────────────────────────────────────────────────────────────

        /// <summary>Opens the Select Window dialog and adds the chosen window to the toolbar.</summary>
        [RelayCommand]
        private async Task AddWindowAsync()
        {
            var window = Application.Current.MainWindow;
            if (window == null)
            {
                return;
            }

            var info = await _dialogService.ShowSelectWindowDialogAsync(window);
            if (info == null)
            {
                return;
            }

            // Extract icon (may already be cached in the dialog, but re-extract for safety)
            var icon = _iconService.ExtractIcon(info.Handle, info.ExecutablePath);
            info.Icon = icon;

            var vm = ToolbarItemViewModel.FromWindowInfo(info);
            Items.Add(vm);
        }

        // ── Focus ─────────────────────────────────────────────────────────────

        /// <summary>Focuses (and restores if minimized) the target window.</summary>
        [RelayCommand]
        private void FocusItem(ToolbarItemViewModel? item)
        {
            if (item == null)
            {
                return;
            }

            // Try the stored handle first
            if (_focusService.FocusWindow(item.WindowHandle))
            {
                item.IsAvailable = true;
                item.UnavailableReason = null;
                return;
            }

            // Handle is stale — try to find a fresh handle by PID
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

            item.IsAvailable = false;
            item.UnavailableReason = "Window is no longer available.";
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
                ProcessId = i.ProcessId,
                Label = i.Label,
                ProcessName = i.ProcessName,
                ExecutablePath = i.ExecutablePath,
                WindowTitle = i.WindowTitle
            }).ToList();

        /// <summary>
        /// Populates the toolbar from persisted data, attempting to rebind each item to a
        /// currently-running window. Items that cannot be rebound are shown as unavailable.
        /// </summary>
        public async Task RestoreFromPersistedItemsAsync(IEnumerable<PinnedItemData> saved)
        {
            foreach (var data in saved)
            {
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
                    // Try the saved executable path first, then look for a running process
                    // with the same name (another instance may be open, giving us the icon).
                    var icon = TryExtractIconWithoutHandle(data.ExecutablePath, data.ProcessName);
                    if (icon != null)
                    {
                        vm.Icon = icon;
                    }
                }

                Items.Add(vm);
            }
        }
        /// <summary>
        /// Attempts to obtain an icon without a live window handle. Tries the saved executable
        /// path first; if that is missing or inaccessible, looks for a running process whose name
        /// matches <paramref name="processName"/> and uses its main module's file path.
        /// Returns <see langword="null"/> when no icon can be found.
        /// </summary>
        private System.Windows.Media.ImageSource? TryExtractIconWithoutHandle(
            string? executablePath, string? processName)
        {
            // 1. Try saved executable path directly.
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                try
                {
                    return _iconService.ExtractIcon(IntPtr.Zero, executablePath);
                }
                catch { /* fall through */ }
            }

            // 2. Find a running process with the same name and grab its executable.
            if (!string.IsNullOrWhiteSpace(processName))
            {
                try
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
                }
                catch { /* fall through */ }
            }

            return null;
        }
    }
}
