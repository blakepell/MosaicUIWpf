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
using System.Collections.Specialized;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// One line on a <see cref="LineGraph"/>: a caption, a colour and the set of time stamped values that
    /// make up the line. Changing any property on the series, adding or removing a point, or editing a
    /// property on any point raises <see cref="INotifyPropertyChanged.PropertyChanged"/>, which causes the
    /// owning graph to repaint. A graph bound to an <see cref="ObservableCollection{T}"/> of these therefore
    /// stays in sync with its data without an explicit refresh call.
    /// </summary>
    public partial class LineGraphSeries : ObservableObject
    {
        /// <summary>
        /// The points currently hooked for <see cref="INotifyPropertyChanged"/>. Tracked separately from
        /// <see cref="Points"/> so a <see cref="NotifyCollectionChangedAction.Reset"/> (which carries no old
        /// items) can still be unhooked cleanly.
        /// </summary>
        private readonly List<LineGraphPoint> _hookedPoints = new();

        /// <summary>
        /// Backing field for the palette colour assigned by the owning graph.
        /// </summary>
        private Brush? _paletteBrush;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraphSeries"/> class.
        /// </summary>
        public LineGraphSeries()
        {
            this.Points = new ObservableCollection<LineGraphPoint>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraphSeries"/> class.
        /// </summary>
        /// <param name="caption">The caption shown for the line in the legend and tooltips.</param>
        /// <param name="brush">The brush used to draw the line, or null to let the graph pick one.</param>
        public LineGraphSeries(string caption, Brush? brush = null) : this()
        {
            this.Caption = caption;
            this.Brush = brush;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraphSeries"/> class.
        /// </summary>
        /// <param name="caption">The caption shown for the line in the legend and tooltips.</param>
        /// <param name="points">The points that make up the line.</param>
        /// <param name="brush">The brush used to draw the line, or null to let the graph pick one.</param>
        public LineGraphSeries(string caption, IEnumerable<LineGraphPoint> points, Brush? brush = null) : this(caption, brush)
        {
            foreach (var point in points)
            {
                this.Points.Add(point);
            }
        }

        /// <summary>
        /// Gets or sets the caption shown for the line in the legend and in point tooltips.
        /// </summary>
        [Category("Common")]
        [Description("The caption shown for the line in the legend and in point tooltips.")]
        [ObservableProperty]
        public partial string Caption { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the brush used to draw this line and its points. When null the owning graph assigns
        /// one from its palette based on the series' position in the collection.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used to draw this line. Null lets the graph pick one from its palette.")]
        [ObservableProperty]
        public partial Brush? Brush { get; set; }

        /// <summary>
        /// Gets or sets the thickness of this line. When null the graph's <see cref="LineGraph.LineThickness"/>
        /// is used.
        /// </summary>
        [Category("Appearance")]
        [Description("The thickness of this line. Null falls back to the graph's LineThickness.")]
        [ObservableProperty]
        public partial double? LineThickness { get; set; }

        /// <summary>
        /// Gets or sets the time stamped values that make up the line. The series observes the collection and
        /// each point in it, so the graph repaints when a point is added, removed or edited.
        /// </summary>
        [Category("Common")]
        [Description("The time stamped values that make up the line.")]
        [ObservableProperty]
        public partial ObservableCollection<LineGraphPoint> Points { get; set; }

        /// <summary>
        /// Gets or sets a custom object for this particular series.
        /// </summary>
        [Category("Common")]
        [Description("A custom object associated with this series.")]
        [ObservableProperty]
        public partial object? Tag { get; set; }

        /// <summary>
        /// Gets the brush the owning <see cref="LineGraph"/> actually painted this line with, which is
        /// <see cref="Brush"/> when one was supplied and a palette colour otherwise. The legend binds to this
        /// so its swatch matches the line.
        /// </summary>
        [Browsable(false)]
        public Brush? EffectiveBrush => this.Brush ?? _paletteBrush;

        /// <summary>
        /// Adds a point to the series.
        /// </summary>
        /// <param name="time">The moment the value was observed.</param>
        /// <param name="value">The value at that moment.</param>
        /// <returns>The point that was added.</returns>
        public LineGraphPoint Add(DateTime time, double value)
        {
            var point = new LineGraphPoint(time, value);
            this.Points.Add(point);
            return point;
        }

        /// <summary>
        /// Assigns the fallback colour the owning graph picked for this series.
        /// </summary>
        /// <param name="brush">The palette brush to fall back to when <see cref="Brush"/> is null.</param>
        internal void SetPaletteBrush(Brush? brush)
        {
            if (ReferenceEquals(_paletteBrush, brush))
            {
                return;
            }

            _paletteBrush = brush;
            this.OnPropertyChanged(nameof(this.EffectiveBrush));
        }

        // Keeps the legend's swatch in step when a series is given (or loses) an explicit colour.
        partial void OnBrushChanged(Brush? value)
        {
            this.OnPropertyChanged(nameof(this.EffectiveBrush));
        }

        // Moves the subscriptions from the old points collection onto the new one.
        partial void OnPointsChanged(ObservableCollection<LineGraphPoint>? oldValue, ObservableCollection<LineGraphPoint> newValue)
        {
            if (oldValue is not null)
            {
                oldValue.CollectionChanged -= this.OnPointsCollectionChanged;
            }

            this.UnhookPoints();

            if (newValue is not null)
            {
                newValue.CollectionChanged += this.OnPointsCollectionChanged;
                this.HookPoints(newValue);
            }
        }

        /// <summary>
        /// Updates point subscriptions and notifies the owning graph when the points collection changes.
        /// </summary>
        /// <param name="sender">The collection that raised the notification.</param>
        /// <param name="e">The collection change data.</param>
        private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // A reset carries no old items, so rebuild the subscriptions from the current collection.
                this.UnhookPoints();
                this.HookPoints(this.Points);
            }
            else
            {
                if (e.OldItems is not null)
                {
                    foreach (var point in e.OldItems.OfType<LineGraphPoint>())
                    {
                        point.PropertyChanged -= this.OnPointPropertyChanged;
                        _hookedPoints.Remove(point);
                    }
                }

                if (e.NewItems is not null)
                {
                    this.HookPoints(e.NewItems.OfType<LineGraphPoint>());
                }
            }

            // Surfacing the change as a property change on the series means the graph only has to watch the
            // series, not every point inside it.
            this.OnPropertyChanged(nameof(this.Points));
        }

        /// <summary>
        /// Notifies the owning graph when a point in the series changes.
        /// </summary>
        /// <param name="sender">The point that raised the notification.</param>
        /// <param name="e">The property change data.</param>
        private void OnPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.OnPropertyChanged(nameof(this.Points));
        }

        /// <summary>
        /// Subscribes to property changes for the specified points.
        /// </summary>
        /// <param name="points">The points to observe.</param>
        private void HookPoints(IEnumerable<LineGraphPoint>? points)
        {
            if (points is null)
            {
                return;
            }

            foreach (var point in points)
            {
                point.PropertyChanged += this.OnPointPropertyChanged;
                _hookedPoints.Add(point);
            }
        }

        /// <summary>
        /// Unsubscribes from property changes for all currently observed points.
        /// </summary>
        private void UnhookPoints()
        {
            foreach (var point in _hookedPoints)
            {
                point.PropertyChanged -= this.OnPointPropertyChanged;
            }

            _hookedPoints.Clear();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Caption} ({this.Points?.Count ?? 0} points)";
        }
    }

    /// <summary>
    /// A single time stamped value on a <see cref="LineGraphSeries"/>. Changing any property raises
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/>, which the owning series forwards to the graph
    /// so it repaints.
    /// </summary>
    public partial class LineGraphPoint : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraphPoint"/> class.
        /// </summary>
        public LineGraphPoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraphPoint"/> class.
        /// </summary>
        /// <param name="time">The moment the value was observed.</param>
        /// <param name="value">The value at that moment.</param>
        /// <param name="label">An optional label shown in the point's tooltip in place of the formatted time and value.</param>
        public LineGraphPoint(DateTime time, double value, string? label = null)
        {
            this.Time = time;
            this.Value = value;
            this.Label = label;
        }

        /// <summary>
        /// Gets or sets the moment the value was observed. This positions the point along the X-axis.
        /// </summary>
        [Category("Common")]
        [Description("The moment the value was observed, which positions the point along the X-axis.")]
        [ObservableProperty]
        public partial DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the value at <see cref="Time"/>. This positions the point along the Y-axis.
        /// </summary>
        [Category("Common")]
        [Description("The value at the point's time, which positions the point along the Y-axis.")]
        [ObservableProperty]
        public partial double Value { get; set; }

        /// <summary>
        /// Gets or sets an optional label shown in the point's tooltip. When null the tooltip shows the
        /// formatted time and value.
        /// </summary>
        [Category("Common")]
        [Description("An optional label shown in the point's tooltip in place of the formatted time and value.")]
        [ObservableProperty]
        public partial string? Label { get; set; }

        /// <summary>
        /// Gets or sets a custom object for this particular point.
        /// </summary>
        [Category("Common")]
        [Description("A custom object associated with this point.")]
        [ObservableProperty]
        public partial object? Tag { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{this.Time:g}: {this.Value}";
        }
    }

    /// <summary>
    /// Carries the <see cref="LineGraphSeries"/> whose line was clicked to handlers of
    /// <see cref="LineGraph.SeriesClicked"/>.
    /// </summary>
    public class LineGraphSeriesEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraphSeriesEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The graph raising the event.</param>
        /// <param name="series">The series backing the line that was clicked.</param>
        public LineGraphSeriesEventArgs(RoutedEvent routedEvent, object source, LineGraphSeries series)
            : base(routedEvent, source)
        {
            this.Series = series;
        }

        /// <summary>
        /// The series backing the line that was clicked.
        /// </summary>
        public LineGraphSeries Series { get; }
    }

    /// <summary>
    /// Carries the <see cref="LineGraphPoint"/> that was clicked, and the <see cref="LineGraphSeries"/> it
    /// belongs to, to handlers of <see cref="LineGraph.PointClicked"/>.
    /// </summary>
    public class LineGraphPointEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraphPointEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event being raised.</param>
        /// <param name="source">The graph raising the event.</param>
        /// <param name="series">The series the clicked point belongs to.</param>
        /// <param name="point">The point that was clicked.</param>
        public LineGraphPointEventArgs(RoutedEvent routedEvent, object source, LineGraphSeries series, LineGraphPoint point)
            : base(routedEvent, source)
        {
            this.Series = series;
            this.Point = point;
        }

        /// <summary>
        /// The series the clicked point belongs to.
        /// </summary>
        public LineGraphSeries Series { get; }

        /// <summary>
        /// The point that was clicked.
        /// </summary>
        public LineGraphPoint Point { get; }
    }
}
