/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// A user control to view and edit color and brush resources in a XAML ResourceDictionary.
    /// It preserves the original XAML file's layout, comments, and formatting.
    /// </summary>
    /// <remarks>
    /// How to use:
    /// 1. Place the ColorPaletteEditor control in your view.
    /// 2. Call `LoadDictionary(string filePath)` to load and parse a XAML file.
    /// 3. Call `SaveAsync()` to save the modified color values back to the file.
    ///
    /// The control handles file I/O internally via Load/Save buttons but can be
    /// driven programmatically.
    /// </remarks>
    public partial class ColorPaletteEditor : UserControl
    {
        private string _currentFilePath;
        private string _originalXamlContent;
        public ObservableCollection<XamlResourceItem> ResourceItems { get; set; } = new();

        public ColorPaletteEditor()
        {
            InitializeComponent();
            
            // Set the DataContext to this UserControl so XAML binding can work
            DataContext = this;
            
            // Add some test data to verify the ListView is working
            AddTestData();
        }
        
        private void AddTestData()
        {
            // Create a dummy XElement for testing
            var testElement = new XElement("Color", "#FF0000");
            testElement.SetAttributeValue(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml"), "TestColor");
            
            ResourceItems.Add(new XamlResourceItem("TestColor1", "Color", "#FF0000", testElement.FirstNode));
            ResourceItems.Add(new XamlResourceItem("TestColor2", "SolidColorBrush", "#00FF00", testElement));
            ResourceItems.Add(new XamlResourceItem("TestColor3", "Color", "#0000FF", testElement.FirstNode));
        }

        /// <summary>
        /// Loads a XAML ResourceDictionary from the specified file path, parses it,
        /// and populates the UI with editable color/brush resources.
        /// </summary>
        /// <param name="filePath">The path to the XAML file.</param>
        public async Task LoadDictionary(string filePath)
        {
            try
            {
                _currentFilePath = filePath;
                _originalXamlContent = await File.ReadAllTextAsync(filePath);

                var items = XamlColorParser.Parse(_originalXamlContent);

                ResourceItems.Clear();
                foreach (var item in items)
                {
                    ResourceItems.Add(item);
                }

                StatusTextBlock.Text = $"Loaded {items.Count} editable resources from {System.IO.Path.GetFileName(filePath)}.";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error loading file: {ex.Message}";
            }
        }

        /// <summary>
        /// Saves the modified color and brush values back to the original file,
        /// preserving all formatting.
        /// </summary>
        /// <returns>A Task that returns true if saving was successful, otherwise false.</returns>
        public async Task<bool> SaveAsync()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || _originalXamlContent == null)
            {
                StatusTextBlock.Text = "No file is loaded. Please load a dictionary first.";
                return false;
            }

            try
            {
                var modifiedItems = ResourceItems.Where(item => item.IsModified).ToList();
                if (modifiedItems.Count == 0)
                {
                    StatusTextBlock.Text = "No changes to save.";
                    return true;
                }

                string updatedXaml = XamlColorParser.UpdateXamlContent(_originalXamlContent, modifiedItems);
                await File.WriteAllTextAsync(_currentFilePath, updatedXaml);

                // Update the original content and state after a successful save
                _originalXamlContent = updatedXaml;
                foreach (var item in modifiedItems)
                {
                    // This effectively resets the 'IsModified' state
                    var newItem = new XamlResourceItem(item.Key, item.ResourceType, item.CurrentValue, item.SourceObject);
                    int index = ResourceItems.IndexOf(item);
                    ResourceItems[index] = newItem;
                }

                StatusTextBlock.Text = $"Successfully saved {modifiedItems.Count} changes to {System.IO.Path.GetFileName(_currentFilePath)}.";
                return true;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error saving file: {ex.Message}";
                return false;
            }
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "XAML Files (*.xaml)|*.xaml|All files (*.*)|*.*",
                Title = "Open Resource Dictionary"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadDictionary(openFileDialog.FileName);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync();
        }
    }
}
