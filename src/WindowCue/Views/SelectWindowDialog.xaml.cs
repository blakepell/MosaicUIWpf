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
using System.Windows.Input;

namespace WindowCue.Views
{
    public partial class SelectWindowDialog
    {
        public SelectWindowDialog()
        {
            InitializeComponent();
        }

        private void WindowList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click confirms the selection if at least one item is selected.
            if (DataContext is ViewModels.SelectWindowDialogViewModel vm && WindowList.SelectedItems.Count > 0)
            {
                vm.SelectedWindows = WindowList.SelectedItems
                    .Cast<ViewModels.RunningWindowViewModel>()
                    .ToList();
                DialogResult = true;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.SelectWindowDialogViewModel vm && WindowList.SelectedItems.Count > 0)
            {
                vm.SelectedWindows = WindowList.SelectedItems
                    .Cast<ViewModels.RunningWindowViewModel>()
                    .ToList();
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
