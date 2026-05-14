/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using WindowCue.Interop;
using WindowCue.ViewModels;
using WindowCue.Views;

namespace WindowCue.Services
{
    /// <summary>
    /// Carries the result of the Select Window/Tab dialog: either a regular desktop
    /// window or a browser tab.
    /// </summary>
    public sealed class AddWindowResult
    {
        /// <summary>
        /// Non-null when the user selected a regular desktop window.
        /// </summary>
        public WindowInfo? Window { get; }

        /// <summary>
        /// Non-null when the user selected a browser tab.
        /// </summary>
        public BrowserTabInfo? BrowserTab { get; }

        private AddWindowResult(WindowInfo? window, BrowserTabInfo? tab)
        {
            Window = window;
            BrowserTab = tab;
        }

        /// <summary>
        /// Creates a result wrapping a desktop window selection.
        /// </summary>
        public static AddWindowResult FromWindow(WindowInfo info) => new(info, null);

        /// <summary>
        /// Creates a result wrapping a browser-tab selection.
        /// </summary>
        public static AddWindowResult FromBrowserTab(BrowserTabInfo tab) => new(null, tab);
    }

    /// <summary>
    /// Opens modal dialogs on behalf of ViewModels, keeping View construction out of VM code.
    /// </summary>
    public class DialogService
    {
        private readonly WindowEnumerationService _enumService;
        private readonly IconExtractionService _iconService;
        private readonly BrowserTabService _tabService;

        public DialogService(WindowEnumerationService enumService, IconExtractionService iconService, BrowserTabService tabService)
        {
            _enumService = enumService;
            _iconService = iconService;
            _tabService = tabService;
        }

        /// <summary>
        /// Shows the Select Window/Tab dialog. Returns a list of <see cref="AddWindowResult"/>
        /// items for every entry the user confirmed (supports Ctrl/Shift multi-selection),
        /// or <see langword="null"/> if the user cancelled or made no selection.
        /// </summary>
        public async Task<List<AddWindowResult>?> ShowSelectWindowDialogAsync(Window owner)
        {
            var vm = new SelectWindowDialogViewModel(_enumService, _iconService, _tabService);
            var dialog = new SelectWindowDialog { DataContext = vm, Owner = owner };

            // Kick off enumeration after the dialog's visual tree is ready.
            // ShowDialog enters a nested WPF dispatcher loop, so this async handler
            // runs inside that loop — the dialog appears immediately with its loading
            // indicator rather than the caller freezing for several seconds.
            dialog.Loaded += async (_, _) => await vm.LoadWindowsAsync();

            if (dialog.ShowDialog() != true || vm.SelectedWindows.Count == 0)
            {
                return null;
            }

            return vm.SelectedWindows
                .Select(selected => selected.IsEdgeTab
                    ? AddWindowResult.FromBrowserTab(selected.BrowserTab!)
                    : AddWindowResult.FromWindow(selected.Source!))
                .ToList();
        }

        /// <summary>
        /// Shows the Rename dialog. Returns the new label or <see langword="null"/> if cancelled.
        /// </summary>
        public string? ShowRenameDialog(string currentLabel, Window owner)
        {
            var dialog = new RenameItemDialog(currentLabel) { Owner = owner };
            return dialog.ShowDialog() == true ? dialog.NewLabel : null;
        }

        /// <summary>
        /// Shows the Object Properties dialog for the given toolbar item.
        /// </summary>
        public void ShowPropertiesDialog(ToolbarItemViewModel item, Window owner)
        {
            var dialog = new ObjectPropertiesDialog(item) { Owner = owner };
            dialog.ShowDialog();
        }
    }
}
