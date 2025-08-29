/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a control that displays a list of available directories and files in a path.
    /// </summary>
    public class DirectoryListBox : ListBox
    {
        static DirectoryListBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DirectoryListBox),
                new FrameworkPropertyMetadata(typeof(DirectoryListBox)));
        }

        public static readonly DependencyProperty CurrentPathProperty = DependencyProperty.Register(
            nameof(CurrentPath), typeof(string), typeof(DirectoryListBox),
            new PropertyMetadata(null, OnCurrentPathChanged));

        public string? CurrentPath
        {
            get => (string?)GetValue(CurrentPathProperty);
            set => SetValue(CurrentPathProperty, value);
        }

        public static readonly DependencyProperty ShowHiddenProperty = DependencyProperty.Register(
            nameof(ShowHidden), typeof(bool), typeof(DirectoryListBox),
            new PropertyMetadata(false, OnShowHiddenChanged));

        /// <summary>
        /// Gets or sets whether hidden files and folders are shown.
        /// </summary>
        public bool ShowHidden
        {
            get => (bool)GetValue(ShowHiddenProperty);
            set => SetValue(ShowHiddenProperty, value);
        }

        public static readonly DependencyProperty IconHeightProperty = DependencyProperty.Register(
            nameof(IconHeight), typeof(double), typeof(DirectoryListBox),
            new PropertyMetadata(24.0));

        public static readonly DependencyProperty IconWidthProperty = DependencyProperty.Register(
            nameof(IconWidth), typeof(double), typeof(DirectoryListBox),
            new PropertyMetadata(24.0));

        /// <summary>
        /// Gets or sets the height of the file/folder icon.
        /// </summary>
        public double IconHeight
        {
            get => (double)GetValue(IconHeightProperty);
            set => SetValue(IconHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the file/folder icon.
        /// </summary>
        public double IconWidth
        {
            get => (double)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        public static readonly DependencyProperty ShowFilesProperty = DependencyProperty.Register(
            nameof(ShowFiles), typeof(bool), typeof(DirectoryListBox),
            new PropertyMetadata(true, OnShowFilesChanged));

        /// <summary>
        /// Gets or sets whether files are shown in the directory list.
        /// </summary>
        public bool ShowFiles
        {
            get => (bool)GetValue(ShowFilesProperty);
            set => SetValue(ShowFilesProperty, value);
        }

        public ObservableCollection<DirectoryListItem> ItemsSourceInternal { get; } = new();

        public DirectoryListBox()
        {
            this.ItemsSource = ItemsSourceInternal;
            this.MouseDoubleClick += DirectoryListBox_MouseDoubleClick;
            Loaded += DirectoryListBox_Loaded;
        }

        private void DirectoryListBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentPath))
            {
                try
                {
                    if (Directory.Exists("C:\\"))
                    {
                        CurrentPath = "C:\\";
                    }
                }
                catch { /* ignore */ }
            }
            Loaded -= DirectoryListBox_Loaded;
        }

        private static void OnCurrentPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DirectoryListBox listBox)
            {
                listBox.LoadDirectory(e.NewValue as string);
            }
        }

        private static void OnShowHiddenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DirectoryListBox listBox)
            {
                listBox.LoadDirectory(listBox.CurrentPath);
            }
        }

        private static void OnShowFilesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DirectoryListBox listBox)
            {
                listBox.LoadDirectory(listBox.CurrentPath);
            }
        }

        private void LoadDirectory(string? path)
        {
            ItemsSourceInternal.Clear();
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            var dirInfo = new DirectoryInfo(path);
            // Add .. if not root
            if (dirInfo.Parent != null)
            {
                ItemsSourceInternal.Add(new DirectoryListItem("..", dirInfo.Parent.FullName, DirectoryListItemType.Up));
            }
            // Add directories
            foreach (var dir in dirInfo.GetDirectories().OrderBy(d => d.Name))
            {
                if (!ShowHidden && dir.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;
                ItemsSourceInternal.Add(new DirectoryListItem(dir.Name, dir.FullName, DirectoryListItemType.Directory));
            }
            // Only add files if ShowFiles is true
            if (ShowFiles)
            {
                foreach (var file in dirInfo.GetFiles().OrderBy(f => f.Name))
                {
                    if (!ShowHidden && file.Attributes.HasFlag(FileAttributes.Hidden))
                        continue;
                    ItemsSourceInternal.Add(new DirectoryListItem(file.Name, file.FullName, DirectoryListItemType.File));
                }
            }
        }

        /// <summary>
        /// Occurs when the selected folder changes (including navigation).
        /// </summary>
        public event EventHandler<DirectoryListItem>? SelectedFolderChanged;

        /// <summary>
        /// Occurs when a file is double-clicked.
        /// </summary>
        public event EventHandler<DirectoryListItem>? FileDoubleClicked;

        private void DirectoryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedItem is DirectoryListItem item)
            {
                if (item.Type == DirectoryListItemType.Directory || item.Type == DirectoryListItemType.Up)
                {
                    // Before changing CurrentPath, check if the target directory is accessible
                    if (IsDirectoryAccessible(item.FullPath))
                    {
                        CurrentPath = item.FullPath;
                        SelectedFolderChanged?.Invoke(this, item);
                    }
                    else
                    {
                        // Optionally, show an error message or add a temporary error item
                        // MessageBox.Show($"Access denied or error accessing: {item.FullPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (item.Type == DirectoryListItemType.File)
                {
                    FileDoubleClicked?.Invoke(this, item);
                }
            }
        }

        /// <summary>
        /// Checks if a directory is accessible by attempting to enumerate its contents.
        /// Handles junctions/reparse points and access denied errors.
        /// </summary>
        private bool IsDirectoryAccessible(string path)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                // Check for reparse point (junction)
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    // Try to enumerate, but catch errors
                    var _ = dirInfo.GetFileSystemInfos().FirstOrDefault();
                }
                else
                {
                    // Try to enumerate
                    var _ = dirInfo.GetFileSystemInfos().FirstOrDefault();
                }
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public enum DirectoryListItemType
    {
        Directory,
        File,
        Up
    }

    public class DirectoryListItem
    {
        public string Name { get; }
        public string FullPath { get; }
        public DirectoryListItemType Type { get; }
        public DirectoryListItem(string name, string fullPath, DirectoryListItemType type)
        {
            Name = name;
            FullPath = fullPath;
            Type = type;
        }
        public override string ToString() => Name;
    }
}
