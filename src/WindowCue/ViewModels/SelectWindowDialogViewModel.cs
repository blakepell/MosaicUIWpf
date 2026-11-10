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
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using WindowCue.Interop;
using WindowCue.Services;

namespace WindowCue.ViewModels
{
    /// <summary>
    /// ViewModel for the "Select a running window or browser tab" picker dialog.
    /// Loads all visible windows and open Edge tabs, extracts their icons asynchronously,
    /// and provides live search/filter across both categories.
    /// </summary>
    public partial class SelectWindowDialogViewModel : ObservableObject, IDisposable
    {
        private readonly WindowEnumerationService _enumService;
        private readonly IconExtractionService _iconService;
        private readonly BrowserTabService _tabService;
        private readonly Func<List<WindowInfo>> _readWindows;
        private readonly Func<List<BrowserTabInfo>> _readTabs;
        private readonly CancellationTokenSource _lifetime = new();
        private bool _disposed;

        private readonly List<RunningWindowViewModel> _allItems = new();
        private bool _loadStarted;

        /// <summary>
        /// Filtered list bound to the dialog's ListBox.
        /// </summary>
        public ObservableCollection<RunningWindowViewModel> FilteredWindows { get; } = new();

        /// <summary>
        /// All items the user confirmed. Populated by the dialog's code-behind just
        /// before <c>DialogResult = true</c> so callers get the full multi-selection.
        /// </summary>
        public List<RunningWindowViewModel> SelectedWindows { get; set; } = new();

        /// <summary>
        /// Live search text; filtering updates on every keystroke.
        /// </summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// True while windows or browser tabs are being enumerated on background workers.
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>Explains an incomplete scan instead of silently displaying an empty list.</summary>
        [ObservableProperty]
        private string _loadStatus = string.Empty;

        partial void OnSearchTextChanged(string value) => ApplyFilter(value);

        public SelectWindowDialogViewModel(WindowEnumerationService enumService, IconExtractionService iconService, BrowserTabService tabService)
            : this(enumService, iconService, tabService,
                () => enumService.GetVisibleWindows(includeCommandLine: false),
                () => tabService.GetOpenTabs("msedge"))
        {
        }

        internal SelectWindowDialogViewModel(WindowEnumerationService enumService, IconExtractionService iconService,
            BrowserTabService tabService, Func<List<WindowInfo>> readWindows, Func<List<BrowserTabInfo>> readTabs)
        {
            _enumService = enumService;
            _iconService = iconService;
            _tabService = tabService;
            _readWindows = readWindows;
            _readTabs = readTabs;
        }

        /// <summary>
        /// Enumerates visible windows and open Edge tabs independently, displaying each
        /// category as it arrives, then back-fills icons on background workers.
        /// Intended to be called after the dialog is shown so the window appears immediately
        /// with a loading indicator rather than blocking the caller for several seconds.
        /// </summary>
        public async Task LoadWindowsAsync()
        {
            if (_loadStarted || _disposed) return;
            _loadStarted = true;
            IsLoading = true;
            LoadStatus = "Loading windows and tabs…";
            try
            {
                // Read-only discovery must not lose its result when a debugger pause or
                // slow provider exceeds the presentation-click timeout. Closing the dialog
                // cancels the wait; the service lane still bounds any stuck native worker.
                var windowsTask = LoadCategoryAsync(_enumService.PickerWork, _readWindows,
                    w => new RunningWindowViewModel(w));
                var tabsTask = LoadCategoryAsync(_tabService.PickerWork, _readTabs,
                    t => new RunningWindowViewModel(t));
                var completed = await Task.WhenAll(windowsTask, tabsTask);
                LoadStatus = completed.All(success => success) ? string.Empty
                    : "Some windows or tabs could not be loaded. Close and reopen to try again.";
            }
            catch (OperationCanceledException) when (_disposed)
            {
                // The dialog closed while a provider was still scanning.
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<bool> LoadCategoryAsync<T>(BackgroundOperationRunner runner,
            Func<List<T>> read, Func<T, RunningWindowViewModel> create)
        {
            var result = await runner.RunAsync(read, Timeout.InfiniteTimeSpan).WaitAsync(_lifetime.Token);
            if (_disposed || !result.Completed) return false;
            var items = (result.Value ?? []).Select(create).ToList();
            foreach (var item in items)
            {
                _allItems.Add(item);
                // Append without rebuilding the list, preserving selections made while
                // the other category is still loading. Respect the current search text.
                if (MatchesFilter(item, SearchText)) FilteredWindows.Add(item);
            }
            _ = LoadIconsAsync(items);
            return true;
        }

        /// <summary>Stops waiting for discovery when the picker closes.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private async Task LoadIconsAsync(List<RunningWindowViewModel> items)
        {
            try
            {
                foreach (var vm in items)
                {
                    if (_disposed) return;
                    IntPtr handle = vm.IsEdgeTab ? vm.BrowserTab!.WindowHandle : vm.Source!.Handle;
                    var icon = await _iconService.ExtractIconAsync(handle, vm.ExecutablePath);
                    if (_disposed || Application.Current?.Dispatcher.HasShutdownStarted == true)
                        return;
                    vm.Icon = icon;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WindowCue picker icon loading failed: {ex}");
            }
        }

        private void ApplyFilter(string text)
        {
            FilteredWindows.Clear();
            var filtered = _allItems.Where(w => MatchesFilter(w, text));

            foreach (var vm in filtered)
            {
                FilteredWindows.Add(vm);
            }

            if (!FilteredWindows.Any())
            {
                SelectedWindows.Clear();
            }
        }

        private static bool MatchesFilter(RunningWindowViewModel item, string text) =>
            string.IsNullOrWhiteSpace(text) ||
            item.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            item.ProcessName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            item.DisplaySubtitle.Contains(text, StringComparison.OrdinalIgnoreCase);

    }
}
