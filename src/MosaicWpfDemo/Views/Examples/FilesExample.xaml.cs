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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            // Opens the right-clicked folder in Windows Explorer. The parameter is the FileItem the folder
            // context menu was invoked on (supplied via CommandParameter="{Binding}").
            this.OpenFolderCommand = new DelegateCommand(
                parameter => OpenInExplorer(parameter as FileItem),
                parameter => parameter is FileItem { IsDirectory: true });

            InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        /// Opens the folder represented by the right-clicked <see cref="FileItem"/> in Windows Explorer.
        /// </summary>
        public ICommand OpenFolderCommand { get; }

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

        private void OpenInExplorer(FileItem? item)
        {
            if (item is not { IsDirectory: true } || !Directory.Exists(item.FullPath))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(item.FullPath) { UseShellExecute = true });
                StatusTextBlock.Text = $"Opened in Explorer: {item.FullPath}";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Could not open '{item.FullPath}': {ex.Message}";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}
