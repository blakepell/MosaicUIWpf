/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A two-pane container, similar in usage to a uniform panel, whose panes are separated by a draggable
    /// <see cref="GridSplitter"/>. The proportion of space allocated to the first pane is exposed through the
    /// two-way <see cref="SplitterPosition"/> dependency property (0.0 = first pane collapsed, 1.0 = second pane
    /// collapsed). Set <see cref="Orientation"/> to <see cref="System.Windows.Controls.Orientation.Vertical"/>
    /// (the default) to stack the panes top/bottom, or <see cref="System.Windows.Controls.Orientation.Horizontal"/>
    /// to place them side-by-side.
    /// </summary>
    [TemplatePart(Name = PartRootGrid, Type = typeof(Grid))]
    [TemplatePart(Name = PartPane1, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartPane2, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartSplitter, Type = typeof(GridSplitter))]
    [DefaultProperty(nameof(Pane1))]
    [DefaultEvent(nameof(SplitterPositionChanged))]
    public class SplitPanel : Control
    {
        private const string PartRootGrid = "PART_RootGrid";
        private const string PartPane1 = "PART_Pane1";
        private const string PartPane2 = "PART_Pane2";
        private const string PartSplitter = "PART_Splitter";

        private Grid? _rootGrid;
        private ContentPresenter? _pane1Host;
        private ContentPresenter? _pane2Host;
        private GridSplitter? _splitter;
        private DefinitionBase? _definition1;
        private DefinitionBase? _definition2;

        /// <summary>
        /// True while the user is actively dragging the splitter. While dragging the splitter owns the
        /// grid lengths directly, so <see cref="SplitterPosition"/> changes are observed rather than applied
        /// to avoid fighting the live drag.
        /// </summary>
        private bool _isDragging;

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitPanel"/> class.
        /// </summary>
        public SplitPanel()
        {
            DefaultStyleKey = typeof(SplitPanel);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Detach from any previously templated splitter before re-templating.
            if (_splitter != null)
            {
                _splitter.DragStarted -= OnSplitterDragStarted;
                _splitter.DragDelta -= OnSplitterDragDelta;
                _splitter.DragCompleted -= OnSplitterDragCompleted;
            }

            base.OnApplyTemplate();

            _rootGrid = GetTemplateChild(PartRootGrid) as Grid;
            _pane1Host = GetTemplateChild(PartPane1) as ContentPresenter;
            _pane2Host = GetTemplateChild(PartPane2) as ContentPresenter;
            _splitter = GetTemplateChild(PartSplitter) as GridSplitter;

            if (_splitter != null)
            {
                _splitter.DragStarted += OnSplitterDragStarted;
                _splitter.DragDelta += OnSplitterDragDelta;
                _splitter.DragCompleted += OnSplitterDragCompleted;
            }

            ConfigureGrid();
        }

        /// <summary>
        /// Rebuilds the grid's row/column definitions for the current <see cref="Orientation"/> and applies the
        /// current <see cref="SplitterPosition"/>, <see cref="SplitterThickness"/> and <see cref="MinPaneSize"/>.
        /// </summary>
        private void ConfigureGrid()
        {
            if (_rootGrid == null || _pane1Host == null || _pane2Host == null || _splitter == null)
            {
                return;
            }

            _rootGrid.ColumnDefinitions.Clear();
            _rootGrid.RowDefinitions.Clear();

            double thickness = Math.Max(0.0, SplitterThickness);
            double pos = Clamp(SplitterPosition);
            double min = Math.Max(0.0, MinPaneSize);

            if (Orientation == Orientation.Horizontal)
            {
                var c1 = new ColumnDefinition { Width = new GridLength(pos, GridUnitType.Star), MinWidth = min };
                var cs = new ColumnDefinition { Width = new GridLength(thickness, GridUnitType.Pixel) };
                var c2 = new ColumnDefinition { Width = new GridLength(1.0 - pos, GridUnitType.Star), MinWidth = min };

                _rootGrid.ColumnDefinitions.Add(c1);
                _rootGrid.ColumnDefinitions.Add(cs);
                _rootGrid.ColumnDefinitions.Add(c2);

                Grid.SetColumn(_pane1Host, 0);
                Grid.SetRow(_pane1Host, 0);
                Grid.SetColumn(_splitter, 1);
                Grid.SetRow(_splitter, 0);
                Grid.SetColumn(_pane2Host, 2);
                Grid.SetRow(_pane2Host, 0);

                _splitter.ResizeDirection = GridResizeDirection.Columns;
                _splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                _splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                _splitter.VerticalAlignment = VerticalAlignment.Stretch;
                _splitter.Width = double.NaN;
                _splitter.Height = double.NaN;
                _splitter.Cursor = Cursors.SizeWE;

                _definition1 = c1;
                _definition2 = c2;
            }
            else
            {
                var r1 = new RowDefinition { Height = new GridLength(pos, GridUnitType.Star), MinHeight = min };
                var rs = new RowDefinition { Height = new GridLength(thickness, GridUnitType.Pixel) };
                var r2 = new RowDefinition { Height = new GridLength(1.0 - pos, GridUnitType.Star), MinHeight = min };

                _rootGrid.RowDefinitions.Add(r1);
                _rootGrid.RowDefinitions.Add(rs);
                _rootGrid.RowDefinitions.Add(r2);

                Grid.SetRow(_pane1Host, 0);
                Grid.SetColumn(_pane1Host, 0);
                Grid.SetRow(_splitter, 1);
                Grid.SetColumn(_splitter, 0);
                Grid.SetRow(_pane2Host, 2);
                Grid.SetColumn(_pane2Host, 0);

                _splitter.ResizeDirection = GridResizeDirection.Rows;
                _splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                _splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                _splitter.VerticalAlignment = VerticalAlignment.Stretch;
                _splitter.Width = double.NaN;
                _splitter.Height = double.NaN;
                _splitter.Cursor = Cursors.SizeNS;

                _definition1 = r1;
                _definition2 = r2;
            }
        }

        /// <summary>
        /// Pushes the current <see cref="SplitterPosition"/> onto the two star-sized definitions.
        /// </summary>
        private void ApplyPosition()
        {
            double pos = Clamp(SplitterPosition);

            switch (_definition1)
            {
                case ColumnDefinition c1 when _definition2 is ColumnDefinition c2:
                    c1.Width = new GridLength(pos, GridUnitType.Star);
                    c2.Width = new GridLength(1.0 - pos, GridUnitType.Star);
                    break;
                case RowDefinition r1 when _definition2 is RowDefinition r2:
                    r1.Height = new GridLength(pos, GridUnitType.Star);
                    r2.Height = new GridLength(1.0 - pos, GridUnitType.Star);
                    break;
            }
        }

        /// <summary>
        /// Reads the realized sizes of the two panes after a drag and reflects the ratio back into
        /// <see cref="SplitterPosition"/> without re-applying it to the (already correct) grid lengths.
        /// </summary>
        private void UpdatePositionFromDefinitions()
        {
            double size1;
            double size2;

            switch (_definition1)
            {
                case ColumnDefinition c1 when _definition2 is ColumnDefinition c2:
                    size1 = c1.ActualWidth;
                    size2 = c2.ActualWidth;
                    break;
                case RowDefinition r1 when _definition2 is RowDefinition r2:
                    size1 = r1.ActualHeight;
                    size2 = r2.ActualHeight;
                    break;
                default:
                    return;
            }

            double total = size1 + size2;
            if (total <= 0.0)
            {
                return;
            }

            // SetCurrentValue keeps any user binding intact; the changed callback sees _isDragging and skips
            // ApplyPosition, so the live drag is not disturbed while bindings still observe the new value.
            SetCurrentValue(SplitterPositionProperty, size1 / total);
        }

        private void OnSplitterDragStarted(object sender, DragStartedEventArgs e) => _isDragging = true;

        private void OnSplitterDragDelta(object sender, DragDeltaEventArgs e) => UpdatePositionFromDefinitions();

        private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
        {
            UpdatePositionFromDefinitions();
            _isDragging = false;
        }

        private static double Clamp(double value)
        {
            if (double.IsNaN(value))
            {
                return 0.5;
            }

            return value < 0.0 ? 0.0 : value > 1.0 ? 1.0 : value;
        }

        /// <summary>
        /// Gets or sets the content of the first pane (top when <see cref="Orientation"/> is vertical, left when horizontal).
        /// </summary>
        /// <value>The content of the first pane.</value>
        [Category("Common")]
        [Description("The content of the first pane (top/left).")]
        public object Pane1
        {
            get => GetValue(Pane1Property);
            set => SetValue(Pane1Property, value);
        }

        /// <summary>
        /// Identifies the <see cref="Pane1"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Pane1Property =
            DependencyProperty.Register(nameof(Pane1), typeof(object), typeof(SplitPanel), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content of the second pane (bottom when <see cref="Orientation"/> is vertical, right when horizontal).
        /// </summary>
        /// <value>The content of the second pane.</value>
        [Category("Common")]
        [Description("The content of the second pane (bottom/right).")]
        public object Pane2
        {
            get => GetValue(Pane2Property);
            set => SetValue(Pane2Property, value);
        }

        /// <summary>
        /// Identifies the <see cref="Pane2"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty Pane2Property =
            DependencyProperty.Register(nameof(Pane2), typeof(object), typeof(SplitPanel), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the proportion of the available space allocated to <see cref="Pane1"/>, expressed as a
        /// value between 0.0 and 1.0. A value of 0.0 collapses the first pane (0% / 100%), 1.0 collapses the
        /// second pane (100% / 0%) and 0.5 splits the space evenly. The value updates live as the user drags
        /// the splitter. This property is two-way by default.
        /// </summary>
        /// <value>The fraction (0.0 - 1.0) of the panel allocated to the first pane.</value>
        [Category("Layout")]
        [Description("The proportion (0.0 - 1.0) of space allocated to the first pane.")]
        public double SplitterPosition
        {
            get => (double)GetValue(SplitterPositionProperty);
            set => SetValue(SplitterPositionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SplitterPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SplitterPositionProperty =
            DependencyProperty.Register(nameof(SplitterPosition), typeof(double), typeof(SplitPanel),
                new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSplitterPositionChanged));

        private static void OnSplitterPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (SplitPanel)d;
            double oldValue = (double)e.OldValue;
            double newValue = (double)e.NewValue;
            double clamped = Clamp(newValue);

            if (clamped != newValue)
            {
                // Re-enter with the clamped value; the subsequent callback raises the event.
                panel.SetCurrentValue(SplitterPositionProperty, clamped);
                return;
            }

            // While the user drags, the splitter already owns the grid lengths; only push the value back into
            // the grid when the change originates from a binding or code (not the drag itself).
            if (!panel._isDragging)
            {
                panel.ApplyPosition();
            }

            panel.RaiseEvent(new RoutedPropertyChangedEventArgs<double>(oldValue, newValue, SplitterPositionChangedEvent));
        }

        /// <summary>
        /// Gets or sets the orientation of the split. <see cref="System.Windows.Controls.Orientation.Vertical"/>
        /// (the default) stacks the panes top/bottom with a horizontal splitter; <see cref="System.Windows.Controls.Orientation.Horizontal"/>
        /// places the panes side-by-side with a vertical splitter.
        /// </summary>
        /// <value>The orientation of the split.</value>
        [Category("Layout")]
        [Description("Whether the panes are stacked vertically (top/bottom) or horizontally (left/right).")]
        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Orientation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(SplitPanel),
                new FrameworkPropertyMetadata(Orientation.Vertical, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the thickness, in device-independent pixels, of the draggable splitter between the panes.
        /// </summary>
        /// <value>The splitter thickness in device-independent pixels.</value>
        [Category("Layout")]
        [Description("The thickness of the draggable splitter between the panes.")]
        public double SplitterThickness
        {
            get => (double)GetValue(SplitterThicknessProperty);
            set => SetValue(SplitterThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SplitterThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SplitterThicknessProperty =
            DependencyProperty.Register(nameof(SplitterThickness), typeof(double), typeof(SplitPanel),
                new FrameworkPropertyMetadata(6.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the minimum size, in device-independent pixels, that each pane is allowed to shrink to
        /// when the splitter is dragged. The default is 0, which allows a pane to fully collapse.
        /// </summary>
        /// <value>The minimum pane size in device-independent pixels.</value>
        [Category("Layout")]
        [Description("The minimum size each pane can be dragged down to (0 allows full collapse).")]
        public double MinPaneSize
        {
            get => (double)GetValue(MinPaneSizeProperty);
            set => SetValue(MinPaneSizeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinPaneSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinPaneSizeProperty =
            DependencyProperty.Register(nameof(MinPaneSize), typeof(double), typeof(SplitPanel),
                new FrameworkPropertyMetadata(0.0, OnLayoutPropertyChanged));

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SplitPanel)d).ConfigureGrid();

        /// <summary>
        /// Identifies the <see cref="SplitterPositionChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SplitterPositionChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SplitterPositionChanged), RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>), typeof(SplitPanel));

        /// <summary>
        /// Occurs when <see cref="SplitterPosition"/> changes, whether by dragging the splitter or by code/binding.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<double> SplitterPositionChanged
        {
            add => AddHandler(SplitterPositionChangedEvent, value);
            remove => RemoveHandler(SplitterPositionChangedEvent, value);
        }
    }
}
