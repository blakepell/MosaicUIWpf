/*
 * Originally based off (MIT): https://github.com/JMHeartley/WPF-Chart-Controls
 */

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A pie chart that renders an <see cref="ObservableCollection{T}"/> of <see cref="PieCategory"/> onto a
    /// canvas. Each slice is sized by its <see cref="PieCategory.Value"/> relative to the total of every value
    /// in the collection, so the values do not have to add up to any particular number. The chart repaints
    /// whenever it is resized, whenever the collection changes, and whenever a property on any category in the
    /// collection changes.
    /// </summary>
    /// <remarks>
    /// A category that leaves <see cref="PieCategory.ColorBrush"/> null is painted with a color from the
    /// chart's palette based on its position in the collection.
    /// </remarks>
    [DefaultProperty(nameof(Categories))]
    public partial class PieChart : UserControl
    {
        /// <summary>
        /// The colors used for categories that do not specify their own <see cref="PieCategory.ColorBrush"/>.
        /// </summary>
        private static readonly Brush[] DefaultPalette;

        /// <summary>
        /// The categories currently hooked for <see cref="INotifyPropertyChanged"/>. Tracked separately from
        /// <see cref="Categories"/> so a <see cref="NotifyCollectionChangedAction.Reset"/> (which carries no old
        /// items) can still be unhooked cleanly.
        /// </summary>
        private readonly List<PieCategory> _hookedCategories = new();

        /// <summary>
        /// Whether a repaint has already been queued onto the dispatcher. Coalesces the many notifications that
        /// a bulk update produces into a single paint.
        /// </summary>
        private bool _repaintQueued;

        /// <summary>
        /// Identifies the <see cref="Categories"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CategoriesProperty =
            DependencyProperty.Register(nameof(Categories), typeof(ObservableCollection<PieCategory>), typeof(PieChart),
                new FrameworkPropertyMetadata(null, OnCategoriesChanged));

        /// <summary>
        /// Gets or sets the collection of pie categories to be displayed in the chart. The chart subscribes to
        /// the collection and to each category, repainting when either changes.
        /// </summary>
        [Category("Common")]
        [Description("The collection of slices to display.")]
        public ObservableCollection<PieCategory> Categories
        {
            get => (ObservableCollection<PieCategory>)GetValue(CategoriesProperty);
            set => SetValue(CategoriesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StrokeBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeBrushProperty =
            DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(PieChart),
                new FrameworkPropertyMetadata(Brushes.White, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the brush used to draw the lines that separate the pie slices.
        /// </summary>
        [Category("Brush")]
        [Description("The brush used to draw the lines that separate the pie slices.")]
        public Brush StrokeBrush
        {
            get => (Brush)GetValue(StrokeBrushProperty);
            set => SetValue(StrokeBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(PieChart),
                new FrameworkPropertyMetadata(5d, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the thickness of the lines that separate the pie slices.
        /// </summary>
        [Category("Appearance")]
        [Description("The thickness of the lines that separate the pie slices.")]
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LegendPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LegendPositionProperty =
            DependencyProperty.Register(nameof(LegendPosition), typeof(LegendPosition), typeof(PieChart),
                new FrameworkPropertyMetadata(LegendPosition.Right, OnVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the position of the pie chart legend.
        /// </summary>
        [Category("Layout")]
        [Description("The position of the legend relative to the pie.")]
        public LegendPosition LegendPosition
        {
            get => (LegendPosition)GetValue(LegendPositionProperty);
            set => SetValue(LegendPositionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SliceClickCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SliceClickCommandProperty =
            DependencyProperty.Register(nameof(SliceClickCommand), typeof(ICommand), typeof(PieChart),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets a command invoked when a slice is clicked. The clicked <see cref="PieCategory"/> is
        /// passed as the command parameter, which is the MVVM equivalent of handling <see cref="SliceClicked"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Command invoked when a slice is clicked, with the clicked category as the parameter.")]
        public ICommand? SliceClickCommand
        {
            get => (ICommand?)GetValue(SliceClickCommandProperty);
            set => SetValue(SliceClickCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SliceClicked"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SliceClickedEvent =
            EventManager.RegisterRoutedEvent(nameof(SliceClicked), RoutingStrategy.Bubble,
                typeof(EventHandler<PieCategoryEventArgs>), typeof(PieChart));

        /// <summary>
        /// Occurs when a slice is clicked. The <see cref="PieCategoryEventArgs.Category"/> on the event args is
        /// the <see cref="PieCategory"/> that backs the clicked slice.
        /// </summary>
        [Category("Behavior")]
        [Description("Occurs when a slice is clicked.")]
        public event EventHandler<PieCategoryEventArgs> SliceClicked
        {
            add => this.AddHandler(SliceClickedEvent, value);
            remove => this.RemoveHandler(SliceClickedEvent, value);
        }

        static PieChart()
        {
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
        /// Initializes a new instance of the <see cref="PieChart"/> class.
        /// </summary>
        public PieChart()
        {
            this.FontSize = 20;

            InitializeComponent();

            // Give the chart a usable collection out of the box so a consumer can add categories without having
            // to construct one first. Assigned after InitializeComponent so it is never shared between instances.
            this.Categories = new ObservableCollection<PieCategory>();
        }

        /// <summary>
        /// Raises <see cref="SliceClicked"/> and invokes <see cref="SliceClickCommand"/> for the given category.
        /// </summary>
        /// <param name="category">The category backing the slice that was clicked.</param>
        protected virtual void OnSliceClicked(PieCategory category)
        {
            this.RaiseEvent(new PieCategoryEventArgs(SliceClickedEvent, this, category));

            var command = this.SliceClickCommand;

            if (command is not null && command.CanExecute(category))
            {
                command.Execute(category);
            }
        }

        /// <summary>
        /// Maps a click on a painted slice back to the category it was drawn from. The category is stashed on
        /// the shape's <see cref="FrameworkElement.Tag"/> when the canvas is built.
        /// </summary>
        /// <param name="sender">The painted element that received the click.</param>
        /// <param name="e">The mouse button event data.</param>
        private void OnSliceMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { Tag: PieCategory category })
            {
                this.OnSliceClicked(category);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Swaps the collection the chart is observing, moving the subscriptions from the old collection and its
        /// categories onto the new one.
        /// </summary>
        /// <param name="d">The chart whose categories changed.</param>
        /// <param name="e">The dependency property change data.</param>
        private static void OnCategoriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PieChart chart)
            {
                return;
            }

            if (e.OldValue is ObservableCollection<PieCategory> oldCategories)
            {
                oldCategories.CollectionChanged -= chart.OnCategoriesCollectionChanged;
            }

            chart.UnhookCategories();

            if (e.NewValue is ObservableCollection<PieCategory> newCategories)
            {
                newCategories.CollectionChanged += chart.OnCategoriesCollectionChanged;
                chart.HookCategories(newCategories);
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
            (d as PieChart)?.InvalidateChart();
        }

        /// <summary>
        /// Updates category subscriptions and invalidates the chart when the categories collection changes.
        /// </summary>
        /// <param name="sender">The collection that raised the notification.</param>
        /// <param name="e">The collection change data.</param>
        private void OnCategoriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // A reset carries no old items, so rebuild the subscriptions from the current collection.
                this.UnhookCategories();
                this.HookCategories(this.Categories);
            }
            else
            {
                if (e.OldItems is not null)
                {
                    foreach (var category in e.OldItems.OfType<PieCategory>())
                    {
                        category.PropertyChanged -= this.OnCategoryPropertyChanged;
                        _hookedCategories.Remove(category);
                    }
                }

                if (e.NewItems is not null)
                {
                    this.HookCategories(e.NewItems.OfType<PieCategory>());
                }
            }

            this.InvalidateChart();
        }

        /// <summary>
        /// Invalidates the chart when a displayed category's property changes.
        /// </summary>
        /// <param name="sender">The category that raised the notification.</param>
        /// <param name="e">The property change data.</param>
        private void OnCategoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Percentage and EffectiveBrush are written by the paint itself, so repainting off them would be
            // a repaint that schedules another repaint.
            if (e.PropertyName is nameof(PieCategory.Percentage) or nameof(PieCategory.EffectiveBrush))
            {
                return;
            }

            this.InvalidateChart();
        }

        /// <summary>
        /// Subscribes to property changes for the specified categories.
        /// </summary>
        /// <param name="categories">The categories to observe.</param>
        private void HookCategories(IEnumerable<PieCategory>? categories)
        {
            if (categories is null)
            {
                return;
            }

            foreach (var category in categories)
            {
                category.PropertyChanged += this.OnCategoryPropertyChanged;
                _hookedCategories.Add(category);
            }
        }

        /// <summary>
        /// Unsubscribes from property changes for all currently observed categories.
        /// </summary>
        private void UnhookCategories()
        {
            foreach (var category in _hookedCategories)
            {
                category.PropertyChanged -= this.OnCategoryPropertyChanged;
            }

            _hookedCategories.Clear();
        }

        /// <summary>
        /// Queues a repaint onto the dispatcher. A bulk update to the collection or to several categories
        /// produces a burst of notifications, and coalescing them means the canvas is only rebuilt once.
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
        /// Clears the canvas and repaints the pie at the control's current size.
        /// </summary>
        public void Redraw()
        {
            this.UpdateLegendPosition();

            MainCanvas.Children.Clear();

            var centerX = MainCanvas.ActualWidth / 2;
            var centerY = MainCanvas.ActualHeight / 2;
            var radius = Math.Min(centerX, centerY);

            var categories = this.Categories;

            if (categories is null || categories.Count == 0)
            {
                return;
            }

            // The share each slice gets is its value over the total, so the values can be raw counts rather
            // than percentages that have to add up to 100.
            var total = categories.Sum(category => Math.Max(0d, category.Value));

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                category.SetPaletteBrush(DefaultPalette[i % DefaultPalette.Length]);
                category.Percentage = total > 0 ? Math.Max(0d, category.Value) / total * 100 : 0;
            }

            if (radius <= 0 || double.IsNaN(radius) || total <= 0)
            {
                return;
            }

            // A single slice covering the whole pie has no angle to sweep, so it is drawn as a circle instead.
            var soleCategory = categories.FirstOrDefault(category => category.Percentage >= 100);

            if (soleCategory is not null)
            {
                var fullCircle = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Fill = soleCategory.EffectiveBrush,
                    Stroke = this.StrokeBrush,
                    StrokeThickness = this.StrokeThickness,
                    ToolTip = ToolTipFor(soleCategory)
                };

                this.MakeClickable(fullCircle, soleCategory);
                Canvas.SetLeft(fullCircle, centerX - radius);
                Canvas.SetTop(fullCircle, centerY - radius);

                MainCanvas.Children.Add(fullCircle);
                return;
            }

            var previousAngle = 0d;

            foreach (var category in categories)
            {
                previousAngle = DrawSlice(previousAngle, category);
            }

            return;

            double DrawSlice(double startAngle, PieCategory category)
            {
                var sweep = category.Percentage * 360 / 100;
                var endAngle = startAngle + sweep;

                // A zero valued slice has nothing to draw, but it still belongs in the legend.
                if (sweep <= 0)
                {
                    return endAngle;
                }

                var startX = radius * Math.Cos(startAngle * Math.PI / 180) + centerX;
                var startY = radius * Math.Sin(startAngle * Math.PI / 180) + centerY;
                var arcX = radius * Math.Cos(endAngle * Math.PI / 180) + centerX;
                var arcY = radius * Math.Sin(endAngle * Math.PI / 180) + centerY;

                var startLineSegment = new LineSegment(new Point(startX, startY), isStroked: false);
                var arcSegment = new ArcSegment
                {
                    Size = new Size(radius, radius),
                    Point = new Point(arcX, arcY),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = sweep > 180
                };
                var endLineSegment = new LineSegment(new Point(centerX, centerY), isStroked: false);

                var pathFigure = new PathFigure(
                    new Point(centerX, centerY),
                    new List<PathSegment>
                    {
                        startLineSegment,
                        arcSegment,
                        endLineSegment
                    },
                    closed: true);

                var path = new Path
                {
                    Fill = category.EffectiveBrush,
                    Data = new PathGeometry(new List<PathFigure> { pathFigure }),
                    ToolTip = ToolTipFor(category)
                };

                this.MakeClickable(path, category);
                MainCanvas.Children.Add(path);

                var outline1 = new Line
                {
                    X1 = centerX,
                    Y1 = centerY,
                    X2 = startLineSegment.Point.X,
                    Y2 = startLineSegment.Point.Y,
                    Stroke = this.StrokeBrush,
                    StrokeThickness = this.StrokeThickness,
                    IsHitTestVisible = false
                };
                var outline2 = new Line
                {
                    X1 = centerX,
                    Y1 = centerY,
                    X2 = arcSegment.Point.X,
                    Y2 = arcSegment.Point.Y,
                    Stroke = this.StrokeBrush,
                    StrokeThickness = this.StrokeThickness,
                    IsHitTestVisible = false
                };

                MainCanvas.Children.Add(outline1);
                MainCanvas.Children.Add(outline2);

                return endAngle;
            }
        }

        /// <summary>
        /// Builds the tooltip shown for a slice.
        /// </summary>
        /// <param name="category">The category the slice was drawn from.</param>
        /// <returns>The tooltip text.</returns>
        private static string ToolTipFor(PieCategory category)
        {
            return $"{category.Name}: {category.Value} ({category.Percentage:0.#}%)";
        }

        /// <summary>
        /// Associates a painted element with the category it was drawn from and hooks it up to raise
        /// <see cref="SliceClicked"/> when it is clicked.
        /// </summary>
        /// <param name="element">The shape that makes up the slice.</param>
        /// <param name="category">The category the element was drawn from.</param>
        private void MakeClickable(FrameworkElement element, PieCategory category)
        {
            element.Tag = category;
            element.Cursor = Cursors.Hand;
            element.MouseLeftButtonUp += this.OnSliceMouseLeftButtonUp;
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

        /// <summary>
        /// Redraws the chart once the legend has been populated and sized, which is what leaves the canvas its
        /// final amount of room.
        /// </summary>
        /// <param name="sender">The legend that raised the event.</param>
        /// <param name="e">The size change data.</param>
        private void LegendColumn_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.InvalidateChart();
        }

        /// <summary>
        /// Moves the legend into the grid cell that matches <see cref="LegendPosition"/>.
        /// </summary>
        private void UpdateLegendPosition()
        {
            switch (this.LegendPosition)
            {
                case LegendPosition.Bottom:
                    Grid.SetColumn(Legend, value: 1);
                    Grid.SetRow(Legend, value: 2);
                    break;
                case LegendPosition.Left:
                    Grid.SetColumn(Legend, value: 0);
                    Grid.SetRow(Legend, value: 1);
                    break;
                case LegendPosition.Right:
                    Grid.SetColumn(Legend, value: 2);
                    Grid.SetRow(Legend, value: 1);
                    break;
                case LegendPosition.Top:
                    Grid.SetColumn(Legend, value: 1);
                    Grid.SetRow(Legend, value: 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(LegendPosition)} is not a valid value.");
            }
        }
    }
}
