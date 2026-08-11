/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single column rendered by a <see cref="ColumnChart"/>. Changing any property raises
    /// <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>, which causes the owning chart
    /// to repaint. A chart bound to an <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/> of
    /// these therefore stays in sync with its data without an explicit refresh call.
    /// </summary>
    public partial class ColumnChartItem : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnChartItem"/> class.
        /// </summary>
        public ColumnChartItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnChartItem"/> class.
        /// </summary>
        /// <param name="name">The label rendered beneath the column on the X-axis.</param>
        /// <param name="value">The value the column represents.</param>
        /// <param name="columnBrush">The brush used to fill the column, or null to use the chart's <see cref="ColumnChart.ColumnBrush"/>.</param>
        public ColumnChartItem(string name, int value, Brush? columnBrush = null)
        {
            this.Name = name;
            this.Value = value;
            this.ColumnBrush = columnBrush;
        }

        /// <summary>
        /// Gets or sets the label rendered beneath the column on the X-axis.
        /// </summary>
        [Category("Common")]
        [Description("The label rendered beneath the column on the X-axis.")]
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value the column represents. The largest value across the chart's items determines
        /// the scale of the Y-axis.
        /// </summary>
        [Category("Common")]
        [Description("The value the column represents.")]
        [ObservableProperty]
        public partial int Value { get; set; }

        /// <summary>
        /// Gets or sets the brush used to fill this column. When null the owning chart's
        /// <see cref="ColumnChart.ColumnBrush"/> is used, which allows a single column to be highlighted
        /// without having to colour every other column.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used to fill this column. Null falls back to the chart's ColumnBrush.")]
        [ObservableProperty]
        public partial Brush? ColumnBrush { get; set; }

        /// <summary>
        /// Gets or sets a custom object for this particular line.
        /// </summary>
        [Category("Common")]
        [Description("A custom object associated with this column.")]
        [ObservableProperty]
        public partial object? Tag { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Name}: {this.Value}";
        }
    }

    /// <summary>
    /// Carries the <see cref="ColumnChartItem"/> whose column was clicked to handlers of
    /// <see cref="ColumnChart.ColumnClicked"/>.
    /// </summary>
    public class ColumnChartItemEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnChartItemEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The chart raising the event.</param>
        /// <param name="item">The item backing the column that was clicked.</param>
        public ColumnChartItemEventArgs(RoutedEvent routedEvent, object source, ColumnChartItem item)
            : base(routedEvent, source)
        {
            this.Item = item;
        }

        /// <summary>
        /// The item backing the column that was clicked.
        /// </summary>
        public ColumnChartItem Item { get; }
    }
}
