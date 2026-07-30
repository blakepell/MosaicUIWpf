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
using System.Windows.Automation.Peers;
using CommunityToolkit.Mvvm.Input;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A grid of cells used to pick a table size in the same manner as the Microsoft Word <c>Insert Table</c>
    /// button. Hovering a cell previews a region anchored at the upper left cell, clicking commits it and raises
    /// <see cref="TableSizeSelected"/> followed by <see cref="RequestClose"/> so a hosting popup can close.
    /// </summary>
    [TemplatePart(Name = PartGrid, Type = typeof(ItemsControl))]
    [DefaultEvent(nameof(TableSizeSelected))]
    [DefaultProperty(nameof(SelectedRowCount))]
    public class TableSizePicker : Control
    {
        private const string PartGrid = "PART_Grid";

        /// <summary>
        /// The largest number of rows or columns the picker will accept, this keeps the cell count bounded.
        /// </summary>
        private const int MaxDimension = 50;

        private ItemsControl? _itemsHost;
        private bool _isMouseDown;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="RowCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowCountProperty = DependencyProperty.Register(
            nameof(RowCount), typeof(int), typeof(TableSizePicker),
            new FrameworkPropertyMetadata(8, OnGridDimensionChanged, CoerceDimension));

        /// <summary>
        /// The number of rows of cells the picker displays. Values are coerced into the range 1 to 50.
        /// </summary>
        [Category("Mosaic")]
        [Description("The number of rows of cells the picker displays.")]
        public int RowCount
        {
            get => (int)this.GetValue(RowCountProperty);
            set => this.SetValue(RowCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnCountProperty = DependencyProperty.Register(
            nameof(ColumnCount), typeof(int), typeof(TableSizePicker),
            new FrameworkPropertyMetadata(8, OnGridDimensionChanged, CoerceDimension));

        /// <summary>
        /// The number of columns of cells the picker displays. Values are coerced into the range 1 to 50.
        /// </summary>
        [Category("Mosaic")]
        [Description("The number of columns of cells the picker displays.")]
        public int ColumnCount
        {
            get => (int)this.GetValue(ColumnCountProperty);
            set => this.SetValue(ColumnCountProperty, value);
        }

        private static readonly DependencyPropertyKey SelectedRowCountPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SelectedRowCount), typeof(int), typeof(TableSizePicker), new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="SelectedRowCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedRowCountProperty = SelectedRowCountPropertyKey.DependencyProperty;

        /// <summary>
        /// The number of rows in the committed selection, or zero when nothing has been committed.
        /// </summary>
        [Category("Mosaic")]
        [Description("The number of rows in the committed selection, zero when there is no selection.")]
        public int SelectedRowCount
        {
            get => (int)this.GetValue(SelectedRowCountProperty);
            private set => this.SetValue(SelectedRowCountPropertyKey, value);
        }

        private static readonly DependencyPropertyKey SelectedColumnCountPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SelectedColumnCount), typeof(int), typeof(TableSizePicker), new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="SelectedColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedColumnCountProperty = SelectedColumnCountPropertyKey.DependencyProperty;

        /// <summary>
        /// The number of columns in the committed selection, or zero when nothing has been committed.
        /// </summary>
        [Category("Mosaic")]
        [Description("The number of columns in the committed selection, zero when there is no selection.")]
        public int SelectedColumnCount
        {
            get => (int)this.GetValue(SelectedColumnCountProperty);
            private set => this.SetValue(SelectedColumnCountPropertyKey, value);
        }

        private static readonly DependencyPropertyKey PreviewRowCountPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PreviewRowCount), typeof(int), typeof(TableSizePicker), new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="PreviewRowCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviewRowCountProperty = PreviewRowCountPropertyKey.DependencyProperty;

        /// <summary>
        /// The number of rows currently being previewed, or zero when there is no preview. The preview never
        /// overwrites the committed selection.
        /// </summary>
        [Category("Mosaic")]
        [Description("The number of rows currently being previewed, zero when there is no preview.")]
        public int PreviewRowCount
        {
            get => (int)this.GetValue(PreviewRowCountProperty);
            private set => this.SetValue(PreviewRowCountPropertyKey, value);
        }

        private static readonly DependencyPropertyKey PreviewColumnCountPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(PreviewColumnCount), typeof(int), typeof(TableSizePicker), new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="PreviewColumnCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviewColumnCountProperty = PreviewColumnCountPropertyKey.DependencyProperty;

        /// <summary>
        /// The number of columns currently being previewed, or zero when there is no preview. The preview never
        /// overwrites the committed selection.
        /// </summary>
        [Category("Mosaic")]
        [Description("The number of columns currently being previewed, zero when there is no preview.")]
        public int PreviewColumnCount
        {
            get => (int)this.GetValue(PreviewColumnCountProperty);
            private set => this.SetValue(PreviewColumnCountPropertyKey, value);
        }

        private static readonly DependencyPropertyKey CellsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Cells), typeof(ObservableCollection<TableSizePickerCell>), typeof(TableSizePicker),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Cells"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellsProperty = CellsPropertyKey.DependencyProperty;

        /// <summary>
        /// The cells rendered by the control template, in row major order. Rebuilt whenever
        /// <see cref="RowCount"/> or <see cref="ColumnCount"/> changes.
        /// </summary>
        [Browsable(false)]
        public ObservableCollection<TableSizePickerCell> Cells
        {
            get => (ObservableCollection<TableSizePickerCell>)this.GetValue(CellsProperty);
            private set => this.SetValue(CellsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey SelectionTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SelectionText), typeof(string), typeof(TableSizePicker), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="SelectionText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionTextProperty = SelectionTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The caption displayed with the grid, for example <c>4 × 6 Table</c>. Reflects the preview dimensions
        /// while hovering, the committed dimensions otherwise, and <see cref="EmptySelectionText"/> when neither
        /// exists.
        /// </summary>
        [Category("Mosaic")]
        [Description("The caption displayed with the grid.")]
        public string SelectionText
        {
            get => (string)this.GetValue(SelectionTextProperty);
            private set => this.SetValue(SelectionTextPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectionTextFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectionTextFormatProperty = DependencyProperty.Register(
            nameof(SelectionTextFormat), typeof(string), typeof(TableSizePicker),
            new FrameworkPropertyMetadata("{0} × {1} Table", OnSelectionTextInputChanged));

        /// <summary>
        /// The composite format string used to build <see cref="SelectionText"/> where <c>{0}</c> is the row
        /// count and <c>{1}</c> is the column count. Defaults to <c>{0} × {1} Table</c>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Composite format string for the caption where {0} is rows and {1} is columns.")]
        public string SelectionTextFormat
        {
            get => (string)this.GetValue(SelectionTextFormatProperty);
            set => this.SetValue(SelectionTextFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EmptySelectionText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EmptySelectionTextProperty = DependencyProperty.Register(
            nameof(EmptySelectionText), typeof(string), typeof(TableSizePicker),
            new FrameworkPropertyMetadata("Insert Table", OnSelectionTextInputChanged));

        /// <summary>
        /// The caption displayed when there is neither a preview nor a committed selection.
        /// </summary>
        [Category("Mosaic")]
        [Description("The caption displayed when nothing is previewed or selected.")]
        public string EmptySelectionText
        {
            get => (string)this.GetValue(EmptySelectionTextProperty);
            set => this.SetValue(EmptySelectionTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ClearSelectionOnShow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClearSelectionOnShowProperty = DependencyProperty.Register(
            nameof(ClearSelectionOnShow), typeof(bool), typeof(TableSizePicker), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Whether <see cref="OnShow"/> clears the committed selection. Defaults to <c>true</c> since an insert
        /// table picker normally opens with nothing selected.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether OnShow clears the committed selection.")]
        public bool ClearSelectionOnShow
        {
            get => (bool)this.GetValue(ClearSelectionOnShowProperty);
            set => this.SetValue(ClearSelectionOnShowProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CellWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellWidthProperty = DependencyProperty.Register(
            nameof(CellWidth), typeof(double), typeof(TableSizePicker), new FrameworkPropertyMetadata(16.0));

        /// <summary>
        /// The width of an individual cell.
        /// </summary>
        [Category("Mosaic")]
        [Description("The width of an individual cell.")]
        public double CellWidth
        {
            get => (double)this.GetValue(CellWidthProperty);
            set => this.SetValue(CellWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CellHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellHeightProperty = DependencyProperty.Register(
            nameof(CellHeight), typeof(double), typeof(TableSizePicker), new FrameworkPropertyMetadata(16.0));

        /// <summary>
        /// The height of an individual cell.
        /// </summary>
        [Category("Mosaic")]
        [Description("The height of an individual cell.")]
        public double CellHeight
        {
            get => (double)this.GetValue(CellHeightProperty);
            set => this.SetValue(CellHeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CellMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellMarginProperty = DependencyProperty.Register(
            nameof(CellMargin), typeof(Thickness), typeof(TableSizePicker), new FrameworkPropertyMetadata(new Thickness(1)));

        /// <summary>
        /// The spacing rendered around each cell.
        /// </summary>
        [Category("Mosaic")]
        [Description("The spacing rendered around each cell.")]
        public Thickness CellMargin
        {
            get => (Thickness)this.GetValue(CellMarginProperty);
            set => this.SetValue(CellMarginProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CellBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellBackgroundProperty = DependencyProperty.Register(
            nameof(CellBackground), typeof(Brush), typeof(TableSizePicker), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The fill used for cells that are neither previewed nor selected.
        /// </summary>
        [Category("Mosaic")]
        [Description("The fill used for unselected cells.")]
        public Brush? CellBackground
        {
            get => (Brush?)this.GetValue(CellBackgroundProperty);
            set => this.SetValue(CellBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CellBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellBorderBrushProperty = DependencyProperty.Register(
            nameof(CellBorderBrush), typeof(Brush), typeof(TableSizePicker), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The outline used for cells that are neither previewed nor selected.
        /// </summary>
        [Category("Mosaic")]
        [Description("The outline used for unselected cells.")]
        public Brush? CellBorderBrush
        {
            get => (Brush?)this.GetValue(CellBorderBrushProperty);
            set => this.SetValue(CellBorderBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HighlightBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightBackgroundProperty = DependencyProperty.Register(
            nameof(HighlightBackground), typeof(Brush), typeof(TableSizePicker), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The fill used for cells inside the hover or keyboard preview region.
        /// </summary>
        [Category("Mosaic")]
        [Description("The fill used for previewed cells.")]
        public Brush? HighlightBackground
        {
            get => (Brush?)this.GetValue(HighlightBackgroundProperty);
            set => this.SetValue(HighlightBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HighlightBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightBorderBrushProperty = DependencyProperty.Register(
            nameof(HighlightBorderBrush), typeof(Brush), typeof(TableSizePicker), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The outline used for cells inside the hover or keyboard preview region.
        /// </summary>
        [Category("Mosaic")]
        [Description("The outline used for previewed cells.")]
        public Brush? HighlightBorderBrush
        {
            get => (Brush?)this.GetValue(HighlightBorderBrushProperty);
            set => this.SetValue(HighlightBorderBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommittedBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommittedBackgroundProperty = DependencyProperty.Register(
            nameof(CommittedBackground), typeof(Brush), typeof(TableSizePicker), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The fill used for cells inside the committed selection.
        /// </summary>
        [Category("Mosaic")]
        [Description("The fill used for cells inside the committed selection.")]
        public Brush? CommittedBackground
        {
            get => (Brush?)this.GetValue(CommittedBackgroundProperty);
            set => this.SetValue(CommittedBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommittedBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommittedBorderBrushProperty = DependencyProperty.Register(
            nameof(CommittedBorderBrush), typeof(Brush), typeof(TableSizePicker), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The outline used for cells inside the committed selection.
        /// </summary>
        [Category("Mosaic")]
        [Description("The outline used for cells inside the committed selection.")]
        public Brush? CommittedBorderBrush
        {
            get => (Brush?)this.GetValue(CommittedBorderBrushProperty);
            set => this.SetValue(CommittedBorderBrushProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Identifies the <see cref="TableSizeSelected"/> routed event.
        /// </summary>
        public static readonly RoutedEvent TableSizeSelectedEvent = EventManager.RegisterRoutedEvent(
            nameof(TableSizeSelected), RoutingStrategy.Bubble, typeof(EventHandler<TableSizeSelectedEventArgs>), typeof(TableSizePicker));

        /// <summary>
        /// Occurs when the user commits a table size. Bubbles so a hosting ribbon, popup or view can handle it.
        /// </summary>
        public event EventHandler<TableSizeSelectedEventArgs> TableSizeSelected
        {
            add => this.AddHandler(TableSizeSelectedEvent, value);
            remove => this.RemoveHandler(TableSizeSelectedEvent, value);
        }

        /// <summary>
        /// Occurs after a selection is committed or when the user presses <c>Escape</c>. A hosting popup or
        /// dropdown should close itself in response, the picker has no knowledge of its host.
        /// </summary>
        public event EventHandler? RequestClose;

        #endregion

        /// <summary>
        /// A command that calls <see cref="Clear"/>.
        /// </summary>
        public ICommand ClearCommand { get; }

        /// <summary>
        /// Initializes static metadata for the <see cref="TableSizePicker"/> class.
        /// </summary>
        static TableSizePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TableSizePicker), new FrameworkPropertyMetadata(typeof(TableSizePicker)));
            FocusableProperty.OverrideMetadata(typeof(TableSizePicker), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSizePicker"/> class.
        /// </summary>
        public TableSizePicker()
        {
            this.ClearCommand = new RelayCommand(this.Clear);
            this.Cells = new ObservableCollection<TableSizePickerCell>();
            this.RebuildCells();
            this.UpdateSelectionText();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _itemsHost = this.GetTemplateChild(PartGrid) as ItemsControl;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TableSizePickerAutomationPeer(this);
        }

        #region Public API

        /// <summary>
        /// Clears the preview and the committed selection returning the control to its initial state.
        /// </summary>
        public void Clear()
        {
            this.PreviewRowCount = 0;
            this.PreviewColumnCount = 0;
            this.SelectedRowCount = 0;
            this.SelectedColumnCount = 0;
            this.UpdateCellStates();
            this.UpdateSelectionText();
        }

        /// <summary>
        /// Prepares the control for interaction. Clears any stale hover state, optionally clears the committed
        /// selection based on <see cref="ClearSelectionOnShow"/>, and moves focus to the control so the keyboard
        /// can be used immediately. Call this when the hosting popup or dropdown opens.
        /// </summary>
        public void OnShow()
        {
            if (this.ClearSelectionOnShow)
            {
                this.Clear();
            }
            else
            {
                this.ClearPreview();
            }

            if (this.Focusable)
            {
                this.Focus();
            }
        }

        /// <summary>
        /// Returns the control to a clean state, clearing the preview and releasing the mouse if it is captured.
        /// The committed selection is preserved. Called automatically after a selection is committed.
        /// </summary>
        public void OnHide()
        {
            _isMouseDown = false;

            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
            }

            this.ClearPreview();
        }

        /// <summary>
        /// Commits the supplied table size exactly as a click would, raising <see cref="TableSizeSelected"/> and
        /// then <see cref="RequestClose"/>.
        /// </summary>
        /// <param name="rowCount">The one based row count to commit.</param>
        /// <param name="columnCount">The one based column count to commit.</param>
        public void Select(int rowCount, int columnCount)
        {
            if (rowCount < 1 || columnCount < 1 || rowCount > this.RowCount || columnCount > this.ColumnCount)
            {
                return;
            }

            this.SelectedRowCount = rowCount;
            this.SelectedColumnCount = columnCount;
            this.PreviewRowCount = 0;
            this.PreviewColumnCount = 0;
            this.UpdateCellStates();
            this.UpdateSelectionText();

            this.RaiseEvent(new TableSizeSelectedEventArgs(TableSizeSelectedEvent, this, rowCount, columnCount));

            this.OnHide();
            this.RequestClose?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Property Change Handling

        private static object CoerceDimension(DependencyObject d, object baseValue)
        {
            return Math.Clamp((int)baseValue, 1, MaxDimension);
        }

        private static void OnGridDimensionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (TableSizePicker)d;
            picker.RebuildCells();

            // Coerce any existing selection or preview back inside the new bounds.
            picker.SelectedRowCount = Math.Min(picker.SelectedRowCount, picker.RowCount);
            picker.SelectedColumnCount = Math.Min(picker.SelectedColumnCount, picker.ColumnCount);
            picker.PreviewRowCount = Math.Min(picker.PreviewRowCount, picker.RowCount);
            picker.PreviewColumnCount = Math.Min(picker.PreviewColumnCount, picker.ColumnCount);

            picker.UpdateCellStates();
            picker.UpdateSelectionText();
        }

        private static void OnSelectionTextInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TableSizePicker)d).UpdateSelectionText();
        }

        #endregion

        #region Input

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!this.IsEnabled || _itemsHost is null)
            {
                return;
            }

            if (this.TryGetCell(e.GetPosition(_itemsHost), out int row, out int column))
            {
                this.SetPreview(row, column);
            }
            else
            {
                this.ClearPreview();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            _isMouseDown = false;
            this.ClearPreview();
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (!this.IsEnabled || _itemsHost is null)
            {
                return;
            }

            if (!this.TryGetCell(e.GetPosition(_itemsHost), out int row, out int column))
            {
                return;
            }

            this.SetPreview(row, column);
            this.Focus();

            // The mouse is deliberately not captured here. A hosting Popup owns the capture in order to detect
            // clicks outside of itself and stealing it breaks that. Tracking the press is enough, a press that
            // started elsewhere never sets the flag and therefore never commits.
            _isMouseDown = true;
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            bool wasPressed = _isMouseDown;
            _isMouseDown = false;

            if (this.IsMouseCaptured)
            {
                this.ReleaseMouseCapture();
            }

            if (!wasPressed || !this.IsEnabled || _itemsHost is null)
            {
                return;
            }

            if (this.TryGetCell(e.GetPosition(_itemsHost), out int row, out int column))
            {
                e.Handled = true;
                this.Select(row, column);
            }
        }

        /// <inheritdoc />
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            _isMouseDown = false;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || !this.IsEnabled)
            {
                return;
            }

            // The keyboard walks from wherever the preview is, falling back to the committed selection and then
            // to the top left cell.
            int row = this.PreviewRowCount > 0 ? this.PreviewRowCount : Math.Max(this.SelectedRowCount, 1);
            int column = this.PreviewColumnCount > 0 ? this.PreviewColumnCount : Math.Max(this.SelectedColumnCount, 1);
            bool hasPreview = this.PreviewRowCount > 0 && this.PreviewColumnCount > 0;

            switch (e.Key)
            {
                case Key.Left:
                    this.SetPreview(row, hasPreview ? column - 1 : column);
                    break;
                case Key.Right:
                    this.SetPreview(row, hasPreview ? column + 1 : column);
                    break;
                case Key.Up:
                    this.SetPreview(hasPreview ? row - 1 : row, column);
                    break;
                case Key.Down:
                    this.SetPreview(hasPreview ? row + 1 : row, column);
                    break;
                case Key.Home:
                    this.SetPreview(1, 1);
                    break;
                case Key.End:
                    this.SetPreview(this.RowCount, this.ColumnCount);
                    break;
                case Key.Enter:
                case Key.Space:
                    if (hasPreview)
                    {
                        this.Select(row, column);
                    }
                    break;
                case Key.Escape:
                    this.ClearPreview();
                    this.RequestClose?.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }

        #endregion

        #region Internals

        /// <summary>
        /// Maps a point inside the items host onto a one based row and column. The uniform grid divides its
        /// space evenly so the cell can be computed rather than hit tested.
        /// </summary>
        private bool TryGetCell(Point point, out int row, out int column)
        {
            row = 0;
            column = 0;

            if (_itemsHost is null)
            {
                return false;
            }

            double width = _itemsHost.ActualWidth;
            double height = _itemsHost.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                return false;
            }

            if (point.X < 0 || point.Y < 0 || point.X >= width || point.Y >= height)
            {
                return false;
            }

            column = Math.Clamp((int)(point.X / (width / this.ColumnCount)) + 1, 1, this.ColumnCount);
            row = Math.Clamp((int)(point.Y / (height / this.RowCount)) + 1, 1, this.RowCount);

            return true;
        }

        /// <summary>
        /// Sets the preview region, clamping it into the grid. The committed selection is left untouched.
        /// </summary>
        private void SetPreview(int row, int column)
        {
            row = Math.Clamp(row, 1, this.RowCount);
            column = Math.Clamp(column, 1, this.ColumnCount);

            if (this.PreviewRowCount == row && this.PreviewColumnCount == column)
            {
                return;
            }

            this.PreviewRowCount = row;
            this.PreviewColumnCount = column;
            this.UpdateCellStates();
            this.UpdateSelectionText();
        }

        /// <summary>
        /// Drops the preview region leaving any committed selection visible.
        /// </summary>
        private void ClearPreview()
        {
            if (this.PreviewRowCount == 0 && this.PreviewColumnCount == 0)
            {
                return;
            }

            this.PreviewRowCount = 0;
            this.PreviewColumnCount = 0;
            this.UpdateCellStates();
            this.UpdateSelectionText();
        }

        /// <summary>
        /// Recreates the cell collection for the current row and column counts.
        /// </summary>
        private void RebuildCells()
        {
            var cells = this.Cells;

            if (cells is null)
            {
                return;
            }

            cells.Clear();

            for (int row = 1; row <= this.RowCount; row++)
            {
                for (int column = 1; column <= this.ColumnCount; column++)
                {
                    cells.Add(new TableSizePickerCell(row, column));
                }
            }
        }

        /// <summary>
        /// Pushes the current preview or committed region onto the cells. While a preview is active it is the
        /// only region shown so the two highlights never compete.
        /// </summary>
        private void UpdateCellStates()
        {
            var cells = this.Cells;

            if (cells is null)
            {
                return;
            }

            bool previewing = this.PreviewRowCount > 0 && this.PreviewColumnCount > 0;
            int rows = previewing ? this.PreviewRowCount : this.SelectedRowCount;
            int columns = previewing ? this.PreviewColumnCount : this.SelectedColumnCount;

            foreach (var cell in cells)
            {
                bool inRegion = rows > 0 && columns > 0 && cell.Row <= rows && cell.Column <= columns;

                cell.IsPreviewSelected = previewing && inRegion;
                cell.IsCommittedSelected = !previewing && inRegion;
                cell.IsAnchor = inRegion && cell.Row == rows && cell.Column == columns;
            }
        }

        /// <summary>
        /// Rebuilds <see cref="SelectionText"/> from the preview, then the committed selection, then the
        /// <see cref="EmptySelectionText"/> prompt.
        /// </summary>
        private void UpdateSelectionText()
        {
            int rows = this.PreviewRowCount > 0 ? this.PreviewRowCount : this.SelectedRowCount;
            int columns = this.PreviewColumnCount > 0 ? this.PreviewColumnCount : this.SelectedColumnCount;

            if (rows <= 0 || columns <= 0)
            {
                this.SelectionText = this.EmptySelectionText ?? string.Empty;
                return;
            }

            try
            {
                this.SelectionText = string.Format(this.SelectionTextFormat ?? "{0} × {1}", rows, columns);
            }
            catch (FormatException)
            {
                // A caller supplied format string that we cannot honor, fall back to the default shape.
                this.SelectionText = $"{rows} × {columns}";
            }
        }

        #endregion
    }
}
