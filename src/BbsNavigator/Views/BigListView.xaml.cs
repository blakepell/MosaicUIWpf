/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Resources;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Displays the bundled Big List CSV in a filterable, editable grid that can be saved to disk.
    /// </summary>
    public partial class BigListView : UserControl
    {
        /// <summary>
        /// The rows of the Big List. Every column is a string so the file round trips unchanged.
        /// </summary>
        private DataTable? _table;

        /// <summary>
        /// The file the list was last saved to, or <see langword="null"/> while it is still the
        /// copy that was loaded from the bundled resource. A save with no file behaves as a save as.
        /// </summary>
        private string? _fileName;

        /// <summary>
        /// Initializes the view and loads the bundled Big List resource.
        /// </summary>
        public BigListView()
        {
            InitializeComponent();
            LoadFromResource();
        }

        /// <summary>
        /// Loads the bundled <c>Assets\bbslist.csv</c> resource into the grid.
        /// </summary>
        private void LoadFromResource()
        {
            try
            {
                var resourceUri = new Uri("pack://application:,,,/BbsNavigator;component/Assets/bbslist.csv", UriKind.Absolute);
                StreamResourceInfo? resource = Application.GetResourceStream(resourceUri);

                if (resource == null)
                {
                    throw new FileNotFoundException("The bundled BBS list resource could not be found.");
                }

                using Stream stream = resource.Stream;
                _table = CsvDataTable.Load(stream, "BigList");
                BigListGrid.ItemsSource = _table.DefaultView;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    $"The Big List could not be loaded.\n\n{ex.Message}",
                    "Big List",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            // The list starts out as the bundled resource rather than a file on disk, so the first
            // save has nowhere to write to and prompts the same way Save As does.
            if (string.IsNullOrWhiteSpace(_fileName))
            {
                SaveAs();
                return;
            }

            Save(_fileName);
        }

        private void SaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        /// <summary>
        /// Prompts for a file name and saves the list to it.
        /// </summary>
        private void SaveAs()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Big List As",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = string.IsNullOrWhiteSpace(_fileName) ? "bbslist.csv" : Path.GetFileName(_fileName)
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            {
                return;
            }

            Save(dialog.FileName);
        }

        /// <summary>
        /// Writes the list out to the given file.
        /// </summary>
        /// <param name="fileName">The file to write.</param>
        private void Save(string fileName)
        {
            if (_table == null)
            {
                return;
            }

            try
            {
                // A cell that is still being edited has not been pushed into its row yet.
                BigListGrid.CommitEdit(DataGridEditingUnit.Row, true);

                CsvDataTable.Save(_table, fileName);
                _fileName = fileName;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    $"The Big List could not be saved.\n\n{ex.Message}",
                    "Save Big List",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Refreshes the row count and file name shown beneath the grid.
        /// </summary>
        private void UpdateStatus()
        {
            if (_table == null)
            {
                StatusText.Text = string.Empty;
                return;
            }

            string source = string.IsNullOrWhiteSpace(_fileName) ? "bundled Big List (not saved to disk)" : _fileName;
            StatusText.Text = $"{_table.Rows.Count:N0} entries — {source}";
        }
    }
}
