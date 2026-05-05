/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class CheckBoxListExample : INotifyPropertyChanged
    {
        private string _selectedTaskSummary = "None";
        private string _selectedPermissionSummary = "None";
        private string _selectedFeatureSummary = "None";

        public ObservableCollection<string> TaskOptions { get; } = new()
        {
            "Inbox triage",
            "Prepare deployment notes",
            "Review pull requests",
            "Update customer follow-ups",
            "Audit stale issues",
            "Publish release notes"
        };

        public ObservableCollection<PermissionOption> PermissionOptions { get; } = new()
        {
            new("read", "Read"),
            new("write", "Write"),
            new("approve", "Approve"),
            new("archive", "Archive")
        };

        public ObservableCollection<FeatureOption> FeatureOptions { get; } = new()
        {
            new("Keyboard selection", "Arrow keys move through the list and space toggles the focused item."),
            new("SelectedItems", "Selections are still available through the inherited ListBox SelectedItems collection."),
            new("ItemTemplate", "Consumer item templates are preserved inside the checkbox row container."),
            new("Theming", "The item container uses Mosaic theme resources for hover, selection, and disabled states.")
        };

        public string SelectedTaskSummary
        {
            get => _selectedTaskSummary;
            set => SetField(ref _selectedTaskSummary, value);
        }

        public string SelectedPermissionSummary
        {
            get => _selectedPermissionSummary;
            set => SetField(ref _selectedPermissionSummary, value);
        }

        public string SelectedFeatureSummary
        {
            get => _selectedFeatureSummary;
            set => SetField(ref _selectedFeatureSummary, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public CheckBoxListExample()
        {
            DataContext = this;
            InitializeComponent();

            TaskList.SelectedItems.Add(TaskOptions[0]);
            TaskList.SelectedItems.Add(TaskOptions[2]);
            PermissionList.SelectedItems.Add(PermissionOptions[0]);
            PermissionList.SelectedItems.Add(PermissionOptions[2]);
            FeatureList.SelectedItems.Add(FeatureOptions[1]);
            FeatureList.SelectedItems.Add(FeatureOptions[2]);

            UpdateSelectedSummaries();
        }

        private void CheckBoxList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedSummaries();
        }

        private void UpdateSelectedSummaries()
        {
            SelectedTaskSummary = FormatSelection(TaskList.SelectedItems, item => item.ToString() ?? "");
            SelectedPermissionSummary = FormatSelection(PermissionList.SelectedItems, item => ((PermissionOption)item).Key);
            SelectedFeatureSummary = FormatSelection(FeatureList.SelectedItems, item => ((FeatureOption)item).Name);
        }

        private static string FormatSelection(IList selectedItems, Func<object, string> formatItem)
        {
            if (selectedItems.Count == 0)
            {
                return "None";
            }

            return string.Join(", ", selectedItems.Cast<object>().Select(formatItem));
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public sealed class PermissionOption
        {
            public string Key { get; }

            public string Name { get; }

            public PermissionOption(string key, string name)
            {
                Key = key;
                Name = name;
            }
        }

        public sealed class FeatureOption
        {
            public string Name { get; }

            public string Description { get; }

            public FeatureOption(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }
    }
}
