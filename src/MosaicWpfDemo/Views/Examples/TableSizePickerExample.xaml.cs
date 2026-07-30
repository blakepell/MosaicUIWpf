/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demonstrates the <see cref="TableSizePicker"/> standalone, in a popup, in a context menu and with
    /// customized cells.
    /// </summary>
    public partial class TableSizePickerExample
    {
        public TableSizePickerExample()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The standalone picker commits a size, build a matching Grid so the result is visible.
        /// </summary>
        private void StandalonePicker_OnTableSizeSelected(object sender, TableSizeSelectedEventArgs e)
        {
            StandaloneResultText.Text = $"Selected {e.RowCount} x {e.ColumnCount} ({e.RowCount * e.ColumnCount} cells).";
            this.BuildTable(e.RowCount, e.ColumnCount);
        }

        /// <summary>
        /// Prepare the picker each time the dropdown opens.
        /// </summary>
        private void InsertTablePopup_OnOpened(object? sender, EventArgs e)
        {
            PopupPicker.OnShow();
        }

        private void PopupPicker_OnTableSizeSelected(object sender, TableSizeSelectedEventArgs e)
        {
            PopupResultText.Text = $"Inserted a {e.RowCount} x {e.ColumnCount} table.";
        }

        /// <summary>
        /// The picker cannot know how its host closes, the host handles RequestClose. Unchecking the button
        /// closes the popup because IsOpen is bound to it.
        /// </summary>
        private void PopupPicker_OnRequestClose(object? sender, EventArgs e)
        {
            InsertTableButton.IsChecked = false;
        }

        private void ContextMenuPicker_OnTableSizeSelected(object sender, TableSizeSelectedEventArgs e)
        {
            ContextMenuResultText.Text = $"Right click picked {e.RowCount} x {e.ColumnCount}.";
        }

        private void ContextMenuPicker_OnRequestClose(object? sender, EventArgs e)
        {
            TableContextMenu.IsOpen = false;
        }

        /// <summary>
        /// Generates a simple grid of cells matching the committed table size.
        /// </summary>
        private void BuildTable(int rows, int columns)
        {
            GeneratedTable.Children.Clear();
            GeneratedTable.RowDefinitions.Clear();
            GeneratedTable.ColumnDefinitions.Clear();

            for (int row = 0; row < rows; row++)
            {
                GeneratedTable.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) });
            }

            for (int column = 0; column < columns; column++)
            {
                GeneratedTable.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            }

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var cell = new Border
                    {
                        BorderThickness = new Thickness(0.5)
                    };

                    cell.SetResourceReference(Border.BorderBrushProperty, MosaicTheme.ControlBorderBrush);
                    cell.SetResourceReference(Border.BackgroundProperty, MosaicTheme.ControlBackgroundLightBrush);

                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, column);
                    GeneratedTable.Children.Add(cell);
                }
            }

            PreviewBorder.Visibility = Visibility.Visible;
        }
    }
}
