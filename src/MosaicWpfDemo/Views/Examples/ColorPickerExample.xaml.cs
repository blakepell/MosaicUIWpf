/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Cache;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    [ObservableObject]
    public partial class ColorPickerExample
    {
        [ObservableProperty]
        private Brush _demoBackgroundBrush = ColorPaletteCache.GetBrush("#185ABD");

        [ObservableProperty]
        private string _selectedHexColor = "#FF185ABD";

        public ColorPickerExample()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ColorPicker_ColorChanged(object sender, ColorChangedEventArgs e)
        {
            if (BasicColorText != null)
            {
                BasicColorText.Text = $"Selected: {e.HexValue} ({e.Color})";
            }
        }
    }
}