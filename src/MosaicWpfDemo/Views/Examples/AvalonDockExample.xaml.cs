/*
 * AvalonDock for Mosaic UI for WPF
 *
 * @forked from       : https://github.com/Dirkster99/AvalonDock
 * @license           : MS-PL
 */

using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using System.Windows;
using AvalonDockMosaicTheme = Mosaic.UI.Wpf.AvalonDock.Themes.MosaicTheme;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class AvalonDockExample
    {
        public AvalonDockExample()
        {
            InitializeComponent();
            ThemeManager.ThemeChanged += OnMosaicThemeChanged;
            Unloaded += OnUnloaded;

            this.ExtensibleSyntaxDocument.Editor.Language = SyntaxLanguage.Xml;
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

        private void InsertThemeSnippetButton_OnClick(object sender, RoutedEventArgs e)
        {
            const string snippet = "<ad:MosaicTheme />";
            int caretOffset = this.ExtensibleSyntaxDocument.Editor.CaretOffset;

            this.ExtensibleSyntaxDocument.Editor.Document.Insert(caretOffset, snippet);
            this.ExtensibleSyntaxDocument.Editor.CaretOffset = caretOffset + snippet.Length;
            this.ExtensibleSyntaxDocument.Editor.Focus();
        }

        private void WordWrapToggle_OnChecked(object sender, RoutedEventArgs e) =>
            this.ExtensibleSyntaxDocument.Editor.WordWrap = true;

        private void WordWrapToggle_OnUnchecked(object sender, RoutedEventArgs e) =>
            this.ExtensibleSyntaxDocument.Editor.WordWrap = false;
    }
}
