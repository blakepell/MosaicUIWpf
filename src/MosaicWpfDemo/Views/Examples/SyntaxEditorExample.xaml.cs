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
using Argus.Memory;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Themes;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class SyntaxEditorExample
    {
        private const string SampleJson = """
        {
          "name": "Mosaic UI for WPF",
          "version": "2026.6.14",
          "enabled": true,
          "tags": [ "wpf", "controls", "mosaic" ],
          "settings": {
            "theme": "Dark",
            "fontSize": 12,
            "showLineNumbers": true
          },
          "dependencies": null
        }
        """;

        public SyntaxEditorExample()
        {
            InitializeComponent();

            this.Editor.Text = SampleJson;

            // Adopt the current application theme, then keep the editor in sync as the user toggles it.
            this.Editor.Theme = AppServices.GetRequiredService<ThemeManager>().Theme;

            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ThemeManager.ThemeChanged += this.OnThemeChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ThemeManager.ThemeChanged -= this.OnThemeChanged;
        }

        private void OnThemeChanged(object? sender, MosaicThemeMode theme)
        {
            // Setting Theme recolors the surface and reloads the matching (Light/Dark) JSON highlighting.
            this.Editor.Theme = theme;
        }
    }
}
