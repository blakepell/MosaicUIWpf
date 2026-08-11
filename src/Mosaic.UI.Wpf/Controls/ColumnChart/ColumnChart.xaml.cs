/*
 * Originally based off (MIT): https://github.com/JMHeartley/WPF-Chart-Controls
 */

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Shapes;
using Mosaic.UI.Wpf.Themes;
// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A responsive column (bar) chart that renders an <see cref="ObservableCollection{T}"/> of
    /// <see cref="ColumnChartItem"/> onto a canvas. The chart repaints whenever it is resized, whenever the
    /// collection changes, and whenever a property on any item in the collection changes.
    /// </summary>
    /// <remarks>
    /// Colours default to Mosaic theme tokens (<c>ControlBackgroundBrush</c>, <c>ControlForegroundBrush</c>,
    /// <c>AccentBrush</c> and <c>ControlBorderBrush</c>) via <c>DynamicResource</c>, so the chart follows a
    /// theme change without any code. An individual column can opt out by setting
    /// <see cref="ColumnChartItem.ColumnBrush"/>.
    /// </remarks>
    [DefaultProperty(nameof(Items))]
    public partial class ColumnChart
    {
        private const int X_AXIS_TEXT_BLOCK_TOP_MARGIN = 5;

        private const int Y_AXIS_TEXT_BLOCK_RIGHT_MARGIN = 10;

        /// <summary>
        /// The items currently hooked for <see cref="INotifyPropertyChanged"/>. Tracked separately from
        /// <see cref="Items"/> so a <see cref="NotifyCollectionChangedAction.Reset"/> (which carries no old
        /// items) can still be unhooked cleanly.
        /// </summary>
        private readonly List<ColumnChartItem> _hookedItems = new();

        /// <summary>
        /// Whether a repaint has already been queued onto the dispatcher. Coalesces the many notifications that
        /// a bulk update produces into a single paint.
        /// </summary>
        private bool _repaintQueued;

        /// <summary>
        /// Identifies the <see cref="Items"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(ObservableCollection<ColumnChartItem>), typeof(ColumnChart),
                new FrameworkPropertyMetadata(null, OnItemsChanged));

        /// <summary>
        /// Gets or sets the collection of column items to be displayed in the chart. The chart subscribes to the
        /// collection and to each item, repainting when either changes.
        /// </summary>
        [Category("Common")]
        [Description("The collection of columns to display.")]
        public ObservableCollection<ColumnChartItem> Items
        {
            get => (ObservableCollection<ColumnChartItem>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ColumnBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnBrushProperty =
            DependencyProperty.Register(nameof(ColumnBrush), typeof(Brush), typeof(ColumnChart),
                new FrameworkPropertyMetadata(Brushes.Gold, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the brush used to fill columns that do not specify their own
        /// <see cref="ColumnChartItem.ColumnBrush"/>. Defaults to the Mosaic theme accent brush.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used to fill columns that do not specify their own brush.")]
        public Brush ColumnBrush
        {
            get => (Brush)GetValue(ColumnBrushProperty);
            set => SetValue(ColumnBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StrokeBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeBrushProperty =
            DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(ColumnChart),
                new FrameworkPropertyMetadata(Brushes.LightGray, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the brush used to draw the axis and lines. Defaults to the Mosaic theme control border brush.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used to draw the axis and grid lines.")]
        public Brush StrokeBrush
        {
            get => (Brush)GetValue(StrokeBrushProperty);
            set => SetValue(StrokeBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(ColumnChart),
                new FrameworkPropertyMetadata(1d, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the thickness of the axis and lines.
        /// </summary>
        [Category("Appearance")]
        [Description("The thickness of the axis and grid lines.")]
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IntervalCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IntervalCountProperty =
            DependencyProperty.Register(nameof(IntervalCount), typeof(int), typeof(ColumnChart),
                new FrameworkPropertyMetadata(8, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the number of intervals to be displayed on the Y-axis.
        /// </summary>
        [Category("Appearance")]
        [Description("The number of intervals displayed on the Y-axis.")]
        public int IntervalCount
        {
            get => (int)GetValue(IntervalCountProperty);
            set => SetValue(IntervalCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="InnerPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InnerPaddingProperty =
            DependencyProperty.Register(nameof(InnerPadding), typeof(Thickness), typeof(ColumnChart),
                new FrameworkPropertyMetadata(new Thickness(100), OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the inner padding of the chart area, which is the space reserved around the plot for the
        /// axis labels and column name.
        /// </summary>
        [Category("Layout")]
        [Description("The space reserved around the plot for axis labels and column names.")]
        public Thickness InnerPadding
        {
            get => (Thickness)GetValue(InnerPaddingProperty);
            set => SetValue(InnerPaddingProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ColumnClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnClickCommandProperty =
            DependencyProperty.Register(nameof(ColumnClickCommand), typeof(ICommand), typeof(ColumnChart),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a command invoked when a column is clicked. The clicked <see cref="ColumnChartItem"/> is
        /// passed as the command parameter, which is the MVVM equivalent of handling <see cref="ColumnClicked"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Command invoked when a column is clicked, with the clicked item as the parameter.")]
        public ICommand? ColumnClickCommand
        {
            get => (ICommand?)GetValue(ColumnClickCommandProperty);
            set => SetValue(ColumnClickCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ColumnClicked"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ColumnClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(ColumnClicked), RoutingStrategy.Bubble,
                typeof(EventHandler<ColumnChartItemEventArgs>), typeof(ColumnChart));

        /// <summary>
        /// Occurs when a column is clicked. The <see cref="ColumnChartItemEventArgs.Item"/> on the event args is
        /// the <see cref="ColumnChartItem"/> that backs the clicked column.
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when a column is clicked.")]
        public event EventHandler<ColumnChartItemEventArgs> ColumnClicked
        {
            add => this.AddHandler(ColumnClickedEvent, value);
            remove => this.RemoveHandler(ColumnClickedEvent, value);
        }

        static ColumnChart()
        {
            // The axis labels and column names are drawn with the control's own text settings, so a change to
            // any of them has to redraw the canvas.
            ForegroundProperty.OverrideMetadata(typeof(ColumnChart),
                new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.Inherits, OnVisualPropertyChanged));
            FontSizeProperty.OverrideMetadata(typeof(ColumnChart),
                new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.Inherits, OnVisualPropertyChanged));
            FontFamilyProperty.OverrideMetadata(typeof(ColumnChart),
                new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.Inherits, OnVisualPropertyChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnChart"/> class.
        /// </summary>
        public ColumnChart()
        {
            InitializeComponent();

            // Dynamic references so the chart follows a theme switch. These are local values, so anything the
            // consumer sets afterward in XAML or code still wins.
            this.SetResourceReference(BackgroundProperty, MosaicTheme.ControlBackgroundBrush);
            this.SetResourceReference(ForegroundProperty, MosaicTheme.ControlForegroundBrush);
            this.SetResourceReference(ColumnBrushProperty, MosaicTheme.AccentBrush);
            this.SetResourceReference(StrokeBrushProperty, MosaicTheme.ControlBorderBrush);

            // Give the chart a usable collection out of the box so a consumer can add items without having to
            // construct one first. Assigned after InitializeComponent so it is never shared between instances.
            this.Items = new ObservableCollection<ColumnChartItem>();
        }

        /// <summary>
        /// Raises <see cref="ColumnClicked"/> and invokes <see cref="ColumnClickCommand"/> for the given item.
        /// </summary>
        /// <param name="item">The item backing the column that was clicked.</param>
        protected virtual void OnColumnClicked(ColumnChartItem item)
        {
            this.RaiseEvent(new ColumnChartItemEventArgs(ColumnClickedEvent, this, item));

            var command = this.ColumnClickCommand;

            if (command is not null && command.CanExecute(item))
            {
                command.Execute(item);
            }
        }

        /// <summary>
        /// Maps a click on a painted column back to the item it was drawn from. The item is stashed on the
        /// shape's <see cref="FrameworkElement.Tag"/> when the canvas is built.
        /// </summary>
        /// <param name="sender">The painted element that received the click.</param>
        /// <param name="e">The mouse button event data.</param>
        private void OnColumnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { Tag: ColumnChartItem item })
            {
                this.OnColumnClicked(item);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Swaps the collection the chart is observing, moving the subscriptions from the old collection and its
        /// items onto the new one.
        /// </summary>
        /// <param name="d">The chart whose items changed.</param>
        /// <param name="e">The dependency property change data.</param>
        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ColumnChart chart)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<ColumnChartItem> oldItems)
            {
                oldItems.CollectionChanged -= chart.OnItemsCollectionChanged;
            }

            chart.UnhookItems();

            if (e.NewValue is ObservableCollection<ColumnChartItem> newItems)
            {
                newItems.CollectionChanged += chart.OnItemsCollectionChanged;
                chart.HookItems(newItems);
            }

            chart.InvalidateChart();
        }

        /// <summary>
        /// Repaints the chart when a property that affects its rendering changes.
        /// </summary>
        /// <param name="d">The chart whose visual property changed.</param>
        /// <param name="e">The dependency property change data.</param>
        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ColumnChart)?.InvalidateChart();
        }

        /// <summary>
        /// Updates item subscriptions and invalidates the chart when the items collection changes.
        /// </summary>
        /// <param name="sender">The collection that raised the notification.</param>
        /// <param name="e">The collection change data.</param>
        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // A reset carries no old items, so rebuild the subscriptions from the current collection.
                this.UnhookItems();
                this.HookItems(this.Items);
            }
            else
            {
                if (e.OldItems is not null)
                {
                    foreach (var item in e.OldItems.OfType<ColumnChartItem>())
                    {
                        item.PropertyChanged -= this.OnItemPropertyChanged;
                        _hookedItems.Remove(item);
                    }
                }

                if (e.NewItems is not null)
                {
                    this.HookItems(e.NewItems.OfType<ColumnChartItem>());
                }
            }

            this.InvalidateChart();
        }

        /// <summary>
        /// Invalidates the chart when a displayed item's property changes.
        /// </summary>
        /// <param name="sender">The item that raised the notification.</param>
        /// <param name="e">The property change data.</param>
        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.InvalidateChart();
        }

        /// <summary>
        /// Subscribes to property changes for the specified chart items.
        /// </summary>
        /// <param name="items">The chart items to observe.</param>
        private void HookItems(IEnumerable<ColumnChartItem>? items)
        {
            if (items is null)
            {
                return;
            }

            foreach (var item in items)
            {
                item.PropertyChanged += this.OnItemPropertyChanged;
                _hookedItems.Add(item);
            }
        }

        /// <summary>
        /// Unsubscribes from property changes for all currently observed chart items.
        /// </summary>
        private void UnhookItems()
        {
            foreach (var item in _hookedItems)
            {
                item.PropertyChanged -= this.OnItemPropertyChanged;
            }

            _hookedItems.Clear();
        }

        /// <summary>
        /// Queues a repaint onto the dispatcher. A bulk update to the collection or to several items produces a
        /// burst of notifications, and coalescing them means the canvas is only rebuilt once.
        /// </summary>
        private void InvalidateChart()
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
        /// Clears the canvas and repaints the chart at the control's current size.
        /// </summary>
        public void Redraw()
        {
            MainCanvas.Children.Clear();
            this.Paint(this.ActualWidth, this.ActualHeight);
        }

        /// <summary>
        ///     Paints the column chart on the canvas.
        /// </summary>
        /// <param name="chartWidth">The width of the chart.</param>
        /// <param name="chartHeight">The height of the chart.</param>
        private void Paint(double chartWidth, double chartHeight)
        {
            if (chartWidth <= 0
                || double.IsNaN(chartWidth)
                || chartHeight <= 0
                || double.IsNaN(chartHeight)
                || this.IntervalCount <= 0
                || this.Items is null
                || this.Items.Count == 0)
            {
                return;
            }

            MainCanvas.Width = chartWidth;
            MainCanvas.Height = chartHeight;

            var yAxisEndPoint = new Point(this.InnerPadding.Left, this.InnerPadding.Top);
            var origin = new Point(this.InnerPadding.Left, chartHeight - this.InnerPadding.Bottom);
            var xAxisEndPoint = new Point(chartWidth - this.InnerPadding.Right, chartHeight - this.InnerPadding.Bottom);

            var yAxisStartLine = new Line
            {
                Stroke = this.StrokeBrush,
                StrokeThickness = this.StrokeThickness,
                X1 = yAxisEndPoint.X,
                Y1 = yAxisEndPoint.Y,
                X2 = origin.X,
                Y2 = origin.Y
            };
            MainCanvas.Children.Add(yAxisStartLine);

            var yAxisEndLine = new Line
            {
                Stroke = this.StrokeBrush,
                StrokeThickness = this.StrokeThickness,
                X1 = xAxisEndPoint.X,
                Y1 = xAxisEndPoint.Y,
                X2 = xAxisEndPoint.X,
                Y2 = yAxisEndPoint.Y
            };
            MainCanvas.Children.Add(yAxisEndLine);

            var maxValue = this.Items.Max(item => item.Value);

            // An empty scale (all zero or negative) would divide by zero below, so fall back to one unit per interval.
            if (maxValue <= 0)
            {
                maxValue = this.IntervalCount;
            }
            else if (maxValue % this.IntervalCount != 0)
            {
                maxValue = (int)Math.Ceiling(maxValue / (double)this.IntervalCount) * this.IntervalCount;
            }

            var chartInnerHeight = chartHeight - this.InnerPadding.Top - this.InnerPadding.Bottom;
            var intervalNumberToYPositionRatio = chartInnerHeight / this.IntervalCount;
            var intervalNumberToValueRatio = maxValue / this.IntervalCount;

            for (var currentIntervalNumber = 0; currentIntervalNumber <= this.IntervalCount; currentIntervalNumber++)
            {
                var currentYPosition = origin.Y - currentIntervalNumber * intervalNumberToYPositionRatio;

                var yLine = new Line
                {
                    Stroke = this.StrokeBrush,
                    StrokeThickness = this.StrokeThickness,
                    X1 = origin.X,
                    Y1 = currentYPosition,
                    X2 = xAxisEndPoint.X,
                    Y2 = currentYPosition
                };
                MainCanvas.Children.Add(yLine);

                var yAxisTextBlock = new TextBlock
                {
                    Text = $"{currentIntervalNumber * intervalNumberToValueRatio}",
                    Foreground = this.Foreground,
                    FontFamily = this.FontFamily,
                    FontSize = this.FontSize,
                    TextAlignment = TextAlignment.Right
                };
                MainCanvas.Children.Add(yAxisTextBlock);

                var yAxisTextBlockEstimatedSize = EstimateSize(yAxisTextBlock);
                Canvas.SetLeft(yAxisTextBlock, origin.X - yAxisTextBlockEstimatedSize.Width - Y_AXIS_TEXT_BLOCK_RIGHT_MARGIN);
                Canvas.SetTop(yAxisTextBlock, currentYPosition - yAxisTextBlockEstimatedSize.Height / 2);
            }

            var heightValueScale = chartInnerHeight / maxValue;
            const float originalBlockWidthRatio = 0.583333f;
            var chartInnerWidth = chartWidth - this.InnerPadding.Left - this.InnerPadding.Right;
            var blockWidth = chartInnerWidth / this.Items.Count * originalBlockWidthRatio;
            var blockMarginX = (chartInnerWidth / this.Items.Count - blockWidth) / 2;
            var currentXValue = origin.X;

            foreach (var item in this.Items)
            {
                currentXValue += blockMarginX;

                // A transparent strip spanning the full plot height, drawn behind the column, so the whole band
                // is clickable. Without it a zero valued column would have no hit area at all.
                var hitArea = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    Width = Math.Max(0, blockWidth),
                    Height = Math.Max(0, chartInnerHeight)
                };

                this.MakeClickable(hitArea, item);
                MainCanvas.Children.Add(hitArea);
                Canvas.SetLeft(hitArea, currentXValue);
                Canvas.SetTop(hitArea, origin.Y - hitArea.Height);

                var block = new Rectangle
                {
                    // A per item brush wins so a single column can be highlighted without recolouring the chart.
                    Fill = item.ColumnBrush ?? this.ColumnBrush,
                    Width = Math.Max(0, blockWidth),
                    Height = Math.Max(0, heightValueScale * item.Value),
                    ToolTip = $"{item.Name}: {item.Value}"
                };

                this.MakeClickable(block, item);
                MainCanvas.Children.Add(block);
                Canvas.SetLeft(block, currentXValue);
                Canvas.SetTop(block, origin.Y - block.Height);

                var blockHeader = new TextBlock
                {
                    Text = item.Name,
                    FontFamily = this.FontFamily,
                    FontSize = this.FontSize,
                    Foreground = this.Foreground,
                    TextAlignment = TextAlignment.Center,
                    Width = block.Width,
                    TextWrapping = TextWrapping.Wrap
                };

                this.MakeClickable(blockHeader, item);
                MainCanvas.Children.Add(blockHeader);
                Canvas.SetLeft(blockHeader, currentXValue);
                Canvas.SetTop(blockHeader, origin.Y + X_AXIS_TEXT_BLOCK_TOP_MARGIN);

                currentXValue += block.Width + blockMarginX;
            }
        }

        /// <summary>
        /// Associates a painted element with the item it was drawn from and hooks it up to raise
        /// <see cref="ColumnClicked"/> when it is clicked.
        /// </summary>
        /// <param name="element">The shape or text block that makes up part of a column.</param>
        /// <param name="item">The item the element was drawn from.</param>
        private void MakeClickable(FrameworkElement element, ColumnChartItem item)
        {
            element.Tag = item;
            element.Cursor = Cursors.Hand;
            element.MouseLeftButtonUp += this.OnColumnMouseLeftButtonUp;
        }

        /// <summary>
        /// Estimates the rendered size of the specified text block using its current typography settings.
        /// </summary>
        /// <param name="textBlock">The text block whose rendered size is estimated.</param>
        /// <returns>The estimated rendered size.</returns>
        private static Size EstimateSize(TextBlock textBlock)
        {
            var formattedText = new FormattedText(
                textBlock.Text,
                CultureInfo.CurrentCulture,
                textBlock.FlowDirection,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                textBlock.Foreground,
                VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

            return new Size(formattedText.Width, formattedText.Height);
        }

        /// <summary>
        /// Redraws the chart when the control's rendered size changes.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">The size change data.</param>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Redraw();
        }
    }
}
