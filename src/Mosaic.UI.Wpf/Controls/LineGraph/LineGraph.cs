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
using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Shapes;

// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A line graph that plots one or more <see cref="LineGraphSeries"/> of time stamped values. Values run up
    /// the Y-axis on the left and time runs along the X-axis at the bottom, divided into minutes, hours, days,
    /// weeks, months or years (chosen automatically unless <see cref="TimeUnit"/> is set). Each series has its
    /// own colour and caption, shown in the legend beneath the plot, and an optional <see cref="Title"/> sits
    /// above it. The graph repaints whenever it is resized, whenever the series collection changes, and
    /// whenever a series or one of its points changes.
    /// </summary>
    /// <remarks>
    /// Clicking a line raises <see cref="SeriesClicked"/> with the <see cref="LineGraphSeries"/> behind it, and
    /// clicking a point raises <see cref="PointClicked"/> with both the point and its series. Colours default to
    /// Mosaic theme tokens via the control's default style, so the graph follows a light/dark switch without
    /// any code. A series that leaves <see cref="LineGraphSeries.Brush"/> null is painted with a colour from the
    /// graph's palette based on its position in the collection.
    /// </remarks>
    [TemplatePart(Name = PART_Canvas, Type = typeof(Canvas))]
    [TemplatePart(Name = PART_Title, Type = typeof(TextBlock))]
    [TemplatePart(Name = PART_Legend, Type = typeof(ItemsControl))]
    [DefaultProperty(nameof(Series))]
    [DefaultEvent(nameof(PointClicked))]
    public class LineGraph : Control
    {
        private const string PART_Canvas = "PART_Canvas";
        private const string PART_Title = "PART_Title";
        private const string PART_Legend = "PART_Legend";

        /// <summary>
        /// Space between the Y-axis labels and the axis line.
        /// </summary>
        private const double Y_AXIS_LABEL_MARGIN = 8;

        /// <summary>
        /// Space between the X-axis line and its labels.
        /// </summary>
        private const double X_AXIS_LABEL_MARGIN = 6;

        /// <summary>
        /// Minimum horizontal gap between two adjacent X-axis labels before the later one is dropped.
        /// </summary>
        private const double X_AXIS_LABEL_GAP = 12;

        /// <summary>
        /// Length of the small tick drawn on the X-axis beneath each labelled time.
        /// </summary>
        private const double TICK_LENGTH = 4;

        /// <summary>
        /// The colours used for series that do not specify their own <see cref="LineGraphSeries.Brush"/>.
        /// </summary>
        private static readonly Brush[] DefaultPalette;

        /// <summary>
        /// The series currently hooked for <see cref="INotifyPropertyChanged"/>. Tracked separately from
        /// <see cref="Series"/> so a <see cref="NotifyCollectionChangedAction.Reset"/> (which carries no old
        /// items) can still be unhooked cleanly.
        /// </summary>
        private readonly List<LineGraphSeries> _hookedSeries = new();

        /// <summary>
        /// Whether a repaint has already been queued onto the dispatcher. Coalesces the many notifications that
        /// a bulk update produces into a single paint.
        /// </summary>
        private bool _repaintQueued;

        private Canvas? _canvas;
        private ItemsControl? _legend;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Series"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeriesProperty =
            DependencyProperty.Register(nameof(Series), typeof(ObservableCollection<LineGraphSeries>), typeof(LineGraph),
                new FrameworkPropertyMetadata(null, OnSeriesChanged));

        /// <summary>
        /// Gets or sets the collection of series (lines) to plot. The graph subscribes to the collection and to
        /// each series, repainting when either changes.
        /// </summary>
        [Category("Common")]
        [Description("The collection of series (lines) to plot.")]
        public ObservableCollection<LineGraphSeries> Series
        {
            get => (ObservableCollection<LineGraphSeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(LineGraph),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the title shown centred above the plot. When null or blank the title row collapses and
        /// the plot reclaims the space.
        /// </summary>
        [Category("Common")]
        [Description("The title shown above the plot. Leave empty to give the space back to the plot.")]
        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TitleFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleFontSizeProperty =
            DependencyProperty.Register(nameof(TitleFontSize), typeof(double), typeof(LineGraph),
                new FrameworkPropertyMetadata(16d));

        /// <summary>
        /// Gets or sets the font size of the <see cref="Title"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("The font size of the title.")]
        public double TitleFontSize
        {
            get => (double)GetValue(TitleFontSizeProperty);
            set => SetValue(TitleFontSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TimeUnit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeUnitProperty =
            DependencyProperty.Register(nameof(TimeUnit), typeof(LineGraphTimeUnit), typeof(LineGraph),
                new FrameworkPropertyMetadata(LineGraphTimeUnit.Auto, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the unit the X-axis is divided into. <see cref="LineGraphTimeUnit.Auto"/> picks the
        /// unit that best fits the span of time covered by the plotted points.
        /// </summary>
        [Category("Appearance")]
        [Description("The unit of time the X-axis is divided into. Auto picks one to fit the data.")]
        public LineGraphTimeUnit TimeUnit
        {
            get => (LineGraphTimeUnit)GetValue(TimeUnitProperty);
            set => SetValue(TimeUnitProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TimeLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeLabelFormatProperty =
            DependencyProperty.Register(nameof(TimeLabelFormat), typeof(string), typeof(LineGraph),
                new FrameworkPropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets a <see cref="DateTime"/> format string for the X-axis labels. When null a format that
        /// suits the effective <see cref="TimeUnit"/> is used (for example "HH:mm" for minutes and "yyyy" for years).
        /// </summary>
        [Category("Appearance")]
        [Description("A DateTime format string for the X-axis labels. Null picks one that suits the time unit.")]
        public string? TimeLabelFormat
        {
            get => (string?)GetValue(TimeLabelFormatProperty);
            set => SetValue(TimeLabelFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ValueLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueLabelFormatProperty =
            DependencyProperty.Register(nameof(ValueLabelFormat), typeof(string), typeof(LineGraph),
                new FrameworkPropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets a numeric format string for the Y-axis labels and point tooltips. When null the number
        /// of decimal places is derived from the axis step so labels such as 0.25 and 1,000 both read cleanly.
        /// </summary>
        [Category("Appearance")]
        [Description("A numeric format string for the Y-axis labels. Null derives the precision from the axis step.")]
        public string? ValueLabelFormat
        {
            get => (string?)GetValue(ValueLabelFormatProperty);
            set => SetValue(ValueLabelFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IntervalCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IntervalCountProperty =
            DependencyProperty.Register(nameof(IntervalCount), typeof(int), typeof(LineGraph),
                new FrameworkPropertyMetadata(5, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the approximate number of intervals on the Y-axis. The axis is snapped to round
        /// numbers, so the actual count can differ by one or two.
        /// </summary>
        [Category("Appearance")]
        [Description("The approximate number of intervals on the Y-axis.")]
        public int IntervalCount
        {
            get => (int)GetValue(IntervalCountProperty);
            set => SetValue(IntervalCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinimumValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumValueProperty =
            DependencyProperty.Register(nameof(MinimumValue), typeof(double?), typeof(LineGraph),
                new FrameworkPropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets a fixed lower bound for the Y-axis. When null the axis starts at zero, or at the
        /// smallest plotted value when any value is negative.
        /// </summary>
        [Category("Appearance")]
        [Description("A fixed lower bound for the Y-axis. Null derives it from the data.")]
        public double? MinimumValue
        {
            get => (double?)GetValue(MinimumValueProperty);
            set => SetValue(MinimumValueProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaximumValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumValueProperty =
            DependencyProperty.Register(nameof(MaximumValue), typeof(double?), typeof(LineGraph),
                new FrameworkPropertyMetadata(null, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets a fixed upper bound for the Y-axis. When null the axis ends at the largest plotted
        /// value, rounded up to a round number.
        /// </summary>
        [Category("Appearance")]
        [Description("A fixed upper bound for the Y-axis. Null derives it from the data.")]
        public double? MaximumValue
        {
            get => (double?)GetValue(MaximumValueProperty);
            set => SetValue(MaximumValueProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(nameof(LineThickness), typeof(double), typeof(LineGraph),
                new FrameworkPropertyMetadata(2d, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the thickness of each plotted line. A series can override this with
        /// <see cref="LineGraphSeries.LineThickness"/>.
        /// </summary>
        [Category("Appearance")]
        [Description("The thickness of each plotted line.")]
        public double LineThickness
        {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PointRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PointRadiusProperty =
            DependencyProperty.Register(nameof(PointRadius), typeof(double), typeof(LineGraph),
                new FrameworkPropertyMetadata(4d, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the radius of the marker drawn at each point when <see cref="ShowPoints"/> is true.
        /// </summary>
        [Category("Appearance")]
        [Description("The radius of the marker drawn at each point.")]
        public double PointRadius
        {
            get => (double)GetValue(PointRadiusProperty);
            set => SetValue(PointRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowPoints"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPointsProperty =
            DependencyProperty.Register(nameof(ShowPoints), typeof(bool), typeof(LineGraph),
                new FrameworkPropertyMetadata(true, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets whether a marker is drawn at each point. Markers are what receive
        /// <see cref="PointClicked"/>, so hiding them also disables point clicks.
        /// </summary>
        [Category("Appearance")]
        [Description("Whether a marker is drawn at each point.")]
        public bool ShowPoints
        {
            get => (bool)GetValue(ShowPointsProperty);
            set => SetValue(ShowPointsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowGridLines"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register(nameof(ShowGridLines), typeof(bool), typeof(LineGraph),
                new FrameworkPropertyMetadata(true, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets whether horizontal grid lines are drawn across the plot at each Y-axis interval.
        /// </summary>
        [Category("Appearance")]
        [Description("Whether horizontal grid lines are drawn at each Y-axis interval.")]
        public bool ShowGridLines
        {
            get => (bool)GetValue(ShowGridLinesProperty);
            set => SetValue(ShowGridLinesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowLegend"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowLegendProperty =
            DependencyProperty.Register(nameof(ShowLegend), typeof(bool), typeof(LineGraph),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the legend of series captions is shown beneath the plot.
        /// </summary>
        [Category("Appearance")]
        [Description("Whether the legend is shown beneath the plot.")]
        public bool ShowLegend
        {
            get => (bool)GetValue(ShowLegendProperty);
            set => SetValue(ShowLegendProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StrokeBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeBrushProperty =
            DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(LineGraph),
                new FrameworkPropertyMetadata(Brushes.LightGray, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the brush used to draw the axes and grid lines. Defaults to the Mosaic theme control
        /// border brush.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used to draw the axes and grid lines.")]
        public Brush StrokeBrush
        {
            get => (Brush)GetValue(StrokeBrushProperty);
            set => SetValue(StrokeBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(LineGraph),
                new FrameworkPropertyMetadata(1d, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the thickness of the axes and grid lines.
        /// </summary>
        [Category("Appearance")]
        [Description("The thickness of the axes and grid lines.")]
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AxisForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AxisForegroundProperty =
            DependencyProperty.Register(nameof(AxisForeground), typeof(Brush), typeof(LineGraph),
                new FrameworkPropertyMetadata(Brushes.Gray, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the brush used for the axis labels. Defaults to the Mosaic theme secondary text brush
        /// so the labels sit back from the plotted lines.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used for the axis labels.")]
        public Brush AxisForeground
        {
            get => (Brush)GetValue(AxisForegroundProperty);
            set => SetValue(AxisForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SeriesClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeriesClickCommandProperty =
            DependencyProperty.Register(nameof(SeriesClickCommand), typeof(ICommand), typeof(LineGraph),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a command invoked when a line (or its legend entry) is clicked. The clicked
        /// <see cref="LineGraphSeries"/> is passed as the command parameter, which is the MVVM equivalent of
        /// handling <see cref="SeriesClicked"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Command invoked when a line is clicked, with the clicked series as the parameter.")]
        public ICommand? SeriesClickCommand
        {
            get => (ICommand?)GetValue(SeriesClickCommandProperty);
            set => SetValue(SeriesClickCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PointClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PointClickCommandProperty =
            DependencyProperty.Register(nameof(PointClickCommand), typeof(ICommand), typeof(LineGraph),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a command invoked when a point is clicked. The clicked <see cref="LineGraphPoint"/> is
        /// passed as the command parameter, which is the MVVM equivalent of handling <see cref="PointClicked"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Command invoked when a point is clicked, with the clicked point as the parameter.")]
        public ICommand? PointClickCommand
        {
            get => (ICommand?)GetValue(PointClickCommandProperty);
            set => SetValue(PointClickCommandProperty, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="SeriesClicked"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SeriesClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(SeriesClicked), RoutingStrategy.Bubble,
                typeof(EventHandler<LineGraphSeriesEventArgs>), typeof(LineGraph));

        /// <summary>
        /// Occurs when a line, or its entry in the legend, is clicked. The
        /// <see cref="LineGraphSeriesEventArgs.Series"/> on the event args is the <see cref="LineGraphSeries"/>
        /// that backs the clicked line.
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when a line or its legend entry is clicked.")]
        public event EventHandler<LineGraphSeriesEventArgs> SeriesClicked
        {
            add => this.AddHandler(SeriesClickedEvent, value);
            remove => this.RemoveHandler(SeriesClickedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="PointClicked"/> routed event.
        /// </summary>
        public static readonly RoutedEvent PointClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(PointClicked), RoutingStrategy.Bubble,
                typeof(EventHandler<LineGraphPointEventArgs>), typeof(LineGraph));

        /// <summary>
        /// Occurs when a point marker is clicked. The event args carry both the
        /// <see cref="LineGraphPointEventArgs.Point"/> and the <see cref="LineGraphPointEventArgs.Series"/> it
        /// belongs to. A point click does not also raise <see cref="SeriesClicked"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when a point is clicked.")]
        public event EventHandler<LineGraphPointEventArgs> PointClicked
        {
            add => this.AddHandler(PointClickedEvent, value);
            remove => this.RemoveHandler(PointClickedEvent, value);
        }

        #endregion

        static LineGraph()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LineGraph), new FrameworkPropertyMetadata(typeof(LineGraph)));

            // The axis labels are drawn with the control's own text settings, and the point markers are
            // outlined in the background colour, so a change to any of them has to redraw the canvas.
            ForegroundProperty.OverrideMetadata(typeof(LineGraph), new FrameworkPropertyMetadata(OnVisualPropertyChanged));
            BackgroundProperty.OverrideMetadata(typeof(LineGraph), new FrameworkPropertyMetadata(OnVisualPropertyChanged));
            FontSizeProperty.OverrideMetadata(typeof(LineGraph), new FrameworkPropertyMetadata(OnVisualPropertyChanged));
            FontFamilyProperty.OverrideMetadata(typeof(LineGraph), new FrameworkPropertyMetadata(OnVisualPropertyChanged));
            FontWeightProperty.OverrideMetadata(typeof(LineGraph), new FrameworkPropertyMetadata(OnVisualPropertyChanged));
            FontStyleProperty.OverrideMetadata(typeof(LineGraph), new FrameworkPropertyMetadata(OnVisualPropertyChanged));

            DefaultPalette = new Brush[]
            {
                new SolidColorBrush(Color.FromRgb(0x4E, 0x79, 0xA7)),
                new SolidColorBrush(Color.FromRgb(0xF2, 0x8E, 0x2B)),
                new SolidColorBrush(Color.FromRgb(0xE1, 0x57, 0x59)),
                new SolidColorBrush(Color.FromRgb(0x76, 0xB7, 0xB2)),
                new SolidColorBrush(Color.FromRgb(0x59, 0xA1, 0x4F)),
                new SolidColorBrush(Color.FromRgb(0xED, 0xC9, 0x48)),
                new SolidColorBrush(Color.FromRgb(0xB0, 0x7A, 0xA1)),
                new SolidColorBrush(Color.FromRgb(0xFF, 0x9D, 0xA7)),
                new SolidColorBrush(Color.FromRgb(0x9C, 0x75, 0x5F)),
                new SolidColorBrush(Color.FromRgb(0xBA, 0xB0, 0xAC))
            };

            foreach (var brush in DefaultPalette)
            {
                brush.Freeze();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineGraph"/> class.
        /// </summary>
        public LineGraph()
        {
            // Give the graph a usable collection out of the box so a consumer can add series without having to
            // construct one first. Assigned per instance so it is never shared between graphs.
            this.Series = new ObservableCollection<LineGraphSeries>();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_canvas is not null)
            {
                _canvas.SizeChanged -= this.OnCanvasSizeChanged;
                _canvas.Children.Clear();
            }

            if (_legend is not null)
            {
                _legend.RemoveHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(this.OnLegendMouseLeftButtonUp));
            }

            base.OnApplyTemplate();

            _canvas = this.GetTemplateChild(PART_Canvas) as Canvas;
            _legend = this.GetTemplateChild(PART_Legend) as ItemsControl;

            if (_canvas is not null)
            {
                _canvas.SizeChanged += this.OnCanvasSizeChanged;
            }

            if (_legend is not null)
            {
                _legend.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(this.OnLegendMouseLeftButtonUp));
            }

            this.InvalidateGraph();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new LineGraphAutomationPeer(this);
        }

        /// <summary>
        /// Raises <see cref="SeriesClicked"/> and invokes <see cref="SeriesClickCommand"/> for the given series.
        /// </summary>
        /// <param name="series">The series backing the line that was clicked.</param>
        protected virtual void OnSeriesClicked(LineGraphSeries series)
        {
            this.RaiseEvent(new LineGraphSeriesEventArgs(SeriesClickedEvent, this, series));

            var command = this.SeriesClickCommand;

            if (command is not null && command.CanExecute(series))
            {
                command.Execute(series);
            }
        }

        /// <summary>
        /// Raises <see cref="PointClicked"/> and invokes <see cref="PointClickCommand"/> for the given point.
        /// </summary>
        /// <param name="series">The series the clicked point belongs to.</param>
        /// <param name="point">The point that was clicked.</param>
        protected virtual void OnPointClicked(LineGraphSeries series, LineGraphPoint point)
        {
            this.RaiseEvent(new LineGraphPointEventArgs(PointClickedEvent, this, series, point));

            var command = this.PointClickCommand;

            if (command is not null && command.CanExecute(point))
            {
                command.Execute(point);
            }
        }

        /// <summary>
        /// Maps a click on a painted line back to the series it was drawn from. The series is stashed on the
        /// shape's <see cref="FrameworkElement.Tag"/> when the canvas is built.
        /// </summary>
        private void OnLineMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { Tag: LineGraphSeries series })
            {
                this.OnSeriesClicked(series);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Maps a click on a painted marker back to the point it was drawn from. The point and its series are
        /// stashed on the shape's <see cref="FrameworkElement.Tag"/> when the canvas is built.
        /// </summary>
        private void OnPointMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { Tag: PointTag tag })
            {
                this.OnPointClicked(tag.Series, tag.Point);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Treats a click on a legend entry the same as a click on its line. The legend items inherit the
        /// series as their data context, so the original source resolves straight back to it.
        /// </summary>
        private void OnLegendMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement { DataContext: LineGraphSeries series })
            {
                this.OnSeriesClicked(series);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Swaps the collection the graph is observing, moving the subscriptions from the old collection and
        /// its series onto the new one.
        /// </summary>
        private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not LineGraph graph)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<LineGraphSeries> oldSeries)
            {
                oldSeries.CollectionChanged -= graph.OnSeriesCollectionChanged;
            }

            graph.UnhookSeries();

            if (e.NewValue is ObservableCollection<LineGraphSeries> newSeries)
            {
                newSeries.CollectionChanged += graph.OnSeriesCollectionChanged;
                graph.HookSeries(newSeries);
            }

            graph.InvalidateGraph();
        }

        /// <summary>
        /// Repaints the graph when a property that affects its rendering changes.
        /// </summary>
        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LineGraph)?.InvalidateGraph();
        }

        /// <summary>
        /// Updates series subscriptions and invalidates the graph when the series collection changes.
        /// </summary>
        private void OnSeriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // A reset carries no old items, so rebuild the subscriptions from the current collection.
                this.UnhookSeries();
                this.HookSeries(this.Series);
            }
            else
            {
                if (e.OldItems is not null)
                {
                    foreach (var series in e.OldItems.OfType<LineGraphSeries>())
                    {
                        series.PropertyChanged -= this.OnSeriesPropertyChanged;
                        _hookedSeries.Remove(series);
                    }
                }

                if (e.NewItems is not null)
                {
                    this.HookSeries(e.NewItems.OfType<LineGraphSeries>());
                }
            }

            this.InvalidateGraph();
        }

        /// <summary>
        /// Invalidates the graph when a series, or a point inside it, changes.
        /// </summary>
        private void OnSeriesPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // EffectiveBrush is raised by the graph itself when it assigns palette colours during a paint, so
            // reacting to it would just schedule a second, identical paint.
            if (e.PropertyName == nameof(LineGraphSeries.EffectiveBrush))
            {
                return;
            }

            this.InvalidateGraph();
        }

        /// <summary>
        /// Subscribes to property changes for the specified series.
        /// </summary>
        private void HookSeries(IEnumerable<LineGraphSeries>? series)
        {
            if (series is null)
            {
                return;
            }

            foreach (var item in series)
            {
                item.PropertyChanged += this.OnSeriesPropertyChanged;
                _hookedSeries.Add(item);
            }
        }

        /// <summary>
        /// Unsubscribes from property changes for all currently observed series.
        /// </summary>
        private void UnhookSeries()
        {
            foreach (var item in _hookedSeries)
            {
                item.PropertyChanged -= this.OnSeriesPropertyChanged;
            }

            _hookedSeries.Clear();
        }

        /// <summary>
        /// Repaints when the plot area is resized.
        /// </summary>
        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Redraw();
        }

        /// <summary>
        /// Queues a repaint onto the dispatcher. A bulk update to the collection or to several series produces
        /// a burst of notifications, and coalescing them means the canvas is only rebuilt once.
        /// </summary>
        private void InvalidateGraph()
        {
            if (_repaintQueued)
            {
                return;
            }

            _repaintQueued = true;

            this.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                _repaintQueued = false;
                this.Redraw();
            });
        }

        /// <summary>
        /// Clears the plot and repaints the graph at its current size. Changes to the series, their points and
        /// the graph's properties repaint automatically, so this rarely needs to be called directly.
        /// </summary>
        public void Redraw()
        {
            if (_canvas is null)
            {
                return;
            }

            _canvas.Children.Clear();
            this.Paint(_canvas, _canvas.ActualWidth, _canvas.ActualHeight);
        }

        #region Painting

        /// <summary>
        /// Paints the axes, grid, lines and markers onto the canvas.
        /// </summary>
        private void Paint(Canvas canvas, double width, double height)
        {
            if (width <= 0 || height <= 0 || double.IsNaN(width) || double.IsNaN(height) || this.Series is null)
            {
                return;
            }

            // Hand out palette colours by position so the legend swatches match before anything is drawn.
            for (var i = 0; i < this.Series.Count; i++)
            {
                this.Series[i].SetPaletteBrush(DefaultPalette[i % DefaultPalette.Length]);
            }

            var allPoints = this.Series
                .Where(s => s.Points is not null)
                .SelectMany(s => s.Points)
                .ToList();

            if (allPoints.Count == 0)
            {
                return;
            }

            // ---- Ranges -------------------------------------------------------------------------------------

            var minTime = allPoints.Min(p => p.Time);
            var maxTime = allPoints.Max(p => p.Time);
            var unit = this.ResolveTimeUnit(minTime, maxTime);

            if (minTime == maxTime)
            {
                // A single instant has no width, so pad half a unit either side to give it somewhere to sit.
                minTime = AddUnits(minTime, unit, -1);
                maxTime = AddUnits(maxTime, unit, 1);
            }

            var dataMin = allPoints.Min(p => p.Value);
            var dataMax = allPoints.Max(p => p.Value);

            // Start the axis at zero when everything is positive so bars of magnitude read honestly.
            var rangeMin = this.MinimumValue ?? Math.Min(0, dataMin);
            var rangeMax = this.MaximumValue ?? dataMax;

            var (yMin, yMax, yStep) = NiceScale(rangeMin, rangeMax, Math.Max(1, this.IntervalCount));

            if (this.MinimumValue.HasValue)
            {
                yMin = this.MinimumValue.Value;
            }

            if (this.MaximumValue.HasValue)
            {
                yMax = this.MaximumValue.Value;
            }

            if (yMax <= yMin)
            {
                yMax = yMin + yStep;
            }

            var valueFormat = this.ValueLabelFormat ?? DeriveValueFormat(yStep);

            // ---- Y labels (measured first so the plot can be sized around them) ------------------------------

            var yLabels = new List<(double Value, TextBlock Label, Size Size)>();

            for (var v = yMin; v <= yMax + yStep / 2; v += yStep)
            {
                // Snap away the floating point drift that accumulates when stepping by e.g. 0.1.
                var snapped = Math.Round(v / yStep) * yStep;
                var label = this.CreateAxisLabel(snapped.ToString(valueFormat, CultureInfo.CurrentCulture));
                yLabels.Add((snapped, label, Measure(label)));
            }

            var yLabelWidth = yLabels.Count > 0 ? yLabels.Max(l => l.Size.Width) : 0;
            var yLabelHeight = yLabels.Count > 0 ? yLabels.Max(l => l.Size.Height) : 0;

            // ---- X labels -----------------------------------------------------------------------------------

            var timeFormat = this.TimeLabelFormat ?? DefaultTimeFormat(unit, maxTime - minTime);
            var sampleLabel = this.CreateAxisLabel(maxTime.ToString(timeFormat, CultureInfo.CurrentCulture));
            var sampleSize = Measure(sampleLabel);

            // ---- Plot rectangle -----------------------------------------------------------------------------

            var left = yLabelWidth + Y_AXIS_LABEL_MARGIN;
            var right = width - Math.Max(sampleSize.Width / 2, this.PointRadius + 2);
            var top = Math.Max(yLabelHeight / 2, this.PointRadius + 2);
            var bottom = height - (sampleSize.Height + X_AXIS_LABEL_MARGIN + TICK_LENGTH);

            var plotWidth = right - left;
            var plotHeight = bottom - top;

            if (plotWidth <= 0 || plotHeight <= 0)
            {
                return;
            }

            var totalTicks = (maxTime - minTime).Ticks;
            var valueSpan = yMax - yMin;

            double XFor(DateTime t) => left + (t - minTime).Ticks / (double)totalTicks * plotWidth;
            double YFor(double v) => bottom - (v - yMin) / valueSpan * plotHeight;

            // ---- Grid lines and Y labels --------------------------------------------------------------------

            foreach (var (value, label, size) in yLabels)
            {
                var y = YFor(value);

                if (this.ShowGridLines || Math.Abs(value - yMin) < yStep / 2)
                {
                    canvas.Children.Add(new Line
                    {
                        Stroke = this.StrokeBrush,
                        StrokeThickness = this.StrokeThickness,
                        X1 = left,
                        Y1 = y,
                        X2 = right,
                        Y2 = y,
                        SnapsToDevicePixels = true
                    });
                }

                canvas.Children.Add(label);
                Canvas.SetLeft(label, left - Y_AXIS_LABEL_MARGIN - size.Width);
                Canvas.SetTop(label, y - size.Height / 2);
            }

            // Y axis line.
            canvas.Children.Add(new Line
            {
                Stroke = this.StrokeBrush,
                StrokeThickness = this.StrokeThickness,
                X1 = left,
                Y1 = top,
                X2 = left,
                Y2 = bottom,
                SnapsToDevicePixels = true
            });

            // ---- X ticks and labels -------------------------------------------------------------------------

            var maxTickCount = Math.Max(2, (int)Math.Floor(plotWidth / (sampleSize.Width + X_AXIS_LABEL_GAP)));
            var lastLabelRight = double.NegativeInfinity;

            foreach (var tick in BuildTimeTicks(minTime, maxTime, unit, maxTickCount))
            {
                var x = XFor(tick);

                canvas.Children.Add(new Line
                {
                    Stroke = this.StrokeBrush,
                    StrokeThickness = this.StrokeThickness,
                    X1 = x,
                    Y1 = bottom,
                    X2 = x,
                    Y2 = bottom + TICK_LENGTH,
                    SnapsToDevicePixels = true
                });

                var label = this.CreateAxisLabel(tick.ToString(timeFormat, CultureInfo.CurrentCulture));
                var size = Measure(label);
                var labelLeft = Math.Max(0, Math.Min(width - size.Width, x - size.Width / 2));

                // Drop a label that would run into the previous one rather than letting them overprint.
                if (labelLeft < lastLabelRight + X_AXIS_LABEL_GAP)
                {
                    continue;
                }

                canvas.Children.Add(label);
                Canvas.SetLeft(label, labelLeft);
                Canvas.SetTop(label, bottom + TICK_LENGTH + X_AXIS_LABEL_MARGIN);
                lastLabelRight = labelLeft + size.Width;
            }

            // ---- Lines --------------------------------------------------------------------------------------

            var markerStroke = this.Background ?? Brushes.Transparent;
            var pointRadius = Math.Max(0, this.PointRadius);
            var markers = new List<UIElement>();

            foreach (var series in this.Series)
            {
                if (series.Points is null || series.Points.Count == 0)
                {
                    continue;
                }

                var brush = series.EffectiveBrush ?? this.Foreground;
                var thickness = series.LineThickness ?? this.LineThickness;
                var ordered = series.Points.OrderBy(p => p.Time).ToList();
                var screenPoints = new PointCollection(ordered.Select(p => new Point(XFor(p.Time), YFor(p.Value))));

                if (ordered.Count > 1)
                {
                    // A fat transparent copy of the line underneath the visible one gives a comfortable click
                    // target without thickening what the user sees.
                    var hitLine = new Polyline
                    {
                        Points = screenPoints,
                        Stroke = Brushes.Transparent,
                        StrokeThickness = Math.Max(thickness + 8, 10),
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Tag = series,
                        Cursor = Cursors.Hand,
                        ToolTip = series.Caption
                    };
                    hitLine.MouseLeftButtonUp += this.OnLineMouseLeftButtonUp;
                    canvas.Children.Add(hitLine);

                    var line = new Polyline
                    {
                        Points = screenPoints,
                        Stroke = brush,
                        StrokeThickness = thickness,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Tag = series,
                        Cursor = Cursors.Hand,
                        ToolTip = series.Caption
                    };
                    line.MouseLeftButtonUp += this.OnLineMouseLeftButtonUp;
                    canvas.Children.Add(line);
                }

                if (!this.ShowPoints)
                {
                    continue;
                }

                for (var i = 0; i < ordered.Count; i++)
                {
                    var point = ordered[i];
                    var center = screenPoints[i];
                    var tag = new PointTag(series, point);
                    var tooltip = point.Label
                                  ?? $"{series.Caption}\n{point.Time.ToString(timeFormat, CultureInfo.CurrentCulture)}: {point.Value.ToString(valueFormat, CultureInfo.CurrentCulture)}";

                    // Same idea as the line: an oversized transparent disc catches the click for a small marker.
                    var hitRadius = Math.Max(pointRadius + 4, 8);
                    var hit = new Ellipse
                    {
                        Width = hitRadius * 2,
                        Height = hitRadius * 2,
                        Fill = Brushes.Transparent,
                        Tag = tag,
                        Cursor = Cursors.Hand,
                        ToolTip = tooltip
                    };
                    hit.MouseLeftButtonUp += this.OnPointMouseLeftButtonUp;
                    Canvas.SetLeft(hit, center.X - hitRadius);
                    Canvas.SetTop(hit, center.Y - hitRadius);
                    markers.Add(hit);

                    if (pointRadius > 0)
                    {
                        var marker = new Ellipse
                        {
                            Width = pointRadius * 2,
                            Height = pointRadius * 2,
                            Fill = brush,
                            Stroke = markerStroke,
                            StrokeThickness = Math.Min(2, pointRadius / 2),
                            Tag = tag,
                            Cursor = Cursors.Hand,
                            ToolTip = tooltip
                        };
                        marker.MouseLeftButtonUp += this.OnPointMouseLeftButtonUp;
                        Canvas.SetLeft(marker, center.X - pointRadius);
                        Canvas.SetTop(marker, center.Y - pointRadius);
                        markers.Add(marker);
                    }
                }
            }

            // Markers go on last so a point always wins the click over any line that crosses it.
            foreach (var marker in markers)
            {
                canvas.Children.Add(marker);
            }
        }

        /// <summary>
        /// Creates a text block for an axis label using the graph's typography and axis brush.
        /// </summary>
        private TextBlock CreateAxisLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = this.AxisForeground,
                FontFamily = this.FontFamily,
                FontSize = this.FontSize,
                FontWeight = this.FontWeight,
                FontStyle = this.FontStyle,
                IsHitTestVisible = false
            };
        }

        /// <summary>
        /// Measures a text block that has not yet been added to the tree.
        /// </summary>
        private static Size Measure(TextBlock textBlock)
        {
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.DesiredSize;
        }

        /// <summary>
        /// Picks the time unit to divide the X-axis into, honouring <see cref="TimeUnit"/> when it is not
        /// <see cref="LineGraphTimeUnit.Auto"/>.
        /// </summary>
        private LineGraphTimeUnit ResolveTimeUnit(DateTime min, DateTime max)
        {
            if (this.TimeUnit != LineGraphTimeUnit.Auto)
            {
                return this.TimeUnit;
            }

            var span = max - min;

            if (span <= TimeSpan.FromHours(3))
            {
                return LineGraphTimeUnit.Minutes;
            }

            if (span <= TimeSpan.FromDays(3))
            {
                return LineGraphTimeUnit.Hours;
            }

            if (span <= TimeSpan.FromDays(60))
            {
                return LineGraphTimeUnit.Days;
            }

            if (span <= TimeSpan.FromDays(200))
            {
                return LineGraphTimeUnit.Weeks;
            }

            if (span <= TimeSpan.FromDays(365 * 3))
            {
                return LineGraphTimeUnit.Months;
            }

            return LineGraphTimeUnit.Years;
        }

        /// <summary>
        /// The label format used for a unit when <see cref="TimeLabelFormat"/> is not set.
        /// </summary>
        private static string DefaultTimeFormat(LineGraphTimeUnit unit, TimeSpan span)
        {
            return unit switch
            {
                LineGraphTimeUnit.Minutes => span >= TimeSpan.FromDays(1) ? "MMM d HH:mm" : "HH:mm",
                LineGraphTimeUnit.Hours => span >= TimeSpan.FromDays(1) ? "MMM d HH:mm" : "HH:mm",
                LineGraphTimeUnit.Days => "MMM d",
                LineGraphTimeUnit.Weeks => "MMM d",
                LineGraphTimeUnit.Months => "MMM yyyy",
                LineGraphTimeUnit.Years => "yyyy",
                _ => "g"
            };
        }

        /// <summary>
        /// Adds a whole number of units to a time.
        /// </summary>
        private static DateTime AddUnits(DateTime time, LineGraphTimeUnit unit, int count)
        {
            return unit switch
            {
                LineGraphTimeUnit.Minutes => time.AddMinutes(count),
                LineGraphTimeUnit.Hours => time.AddHours(count),
                LineGraphTimeUnit.Days => time.AddDays(count),
                LineGraphTimeUnit.Weeks => time.AddDays(7 * count),
                LineGraphTimeUnit.Months => time.AddMonths(count),
                LineGraphTimeUnit.Years => time.AddYears(count),
                _ => time.AddDays(count)
            };
        }

        /// <summary>
        /// Rounds a time down to the start of the given unit.
        /// </summary>
        private static DateTime FloorToUnit(DateTime time, LineGraphTimeUnit unit)
        {
            switch (unit)
            {
                case LineGraphTimeUnit.Minutes:
                    return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Kind);
                case LineGraphTimeUnit.Hours:
                    return new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0, time.Kind);
                case LineGraphTimeUnit.Days:
                    return time.Date;
                case LineGraphTimeUnit.Weeks:
                {
                    var firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                    var offset = ((int)time.DayOfWeek - (int)firstDay + 7) % 7;
                    return time.Date.AddDays(-offset);
                }
                case LineGraphTimeUnit.Months:
                    return new DateTime(time.Year, time.Month, 1, 0, 0, 0, time.Kind);
                case LineGraphTimeUnit.Years:
                    return new DateTime(time.Year, 1, 1, 0, 0, 0, time.Kind);
                default:
                    return time.Date;
            }
        }

        /// <summary>
        /// The number of whole units between two times, used to decide how many units each tick should step.
        /// </summary>
        private static double UnitsBetween(DateTime min, DateTime max, LineGraphTimeUnit unit)
        {
            var span = max - min;

            return unit switch
            {
                LineGraphTimeUnit.Minutes => span.TotalMinutes,
                LineGraphTimeUnit.Hours => span.TotalHours,
                LineGraphTimeUnit.Days => span.TotalDays,
                LineGraphTimeUnit.Weeks => span.TotalDays / 7,
                LineGraphTimeUnit.Months => span.TotalDays / 30.4375,
                LineGraphTimeUnit.Years => span.TotalDays / 365.25,
                _ => span.TotalDays
            };
        }

        /// <summary>
        /// The tick step sizes, in units, that read naturally for each unit. The smallest one that keeps the
        /// tick count under the limit is used.
        /// </summary>
        private static int[] NiceStepsFor(LineGraphTimeUnit unit)
        {
            return unit switch
            {
                LineGraphTimeUnit.Minutes => new[] { 1, 2, 5, 10, 15, 30, 60, 120, 180, 360, 720, 1440 },
                LineGraphTimeUnit.Hours => new[] { 1, 2, 3, 4, 6, 12, 24, 48, 72, 168 },
                LineGraphTimeUnit.Days => new[] { 1, 2, 3, 5, 7, 14, 21, 28, 56, 91, 182, 365 },
                LineGraphTimeUnit.Weeks => new[] { 1, 2, 4, 8, 13, 26, 52 },
                LineGraphTimeUnit.Months => new[] { 1, 2, 3, 4, 6, 12, 24, 60, 120 },
                LineGraphTimeUnit.Years => new[] { 1, 2, 5, 10, 20, 25, 50, 100 },
                _ => new[] { 1, 2, 5, 10 }
            };
        }

        /// <summary>
        /// Produces the times at which X-axis ticks are drawn: aligned to unit boundaries and stepping by a
        /// natural multiple of the unit so no more than <paramref name="maxTicks"/> fall inside the range.
        /// </summary>
        private static IEnumerable<DateTime> BuildTimeTicks(DateTime min, DateTime max, LineGraphTimeUnit unit, int maxTicks)
        {
            var units = Math.Max(1, UnitsBetween(min, max, unit));
            var steps = NiceStepsFor(unit);
            var step = steps[^1];

            foreach (var candidate in steps)
            {
                if (units / candidate <= maxTicks)
                {
                    step = candidate;
                    break;
                }
            }

            // If even the largest natural step is too dense, keep doubling it.
            while (units / step > maxTicks)
            {
                step *= 2;
            }

            var tick = FloorToUnit(min, unit);

            // Align to the step so, for example, 15 minute ticks land on :00, :15, :30 and :45.
            if (unit is LineGraphTimeUnit.Minutes or LineGraphTimeUnit.Hours or LineGraphTimeUnit.Months)
            {
                var component = unit switch
                {
                    LineGraphTimeUnit.Minutes => tick.Minute,
                    LineGraphTimeUnit.Hours => tick.Hour,
                    _ => tick.Month - 1
                };

                tick = AddUnits(tick, unit, -(component % step));
            }

            if (tick < min)
            {
                tick = AddUnits(tick, unit, step);
            }

            var guard = 0;

            while (tick <= max && guard++ < 10_000)
            {
                yield return tick;
                tick = AddUnits(tick, unit, step);
            }
        }

        /// <summary>
        /// Computes a Y-axis range whose bounds and step are round numbers (1, 2 or 5 times a power of ten),
        /// after Heckbert's "nice numbers" algorithm.
        /// </summary>
        private static (double Min, double Max, double Step) NiceScale(double min, double max, int intervals)
        {
            if (double.IsNaN(min) || double.IsInfinity(min))
            {
                min = 0;
            }

            if (double.IsNaN(max) || double.IsInfinity(max))
            {
                max = min + 1;
            }

            if (max <= min)
            {
                // Every value is identical (or the fixed bounds are inverted). Give the flat line some room.
                var pad = Math.Abs(min) > 0 ? Math.Abs(min) * 0.5 : 1;
                max = min + pad;
            }

            var range = NiceNumber(max - min, false);
            var step = NiceNumber(range / intervals, true);
            var niceMin = Math.Floor(min / step) * step;
            var niceMax = Math.Ceiling(max / step) * step;

            return (niceMin, niceMax, step);
        }

        /// <summary>
        /// Rounds a number to the nearest (or next) 1, 2 or 5 times a power of ten.
        /// </summary>
        private static double NiceNumber(double value, bool round)
        {
            if (value <= 0)
            {
                return 1;
            }

            var exponent = Math.Floor(Math.Log10(value));
            var fraction = value / Math.Pow(10, exponent);
            double niceFraction;

            if (round)
            {
                niceFraction = fraction < 1.5 ? 1 : fraction < 3 ? 2 : fraction < 7 ? 5 : 10;
            }
            else
            {
                niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
            }

            return niceFraction * Math.Pow(10, exponent);
        }

        /// <summary>
        /// Derives a numeric format with just enough decimal places to distinguish labels one step apart.
        /// </summary>
        private static string DeriveValueFormat(double step)
        {
            if (step >= 1)
            {
                return "N0";
            }

            var decimals = (int)Math.Ceiling(-Math.Log10(step));
            return "N" + Math.Clamp(decimals, 1, 6);
        }

        /// <summary>
        /// Stashed on each marker so a click can be mapped back to both the point and the series it belongs to.
        /// </summary>
        private sealed record PointTag(LineGraphSeries Series, LineGraphPoint Point);

        #endregion
    }
}
