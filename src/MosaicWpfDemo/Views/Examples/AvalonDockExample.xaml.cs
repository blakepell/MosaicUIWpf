/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;
using System.Windows;
using AvalonDockMosaicTheme = AvalonDock.Themes.MosaicTheme;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class AvalonDockExample
    {
        public AvalonDockExample()
        {
            InitializeComponent();
            ThemeManager.ThemeChanged += OnMosaicThemeChanged;
            Unloaded += OnUnloaded;
        }

        private void OnMosaicThemeChanged(object? sender, MosaicThemeMode e)
        {
            Dispatcher.Invoke(() => DockingManager.Theme = new AvalonDockMosaicTheme());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ThemeManager.ThemeChanged -= OnMosaicThemeChanged;
            Unloaded -= OnUnloaded;
        }
    }
}
