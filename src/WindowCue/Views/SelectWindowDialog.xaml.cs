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
    public partial class SelectWindowDialog : Window
    {
        public SelectWindowDialog()
        {
            InitializeComponent();
        }

        private void WindowList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click confirms the selection if one is active
            if (DataContext is ViewModels.SelectWindowDialogViewModel vm && vm.SelectedWindow != null)
            {
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
