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
    /// ViewModel for the "Select a running window" picker dialog.
    /// Loads all visible windows, extracts their icons asynchronously,
    /// and provides live search/filter.
    /// </summary>
    public partial class SelectWindowDialogViewModel : ObservableObject
    {
        private readonly WindowEnumerationService _enumService;
        private readonly IconExtractionService    _iconService;

        private List<RunningWindowViewModel> _allWindows = new();

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
            IconExtractionService iconService)
        {
            _enumService = enumService;
            _iconService = iconService;
        }

        /// <summary>
        /// Enumerates visible windows and back-fills icons on the thread pool.
        /// Call before showing the dialog.
        /// </summary>
        public async Task LoadWindowsAsync()
        {
            var rawWindows = await Task.Run(_enumService.GetVisibleWindows);

            _allWindows = rawWindows
                .Select(w => new RunningWindowViewModel(w))
                .ToList();

            ApplyFilter(SearchText);

            // Extract icons without blocking the UI
            _ = Task.Run(async () =>
            {
                foreach (var vm in _allWindows)
                {
                    var icon = _iconService.ExtractIcon(vm.Source.Handle, vm.Source.ExecutablePath);
                    await Application.Current.Dispatcher.InvokeAsync(() => vm.Icon = icon);
                }
            });
        }

        private void ApplyFilter(string text)
        {
            FilteredWindows.Clear();
            var filtered = string.IsNullOrWhiteSpace(text)
                ? _allWindows
                : _allWindows.Where(w =>
                    w.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                    w.ProcessName.Contains(text, StringComparison.OrdinalIgnoreCase));

            foreach (var vm in filtered)
                FilteredWindows.Add(vm);

            if (SelectedWindow != null && !FilteredWindows.Contains(SelectedWindow))
                SelectedWindow = null;
        }

        /// <summary>Confirms the selection and closes the dialog.</summary>
        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Confirm(Window? dialog)
        {
            if (dialog != null)
                dialog.DialogResult = true;
        }

        private bool CanConfirm() => SelectedWindow != null;
    }
}
