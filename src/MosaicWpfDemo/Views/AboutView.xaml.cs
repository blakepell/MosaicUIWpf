/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Controls;
using Argus.Memory;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;
using MosaicWpfDemo.Common;
using MosaicThemeMode = Mosaic.UI.Wpf.ThemeMode;

namespace MosaicWpfDemo.Views
{
    public partial class AboutView
    {
        private bool _syncingThemeControls;

        public AboutView()
        {
            InitializeComponent();
        }

        private void AboutView_OnLoaded(object sender, RoutedEventArgs e)
        {
            SyncThemeEditorFromManager();
        }

        private void ThemeModeSelector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncingThemeControls)
            {
                return;
            }

            if (ThemeModeSelector.SelectedItem is not ComboBoxItem selected || selected.Tag is not string selectedTheme)
            {
                return;
            }

            if (!Enum.TryParse<MosaicThemeMode>(selectedTheme, ignoreCase: true, out var mode))
            {
                return;
            }

            var manager = AppServices.GetService<ThemeManager>();
            if (manager == null)
            {
                return;
            }

            manager.Theme = mode;
            PersistTheme(mode);
        }

        private void NativeStylesCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            if (_syncingThemeControls)
            {
                return;
            }

            var manager = AppServices.GetService<ThemeManager>();
            if (manager == null)
            {
                return;
            }

            manager.Native = NativeStylesCheckBox.IsChecked == true;
        }

        private void SystemColorsCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            if (_syncingThemeControls)
            {
                return;
            }

            var manager = AppServices.GetService<ThemeManager>();
            if (manager == null)
            {
                return;
            }

            manager.SystemColors = SystemColorsCheckBox.IsChecked == true;
        }

        private void SyncThemeEditorFromManager()
        {
            var manager = AppServices.GetService<ThemeManager>();
            if (manager == null)
            {
                return;
            }

            _syncingThemeControls = true;

            NativeStylesCheckBox.IsChecked = manager.Native;
            SystemColorsCheckBox.IsChecked = manager.SystemColors;

            ComboBoxItem? selectedItem = null;

            foreach (var item in ThemeModeSelector.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    string.Equals(comboBoxItem.Tag as string, manager.Theme.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    selectedItem = comboBoxItem;
                    break;
                }
            }

            ThemeModeSelector.SelectedItem = selectedItem;

            _syncingThemeControls = false;
        }

        private static void PersistTheme(MosaicThemeMode theme)
        {
            if (!AppServices.IsRegistered<AppSettings>())
            {
                return;
            }

            var appSettings = AppServices.GetRequiredService<AppSettings>();
            appSettings.Theme = theme;
        }
    }
}
