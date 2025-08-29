/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class DriveComboBoxExample : UserControl
    {
        private TextBlock? _textBlockSelected;
        private Mosaic.UI.Wpf.Controls.DirectoryListBox? _directoryListBox;

        public DriveComboBoxExample()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _textBlockSelected = FindChild<TextBlock>(this, "TextBlockSelected");
            _directoryListBox = FindChild<Mosaic.UI.Wpf.Controls.DirectoryListBox>(this, "DirectoryListBox");
        }

        private void DriveComboBox_OnSelectedDriveChanged(object? sender, DriveInfo e)
        {
            if (_textBlockSelected != null)
            {
                _textBlockSelected.Text = $"{e.Name} is a {e.DriveType} drive with {e.TotalFreeSpace / (1024 * 1024 * 1024.0):F2} GB free of {e.TotalSize / (1024 * 1024 * 1024.0):F2} GB total.";
            }
            if (_directoryListBox != null)
            {
                _directoryListBox.CurrentPath = e.Name;
            }
        }

        private static T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t && t.Name == childName)
                {
                    return t;
                }

                var result = FindChild<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void DirectoryListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_textBlockSelected != null)
            {
                if (e.AddedItems.Count > 0 && e.AddedItems[0] is Mosaic.UI.Wpf.Controls.DirectoryListItem item)
                {
                    _textBlockSelected.Text = $"{item.FullPath} selected.";
                }
                else
                {
                    _textBlockSelected.Text = "No directory selected.";
                }
                //_textBlockSelected.Text = $"{e.AddedItems} is a {e.DriveType} drive with {e.TotalFreeSpace / (1024 * 1024 * 1024.0):F2} GB free of {e.TotalSize / (1024 * 1024 * 1024.0):F2} GB total.";
            }
        }
    }
}
