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
using CommunityToolkit.Mvvm.Input;
using WindowCue.Interop;
using WindowCue.Services;

namespace WindowCue.ViewModels
{
    /// <summary>
    /// ViewModel for the "Select a running window or browser tab" picker dialog.
    /// Loads all visible windows and open Edge tabs, extracts their icons asynchronously,
    /// and provides live search/filter across both categories.
    /// </summary>
    public partial class SelectWindowDialogViewModel : ObservableObject
    {
        private readonly WindowEnumerationService _enumService;
        private readonly IconExtractionService    _iconService;
        private readonly BrowserTabService        _tabService;

        private List<RunningWindowViewModel> _allItems = new();

        /// <summary>Filtered list bound to the dialog's ListBox.</summary>
        public ObservableCollection<RunningWindowViewModel> FilteredWindows { get; } = new();

        /// <summary>The item the user has selected.</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
        private RunningWindowViewModel? _selectedWindow;

        /// <summary>Live search text; filtering updates on every keystroke.</summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        partial void OnSearchTextChanged(string value) => ApplyFilter(value);

        public SelectWindowDialogViewModel(
            WindowEnumerationService enumService,
            IconExtractionService iconService,
            BrowserTabService tabService)
        {
            _enumService = enumService;
            _iconService = iconService;
            _tabService  = tabService;
        }

        /// <summary>
        /// Enumerates visible windows and open Edge tabs, then back-fills icons on the
        /// thread pool. Desktop windows are listed first, followed by browser tabs.
        /// Call before showing the dialog.
        /// </summary>
        public async Task LoadWindowsAsync()
        {
            // Run both enumerations in parallel on the thread pool.
            var rawWindowsTask = Task.Run(_enumService.GetVisibleWindows);
            var rawTabsTask    = Task.Run(() => _tabService.GetOpenTabs("msedge"));

            await Task.WhenAll(rawWindowsTask, rawTabsTask);

            var windowVms = rawWindowsTask.Result
                .Select(w => new RunningWindowViewModel(w))
                .ToList();

            var tabVms = rawTabsTask.Result
                .Select(t => new RunningWindowViewModel(t))
                .ToList();

            // Desktop windows first, then Edge tabs.
            _allItems = [.. windowVms, .. tabVms];

            ApplyFilter(SearchText);

            // Extract icons without blocking the UI.
            _ = Task.Run(async () =>
            {
                foreach (var vm in _allItems)
                {
                    IntPtr handle  = vm.IsEdgeTab ? vm.BrowserTab!.WindowHandle : vm.Source!.Handle;
                    string? exePath = vm.ExecutablePath;
                    var icon = _iconService.ExtractIcon(handle, exePath);
                    await Application.Current.Dispatcher.InvokeAsync(() => vm.Icon = icon);
                }
            });
        }

        private void ApplyFilter(string text)
        {
            FilteredWindows.Clear();
            var filtered = string.IsNullOrWhiteSpace(text)
                ? _allItems
                : _allItems.Where(w =>
                    w.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    w.ProcessName.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    w.DisplaySubtitle.Contains(text, StringComparison.OrdinalIgnoreCase));

            foreach (var vm in filtered)
            {
                FilteredWindows.Add(vm);
            }

            if (SelectedWindow != null && !FilteredWindows.Contains(SelectedWindow))
            {
                SelectedWindow = null;
            }
        }

        /// <summary>Confirms the selection and closes the dialog.</summary>
        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Confirm(Window? dialog)
        {
            if (dialog != null)
            {
                dialog.DialogResult = true;
            }
        }

        private bool CanConfirm() => SelectedWindow != null;
    }
}
