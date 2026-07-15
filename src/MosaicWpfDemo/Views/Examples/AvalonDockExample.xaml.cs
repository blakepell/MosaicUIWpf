/*
 * AvalonDock for Mosaic UI for WPF
 *
 * @forked from       : https://github.com/Dirkster99/AvalonDock
 * @license           : MS-PL
 */

using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.AvalonDock;
using Mosaic.UI.Wpf.Themes;
using System.Windows;
using Mosaic.UI.Wpf.Controls;
using AvalonDockMosaicTheme = Mosaic.UI.Wpf.AvalonDock.Themes.MosaicTheme;
using System.Drawing;
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class AvalonDockExample
    {
        public AvalonDockExample()
        {
            InitializeComponent();
            ThemeManager.ThemeChanged += OnMosaicThemeChanged;
            Unloaded += OnUnloaded;

            this.DockingManager.Add(new SyntaxEditor()
            {
                Language = SyntaxLanguage.Xml,
                Text = "<html></html>"
            }, "Sample HTML", Colors.Green, false, true, true);
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
