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
    public partial class HorizontalListBoxExample : INotifyPropertyChanged
    {
        private string _selectedDaySummary = "None";
        private string _selectedViewSummary = "None";
        private string _selectedPrioritySummary = "None";
        private string _selectedChannelSummary = "None";

        public ObservableCollection<DayOption> Days { get; } = new()
        {
            new("Sun", "Sunday"),
            new("Mon", "Monday"),
            new("Tue", "Tuesday"),
            new("Wed", "Wednesday"),
            new("Thu", "Thursday"),
            new("Fri", "Friday"),
            new("Sat", "Saturday")
        };

        public ObservableCollection<string> Views { get; } = new()
        {
            "Day",
            "Week",
            "Month",
            "Year"
        };

        public ObservableCollection<string> Priorities { get; } = new()
        {
            "Low",
            "Normal",
            "High",
            "Critical"
        };

        public ObservableCollection<ChannelOption> Channels { get; } = new()
        {
            new("\U0001F4E7", "Email"),
            new("\U0001F4AC", "Chat"),
            new("\U0001F4F1", "SMS"),
            new("\U0001F514", "Push")
        };

        public string SelectedDaySummary
        {
            get => _selectedDaySummary;
            set => SetField(ref _selectedDaySummary, value);
        }

        public string SelectedViewSummary
        {
            get => _selectedViewSummary;
            set => SetField(ref _selectedViewSummary, value);
        }

        public string SelectedPrioritySummary
        {
            get => _selectedPrioritySummary;
            set => SetField(ref _selectedPrioritySummary, value);
        }

        public string SelectedChannelSummary
        {
            get => _selectedChannelSummary;
            set => SetField(ref _selectedChannelSummary, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public HorizontalListBoxExample()
        {
            DataContext = this;
            InitializeComponent();

            // Weekdays are toggled on, the weekend is left off.
            for (int i = 1; i <= 5; i++)
            {
                DayList.SelectedItems.Add(Days[i]);
            }

            ViewList.SelectedItem = Views[1];
            PriorityList.SelectedItems.Add(Priorities[1]);
            PriorityList.SelectedItems.Add(Priorities[2]);
            ChannelList.SelectedItems.Add(Channels[0]);

            UpdateSelectedSummaries();
        }

        private void HorizontalListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectedSummaries();
        }

        private void UpdateSelectedSummaries()
        {
            SelectedDaySummary = FormatSelection(DayList.SelectedItems, item => ((DayOption)item).Name);
            SelectedViewSummary = FormatSelection(ViewList.SelectedItems, item => item.ToString() ?? "");
            SelectedPrioritySummary = FormatSelection(PriorityList.SelectedItems, item => item.ToString() ?? "");
            SelectedChannelSummary = FormatSelection(ChannelList.SelectedItems, item => ((ChannelOption)item).Name);
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

        public sealed class DayOption
        {
            public string Abbreviation { get; }

            public string Name { get; }

            public DayOption(string abbreviation, string name)
            {
                Abbreviation = abbreviation;
                Name = name;
            }
        }

        public sealed class ChannelOption
        {
            public string Glyph { get; }

            public string Name { get; }

            public ChannelOption(string glyph, string name)
            {
                Glyph = glyph;
                Name = name;
            }
        }
    }
}
