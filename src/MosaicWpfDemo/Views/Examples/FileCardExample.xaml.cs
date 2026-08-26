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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    [ObservableObject]
    public partial class FileCardExample
    {
        /// <summary>
        /// The most recent card activation, shown at the bottom of the example.
        /// </summary>
        [ObservableProperty]
        private string _lastAction = string.Empty;

        public FileCardExample()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Handles the bubbling <see cref="FileCard.Click"/> routed event.
        /// </summary>
        private void FileCard_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is FileCard card)
            {
                LastAction = $"Click event: {card.FilePath} (exists: {card.FileExists})";
            }
        }

        /// <summary>
        /// Invoked by the cards bound to <c>OpenFileCommand</c>. The card supplies its own FilePath
        /// because no CommandParameter was set.
        /// </summary>
        /// <param name="filePath">The path of the file whose card was clicked.</param>
        [RelayCommand]
        private void OpenFile(string? filePath)
        {
            LastAction = $"Command executed with: {filePath}";
        }
    }
}
