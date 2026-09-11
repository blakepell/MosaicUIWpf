/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class LineGraphExample
    {
        private static readonly Random Random = new();

        private static readonly DateTime Start = new(2026, 1, 1);

        private int _nextSeriesNumber = 4;

        public LineGraphExample()
        {
            // Thirty days of readings for three regions. Series that leave Brush null are coloured from the
            // graph's palette in the order they appear.
            this.DailySeries = new ObservableCollection<LineGraphSeries>
            {
                BuildDailySeries("North America", 30, 400, 650),
                BuildDailySeries("Europe", 30, 250, 480),
                BuildDailySeries("Asia Pacific", 30, 120, 360)
            };

            // Two days of hourly readings. Series can carry their own brush and thickness, and here the
            // theme's status brushes are used so they follow a theme switch.
            var cpu = new LineGraphSeries("CPU", Brush(MosaicTheme.ErrorBrush)) { LineThickness = 3 };
            var memory = new LineGraphSeries("Memory", Brush(MosaicTheme.InfoBrush));

            for (var hour = 0; hour <= 48; hour += 2)
            {
                var time = Start.AddHours(hour);
                cpu.Add(time, Random.Next(20, 95));
                memory.Add(time, 45 + Random.Next(0, 30));
            }

            this.HourlySeries = new ObservableCollection<LineGraphSeries> { cpu, memory };

            InitializeComponent();
        }

        /// <summary>
        /// The data behind the first graph. The graph listens to the collection, to each series and to each
        /// point, so the buttons below only have to change the data.
        /// </summary>
        public ObservableCollection<LineGraphSeries> DailySeries { get; }

        /// <summary>
        /// The data behind the second graph, which pins the time unit to hours.
        /// </summary>
        public ObservableCollection<LineGraphSeries> HourlySeries { get; }

        /// <summary>
        /// Builds a series of one reading per day starting on the first of January.
        /// </summary>
        private static LineGraphSeries BuildDailySeries(string caption, int days, int min, int max)
        {
            var series = new LineGraphSeries(caption);

            for (var day = 0; day < days; day++)
            {
                series.Add(Start.AddDays(day), Random.Next(min, max));
            }

            return series;
        }

        /// <summary>
        /// Resolves a theme brush so the sample colours follow a theme switch.
        /// </summary>
        private static SolidColorBrush? Brush(ComponentResourceKey key)
        {
            return Application.Current?.TryFindResource(key) as SolidColorBrush;
        }

        /// <summary>
        /// The clicked series arrives on the event args. Clicking a legend entry raises the same event.
        /// </summary>
        private void Graph_SeriesClicked(object sender, LineGraphSeriesEventArgs e)
        {
            this.ClickResult.Text = $"SeriesClicked: \"{e.Series.Caption}\" with {e.Series.Points.Count} points.";
        }

        /// <summary>
        /// The clicked point and the series it belongs to both arrive on the event args.
        /// </summary>
        private void Graph_PointClicked(object sender, LineGraphPointEventArgs e)
        {
            this.ClickResult.Text = $"PointClicked: \"{e.Series.Caption}\" on {e.Point.Time:MMM d, yyyy} had a value of {e.Point.Value:N0}.";
        }

        /// <summary>
        /// Mutates the existing points rather than replacing them, which shows the graph repainting off the
        /// point level PropertyChanged notifications.
        /// </summary>
        private void RandomizeValues_Click(object sender, RoutedEventArgs e)
        {
            foreach (var series in this.DailySeries)
            {
                foreach (var point in series.Points)
                {
                    point.Value = Random.Next(100, 700);
                }
            }
        }

        /// <summary>
        /// Appends one more day to every series, which extends the X-axis.
        /// </summary>
        private void AddDay_Click(object sender, RoutedEventArgs e)
        {
            foreach (var series in this.DailySeries)
            {
                var last = series.Points.Count > 0 ? series.Points.Max(p => p.Time) : Start;
                series.Add(last.AddDays(1), Random.Next(100, 700));
            }
        }

        private void AddSeries_Click(object sender, RoutedEventArgs e)
        {
            var days = this.DailySeries.Count > 0 ? this.DailySeries[0].Points.Count : 30;
            this.DailySeries.Add(BuildDailySeries($"Region {_nextSeriesNumber++}", days, 100, 700));
        }

        private void RemoveSeries_Click(object sender, RoutedEventArgs e)
        {
            if (this.DailySeries.Count > 1)
            {
                this.DailySeries.RemoveAt(this.DailySeries.Count - 1);
            }
        }

        /// <summary>
        /// An empty title collapses the title row so the plot reclaims the space.
        /// </summary>
        private void ToggleTitle_Click(object sender, RoutedEventArgs e)
        {
            this.Graph.Title = string.IsNullOrEmpty(this.Graph.Title) ? "Daily Active Users by Region" : null;
        }
    }
}
