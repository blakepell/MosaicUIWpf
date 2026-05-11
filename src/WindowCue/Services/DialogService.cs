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
    /// Opens modal dialogs on behalf of ViewModels, keeping View construction out of VM code.
    /// </summary>
    public class DialogService
    {
        private readonly WindowEnumerationService _enumService;
        private readonly IconExtractionService    _iconService;

        public DialogService(WindowEnumerationService enumService, IconExtractionService iconService)
        {
            _enumService = enumService;
            _iconService = iconService;
        }

        /// <summary>
        /// Shows the Select Window dialog. Returns the chosen <see cref="WindowInfo"/> or
        /// <see langword="null"/> if the user cancelled.
        /// </summary>
        public async Task<WindowInfo?> ShowSelectWindowDialogAsync(Window owner)
        {
            var vm = new SelectWindowDialogViewModel(_enumService, _iconService);
            var dialog = new SelectWindowDialog { DataContext = vm, Owner = owner };

            // Load windows in the background; icons are extracted lazily on the thread pool.
            await vm.LoadWindowsAsync();

            return dialog.ShowDialog() == true ? vm.SelectedWindow?.Source : null;
        }

        /// <summary>
        /// Shows the Rename dialog. Returns the new label or <see langword="null"/> if cancelled.
        /// </summary>
        public string? ShowRenameDialog(string currentLabel, Window owner)
        {
            var dialog = new RenameItemDialog(currentLabel) { Owner = owner };
            return dialog.ShowDialog() == true ? dialog.NewLabel : null;
        }
    }
}
