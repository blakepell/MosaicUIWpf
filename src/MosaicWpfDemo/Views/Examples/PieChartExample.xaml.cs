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
    public partial class PieChartExample
    {
        private static readonly Random Random = new();

        private int _nextSliceNumber = 5;

        public PieChartExample()
        {
            // The values are raw numbers, the chart works out each slice's share of the pie from the total.
            this.Categories = new ObservableCollection<PieCategory>
            {
                new("Windows", 412),
                new("macOS", 168),
                new("Linux", 96),
                new("Other", 24)
            };

            // Individual categories can set their own brush. Anything left null gets a color from the chart's
            // palette based on its position in the collection.
            this.StatusCategories = new ObservableCollection<PieCategory>
            {
                new("Passed", 128, Brush(MosaicTheme.SuccessBrush)),
                new("Warnings", 34, Brush(MosaicTheme.WarningBrush)),
                new("Failed", 12, Brush(MosaicTheme.ErrorBrush)),
                new("Skipped", 21)
            };

            InitializeComponent();
        }

        /// <summary>
        /// The data behind the first chart. The chart listens to the collection and to each category, so the
        /// buttons below only have to change the data.
        /// </summary>
        public ObservableCollection<PieCategory> Categories { get; }

        /// <summary>
        /// The data behind the second chart, which colors each slice individually.
        /// </summary>
        public ObservableCollection<PieCategory> StatusCategories { get; }

        /// <summary>
        /// Resolves a theme brush so the sample colors follow a theme switch.
        /// </summary>
        private static SolidColorBrush? Brush(ComponentResourceKey key)
        {
            return Application.Current?.TryFindResource(key) as SolidColorBrush;
        }

        /// <summary>
        /// The clicked category arrives on the event args, so there is no need to map a hit position back to
        /// the data.
        /// </summary>
        private void Chart_SliceClicked(object sender, PieCategoryEventArgs e)
        {
            this.ClickResult.Text = $"Clicked slice \"{e.Category.Name}\" with a value of {e.Category.Value} ({e.Category.Percentage:0.#}% of the total).";
        }

        /// <summary>
        /// Mutates the existing categories rather than replacing them, which shows the chart repainting off
        /// the item level PropertyChanged notifications.
        /// </summary>
        private void RandomizeValues_Click(object sender, RoutedEventArgs e)
        {
            foreach (var category in this.Categories)
            {
                category.Value = Random.Next(10, 400);
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            this.Categories.Add(new PieCategory($"Slice {_nextSliceNumber++}", Random.Next(10, 400)));
        }

        private void RemoveCategory_Click(object sender, RoutedEventArgs e)
        {
            if (this.Categories.Count > 1)
            {
                this.Categories.RemoveAt(this.Categories.Count - 1);
            }
        }
    }
}
