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
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    [ObservableObject]
    public partial class FontControlsExample
    {
        /// <summary>
        /// The font name shared by the first three examples, showing that the string and the
        /// FontFamily binding surfaces are interchangeable.
        /// </summary>
        [ObservableProperty]
        private string? _selectedFontName = "Segoe UI";

        /// <summary>
        /// A separate selection bound as an actual <see cref="FontFamily"/>.
        /// </summary>
        [ObservableProperty]
        private FontFamily? _sampleFontFamily = FontFamilyCatalog.Find("Times New Roman");

        [ObservableProperty]
        private string _sampleText = "The quick brown fox jumps over the lazy dog. 0123456789";

        /// <summary>
        /// The weight applied to the live sample, bound as an actual <see cref="FontWeight"/>.
        /// </summary>
        [ObservableProperty]
        private FontWeight _selectedFontWeight = FontWeights.Normal;

        public FontControlsExample()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Assigns a font family by name so the controls bound to it update their selection.
        /// </summary>
        /// <param name="fontName">The name of the font family to select.</param>
        [RelayCommand]
        private void SetSampleFontFamily(string fontName)
        {
            SampleFontFamily = FontFamilyCatalog.Find(fontName);
        }
    }
}
