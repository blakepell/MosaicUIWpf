/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ColumnChartExample
    {
        private static readonly Random Random = new();

        private int _nextColumnNumber = 7;

        public ColumnChartExample()
        {
            this.ChartItems = new ObservableCollection<ColumnChartItem>
            {
                new("Jan", 42),
                new("Feb", 58),
                new("Mar", 31),
                new("Apr", 76),
                new("May", 64),
                new("Jun", 88)
            };

            // Individual columns can set their own brush. Anything left null uses the chart's ColumnBrush,
            // which defaults to the Mosaic theme accent.
            this.StatusItems = new ObservableCollection<ColumnChartItem>
            {
                new("Passed", 128, Brush(MosaicTheme.SuccessBrush)),
                new("Warnings", 34, Brush(MosaicTheme.WarningBrush)),
                new("Failed", 12, Brush(MosaicTheme.ErrorBrush)),
                new("Skipped", 21)
            };

            InitializeComponent();
        }

        /// <summary>
        /// The data behind the first chart. The chart listens to the collection and to each item, so the
        /// buttons below only have to change the data.
        /// </summary>
        public ObservableCollection<ColumnChartItem> ChartItems { get; }

        /// <summary>
        /// The data behind the second chart, which colors each column individually.
        /// </summary>
        public ObservableCollection<ColumnChartItem> StatusItems { get; }

        /// <summary>
        /// Resolves a theme brush so the sample colors follow a theme switch.
        /// </summary>
        private static SolidColorBrush? Brush(ComponentResourceKey key)
        {
            return Application.Current?.TryFindResource(key) as SolidColorBrush;
        }

        /// <summary>
        /// The clicked column arrives on the event args, so there is no need to map a hit position back to
        /// the data.
        /// </summary>
        private void Chart_ColumnClicked(object sender, ColumnChartItemEventArgs e)
        {
            this.ClickResult.Text = $"Clicked column \"{e.Item.Name}\" with a value of {e.Item.Value}.";
        }

        /// <summary>
        /// Mutates the existing items rather than replacing them, which shows the chart repainting off the
        /// item level PropertyChanged notifications.
        /// </summary>
        private void RandomizeValues_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.ChartItems)
            {
                item.Value = Random.Next(10, 100);
            }
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            this.ChartItems.Add(new ColumnChartItem($"Col {_nextColumnNumber++}", Random.Next(10, 100)));
        }

        private void RemoveColumn_Click(object sender, RoutedEventArgs e)
        {
            if (this.ChartItems.Count > 1)
            {
                this.ChartItems.RemoveAt(this.ChartItems.Count - 1);
            }
        }
    }
}
