/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class FilesExample : INotifyPropertyChanged
    {
        private string _folderPath;
        private bool _multiSelect;

        public FilesExample()
        {
            // Start in a folder that reliably has files to show.
            _folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        /// The folder whose files are displayed by the <see cref="Files"/> control.
        /// </summary>
        public string FolderPath
        {
            get => _folderPath;
            set
            {
                if (_folderPath != value)
                {
                    _folderPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Toggles the <see cref="Files"/> control between single and extended (multi) selection.
        /// </summary>
        public bool MultiSelect
        {
            get => _multiSelect;
            set
            {
                if (_multiSelect != value)
                {
                    _multiSelect = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectionMode));
                }
            }
        }

        public SelectionMode SelectionMode => _multiSelect ? SelectionMode.Extended : SelectionMode.Single;

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select a folder",
                InitialDirectory = Directory.Exists(this.FolderPath) ? this.FolderPath : string.Empty
            };

            if (dialog.ShowDialog() == true)
            {
                this.FolderPath = dialog.FolderName;
            }
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            FilesControl.Refresh();
        }

        private void FilesControl_OnFileDoubleClick(object sender, FileActivatedEventArgs e)
        {
            StatusTextBlock.Text = $"Double-clicked: {e.File.FullName}  ({FileItem.FormatSize(e.File.Length)}, modified {e.File.LastWriteTime:g})";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
