/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Collections.ObjectModel;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single vertical wheel used by <see cref="DateSpinner"/> to choose a month, a day, or a year. The selected
    /// entry sits in a highlighted band in the middle of the control with neighbouring values visible above and
    /// below it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The control derives from <see cref="ListBox"/> so that virtualization, selection, and item containers come
    /// for free. Its panel is a pixel-scrolling <see cref="VirtualizingStackPanel"/>, which keeps the realized
    /// container count proportional to <see cref="VisibleItemCount"/> rather than to the number of values, so a
    /// range of ten thousand years costs the same as a range of ten.
    /// </para>
    /// <para>
    /// Centring is achieved by padding each end of the list with blank <see cref="DateSpinnerItem.IsSpacer"/>
    /// entries. With <see cref="VisibleItemCount"/> items on screen and half that many spacers at each end, the
    /// scroll offset that centres real index <c>i</c> is exactly <c>i * ItemHeight</c>, so no measurement of the
    /// realized containers is needed.
    /// </para>
    /// <para>
    /// The wheel is a single tab stop. Its containers remain focusable so standard <see cref="ListBoxItem"/>
    /// click and tap selection works, but they are not tab stops, so Tab moves between whole wheels rather than
    /// between individual values. Arrow keys are handled here rather than by the base class's focus-driven
    /// navigation.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PartScrollViewer, Type = typeof(InertiaScrollViewer))]
    [DefaultEvent(nameof(ValueChanged))]
    [DefaultProperty(nameof(SelectedNumber))]
    public class DateSpinnerSelector : ListBox
    {
        private const string PartScrollViewer = "PART_ScrollViewer";

        /// <summary>
        /// How long the wheel must be still after a free scroll (touch inertia, scroll bar drag) before it snaps
        /// onto the nearest value.
        /// </summary>
        private static readonly TimeSpan SnapDelay = TimeSpan.FromMilliseconds(140);

        private readonly ObservableCollection<DateSpinnerItem> _entries = new();
        private readonly DispatcherTimer _snapTimer;

        private InertiaScrollViewer? _scrollViewer;
        private bool _isSynchronizing;
        private int _pendingScrollIndex = -1;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Values"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            nameof(Values), typeof(IReadOnlyList<DateSpinnerItem>), typeof(DateSpinnerSelector),
            new FrameworkPropertyMetadata(null, OnValuesChanged));

        /// <summary>
        /// The values the wheel offers, in ascending order. The wheel owns the spacer padding around them, so this
        /// collection should contain nothing but real values.
        /// </summary>
        [Category("Mosaic")]
        [Description("The values the wheel offers, in ascending order.")]
        public IReadOnlyList<DateSpinnerItem>? Values
        {
            get => (IReadOnlyList<DateSpinnerItem>?)this.GetValue(ValuesProperty);
            set => this.SetValue(ValuesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedNumber"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedNumberProperty = DependencyProperty.Register(
            nameof(SelectedNumber), typeof(int?), typeof(DateSpinnerSelector),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedNumberChanged));

        /// <summary>
        /// The <see cref="DateSpinnerItem.Value"/> of the centred entry, or <see langword="null"/> when the wheel
        /// has no selection. Setting a value that the wheel does not offer clears the selection.
        /// </summary>
        [Category("Mosaic")]
        [Description("The numeric value of the centred entry.")]
        public int? SelectedNumber
        {
            get => (int?)this.GetValue(SelectedNumberProperty);
            set => this.SetValue(SelectedNumberProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            nameof(ItemHeight), typeof(double), typeof(DateSpinnerSelector),
            new FrameworkPropertyMetadata(30.0, OnMetricsChanged, CoerceItemHeight));

        /// <summary>
        /// The fixed height of a single entry. A uniform height is what makes the wheel's scroll arithmetic exact,
        /// so entries are not allowed to size to their content.
        /// </summary>
        [Category("Mosaic")]
        [Description("The fixed height of a single entry.")]
        public double ItemHeight
        {
            get => (double)this.GetValue(ItemHeightProperty);
            set => this.SetValue(ItemHeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VisibleItemCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibleItemCountProperty = DependencyProperty.Register(
            nameof(VisibleItemCount), typeof(int), typeof(DateSpinnerSelector),
            new FrameworkPropertyMetadata(5, OnMetricsChanged, CoerceVisibleItemCount));

        /// <summary>
        /// How many entries are on screen at once. Coerced to an odd number of at least three so that exactly one
        /// entry can sit in the centre band with the same number of neighbours above and below it.
        /// </summary>
        [Category("Mosaic")]
        [Description("How many entries are visible at once. Coerced to an odd number of at least three.")]
        public int VisibleItemCount
        {
            get => (int)this.GetValue(VisibleItemCountProperty);
            set => this.SetValue(VisibleItemCountProperty, value);
        }

        private static readonly DependencyPropertyKey ViewportHeightPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ViewportHeight), typeof(double), typeof(DateSpinnerSelector), new FrameworkPropertyMetadata(150.0));

        /// <summary>
        /// Identifies the <see cref="ViewportHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewportHeightProperty = ViewportHeightPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="ItemHeight"/> multiplied by <see cref="VisibleItemCount"/>. The default style binds the
        /// wheel's height to this so the viewport always holds a whole number of entries.
        /// </summary>
        [Browsable(false)]
        public double ViewportHeight
        {
            get => (double)this.GetValue(ViewportHeightProperty);
            private set => this.SetValue(ViewportHeightPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsScrollAnimationEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsScrollAnimationEnabledProperty = DependencyProperty.Register(
            nameof(IsScrollAnimationEnabled), typeof(bool), typeof(DateSpinnerSelector),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Whether the wheel eases into its new position using the <see cref="InertiaScrollViewer"/> in its
        /// template. Set to <see langword="false"/> to move instantly, which is the appropriate behavior when the
        /// user has asked the system to reduce motion.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the wheel eases into position rather than jumping.")]
        public bool IsScrollAnimationEnabled
        {
            get => (bool)this.GetValue(IsScrollAnimationEnabledProperty);
            set => this.SetValue(IsScrollAnimationEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ScrollAnimationDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollAnimationDurationProperty = DependencyProperty.Register(
            nameof(ScrollAnimationDuration), typeof(int), typeof(DateSpinnerSelector),
            new FrameworkPropertyMetadata(160));

        /// <summary>
        /// The length of the easing animation in milliseconds. Kept deliberately short, a value wheel should feel
        /// responsive rather than decorative.
        /// </summary>
        [Category("Mosaic")]
        [Description("The length of the easing animation in milliseconds.")]
        public int ScrollAnimationDuration
        {
            get => (int)this.GetValue(ScrollAnimationDurationProperty);
            set => this.SetValue(ScrollAnimationDurationProperty, value);
        }

        #endregion

        /// <summary>
        /// Occurs when <see cref="SelectedNumber"/> changes, whether from user interaction or from a programmatic
        /// assignment. The owning <see cref="DateSpinner"/> guards against reentrancy on its side.
        /// </summary>
        public event EventHandler? ValueChanged;

        /// <summary>
        /// Initializes static metadata for the <see cref="DateSpinnerSelector"/> class.
        /// </summary>
        static DateSpinnerSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateSpinnerSelector), new FrameworkPropertyMetadata(typeof(DateSpinnerSelector)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateSpinnerSelector"/> class.
        /// </summary>
        public DateSpinnerSelector()
        {
            this.ItemsSource = _entries;
            this.ViewportHeight = this.ItemHeight * this.VisibleItemCount;

            _snapTimer = new DispatcherTimer(DispatcherPriority.Input) { Interval = SnapDelay };
            _snapTimer.Tick += this.OnSnapTimerTick;

            this.Loaded += this.OnSelectorLoaded;
            this.Unloaded += this.OnSelectorUnloaded;
        }

        /// <summary>
        /// The number of blank entries padding each end of the list. Half a viewport, so that the first and last
        /// real value can reach the centre band.
        /// </summary>
        private int SpacerCount => (this.VisibleItemCount - 1) / 2;

        /// <summary>
        /// The index into <see cref="Values"/> of the current selection, or -1 when nothing is selected.
        /// </summary>
        private int SelectedRealIndex
        {
            get
            {
                int index = this.SelectedIndex;

                return index < 0 ? -1 : index - this.SpacerCount;
            }
        }

        #region Template

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged -= this.OnViewerScrollChanged;
            }

            base.OnApplyTemplate();

            _scrollViewer = this.GetTemplateChild(PartScrollViewer) as InertiaScrollViewer;

            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += this.OnViewerScrollChanged;
            }

            // A selection made before the template existed still needs to be brought into view.
            if (_pendingScrollIndex >= 0)
            {
                this.ScrollToRealIndex(_pendingScrollIndex, false);
            }
        }

        private void OnSelectorLoaded(object sender, RoutedEventArgs e)
        {
            // The popup realizes the wheel with a zero viewport, so the centring done at selection time lands on
            // nothing. Redo it now that the wheel has a size.
            int index = this.SelectedRealIndex;

            if (index >= 0)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => this.ScrollToRealIndex(index, false)));
            }
        }

        private void OnSelectorUnloaded(object sender, RoutedEventArgs e)
        {
            _snapTimer.Stop();
        }

        #endregion

        #region Property Change Handling

        private static object CoerceItemHeight(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;

            return double.IsNaN(value) || double.IsInfinity(value) || value < 1.0 ? 1.0 : value;
        }

        private static object CoerceVisibleItemCount(DependencyObject d, object baseValue)
        {
            int value = Math.Max(3, (int)baseValue);

            // An even count cannot have a single centred entry with symmetric neighbours.
            return value % 2 == 0 ? value + 1 : value;
        }

        private static void OnMetricsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DateSpinnerSelector)d;

            selector.ViewportHeight = selector.ItemHeight * selector.VisibleItemCount;
            selector.RebuildEntries();
        }

        private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DateSpinnerSelector)d).RebuildEntries();
        }

        private static void OnSelectedNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = (DateSpinnerSelector)d;

            if (selector._isSynchronizing)
            {
                return;
            }

            selector.ApplySelectedNumber((int?)e.NewValue, false);
            selector.ValueChanged?.Invoke(selector, EventArgs.Empty);
        }

        #endregion

        #region Entry Management

        /// <summary>
        /// Rebuilds the backing collection as spacers, values, spacers. The current selection is restored by value
        /// where the new collection still offers it.
        /// </summary>
        private void RebuildEntries()
        {
            int? previous = this.SelectedNumber;

            _isSynchronizing = true;

            try
            {
                _entries.Clear();

                int spacers = this.SpacerCount;

                for (int i = 0; i < spacers; i++)
                {
                    _entries.Add(DateSpinnerItem.CreateSpacer());
                }

                var values = this.Values;

                if (values != null)
                {
                    foreach (var value in values)
                    {
                        _entries.Add(value);
                    }
                }

                for (int i = 0; i < spacers; i++)
                {
                    _entries.Add(DateSpinnerItem.CreateSpacer());
                }
            }
            finally
            {
                _isSynchronizing = false;
            }

            this.ApplySelectedNumber(previous, false);
        }

        /// <summary>
        /// Moves the selection onto the entry carrying <paramref name="value"/>, clearing it when the wheel does
        /// not offer that value.
        /// </summary>
        /// <param name="value">The value to select, or <see langword="null"/> to clear the selection.</param>
        /// <param name="animate">Whether to ease into the new position.</param>
        private void ApplySelectedNumber(int? value, bool animate)
        {
            int index = -1;

            if (value.HasValue)
            {
                var values = this.Values;

                if (values != null)
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (values[i].Value == value.Value)
                        {
                            index = i;
                            break;
                        }
                    }
                }
            }

            _isSynchronizing = true;

            try
            {
                this.SelectedIndex = index < 0 ? -1 : index + this.SpacerCount;
            }
            finally
            {
                _isSynchronizing = false;
            }

            if (index >= 0)
            {
                this.ScrollToRealIndex(index, animate);
            }
        }

        #endregion

        #region Selection

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_isSynchronizing)
            {
                return;
            }

            int index = this.SelectedRealIndex;
            var values = this.Values;

            if (index < 0 || values == null || index >= values.Count)
            {
                this.SetSelectedNumberQuietly(null);
                this.ValueChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            this.ScrollToRealIndex(index, true);
            this.SetSelectedNumberQuietly(values[index].Value);
            this.ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Writes <see cref="SelectedNumber"/> without re-entering <see cref="OnSelectedNumberChanged"/>, which
        /// would otherwise push the selection straight back into the list.
        /// </summary>
        private void SetSelectedNumberQuietly(int? value)
        {
            if (this.SelectedNumber == value)
            {
                return;
            }

            _isSynchronizing = true;

            try
            {
                this.SelectedNumber = value;
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Moves the selection by <paramref name="delta"/> entries, skipping over values that fall outside the
        /// spinner's permitted range and stopping at the ends rather than wrapping.
        /// </summary>
        /// <param name="delta">How many entries to move. Negative moves toward earlier values.</param>
        /// <returns><see langword="true"/> when the selection actually moved.</returns>
        public bool MoveSelection(int delta)
        {
            var values = this.Values;

            if (values == null || values.Count == 0 || delta == 0)
            {
                return false;
            }

            int current = this.SelectedRealIndex;

            // With no selection yet, the first move lands on the nearest selectable value rather than stepping off
            // an imaginary starting point.
            int target = current < 0 ? (delta > 0 ? 0 : values.Count - 1) : Math.Clamp(current + delta, 0, values.Count - 1);

            target = this.NearestSelectable(target, delta >= 0 ? 1 : -1);

            if (target < 0 || target == current)
            {
                return false;
            }

            this.SelectRealIndex(target);

            return true;
        }

        /// <summary>
        /// Selects the first or last value the spinner permits.
        /// </summary>
        /// <param name="last"><see langword="true"/> for the last value, otherwise the first.</param>
        /// <returns><see langword="true"/> when the selection actually moved.</returns>
        public bool MoveToEnd(bool last)
        {
            var values = this.Values;

            if (values == null || values.Count == 0)
            {
                return false;
            }

            int target = this.NearestSelectable(last ? values.Count - 1 : 0, last ? -1 : 1);

            if (target < 0 || target == this.SelectedRealIndex)
            {
                return false;
            }

            this.SelectRealIndex(target);

            return true;
        }

        /// <summary>
        /// Walks outward from <paramref name="index"/> looking for a selectable value, preferring
        /// <paramref name="direction"/> before falling back to the other side.
        /// </summary>
        /// <returns>The index of a selectable value, or -1 when the wheel offers none.</returns>
        private int NearestSelectable(int index, int direction)
        {
            var values = this.Values;

            if (values == null || values.Count == 0)
            {
                return -1;
            }

            index = Math.Clamp(index, 0, values.Count - 1);

            if (values[index].IsSelectable)
            {
                return index;
            }

            for (int distance = 1; distance < values.Count; distance++)
            {
                int first = index + distance * direction;
                int second = index - distance * direction;

                if (first >= 0 && first < values.Count && values[first].IsSelectable)
                {
                    return first;
                }

                if (second >= 0 && second < values.Count && values[second].IsSelectable)
                {
                    return second;
                }
            }

            return -1;
        }

        /// <summary>
        /// Selects the value at <paramref name="index"/> in <see cref="Values"/>.
        /// </summary>
        private void SelectRealIndex(int index)
        {
            this.SelectedIndex = index + this.SpacerCount;
        }

        #endregion

        #region Scrolling

        /// <summary>
        /// Centres the value at <paramref name="index"/> in the selection band. With half a viewport of spacers at
        /// the head of the list, the offset that centres real index <c>i</c> is exactly <c>i * ItemHeight</c>.
        /// </summary>
        private void ScrollToRealIndex(int index, bool animate)
        {
            if (_scrollViewer == null)
            {
                _pendingScrollIndex = index;
                return;
            }

            _pendingScrollIndex = -1;

            double target = index * this.ItemHeight;

            if (Math.Abs(_scrollViewer.VerticalOffset - target) < 0.5)
            {
                return;
            }

            if (animate && this.IsScrollAnimationEnabled)
            {
                _scrollViewer.AnimationDurationMilliseconds = this.ScrollAnimationDuration;
                _scrollViewer.AnimateScroll(target);
            }
            else
            {
                _scrollViewer.BeginAnimation(InertiaScrollViewerBehavior.VerticalOffsetProperty, null);
                _scrollViewer.ScrollToVerticalOffset(target);
            }
        }

        private void OnViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange == 0)
            {
                return;
            }

            // Free scrolling (touch inertia, a scroll bar drag) can leave the wheel between two values. Wait for it
            // to go quiet, then snap. A snap that is already correct is a no-op, so this terminates.
            _snapTimer.Stop();
            _snapTimer.Start();
        }

        private void OnSnapTimerTick(object? sender, EventArgs e)
        {
            _snapTimer.Stop();

            if (_scrollViewer == null)
            {
                return;
            }

            var values = this.Values;

            if (values == null || values.Count == 0)
            {
                return;
            }

            int nearest = (int)Math.Round(_scrollViewer.VerticalOffset / this.ItemHeight, MidpointRounding.AwayFromZero);
            nearest = this.NearestSelectable(Math.Clamp(nearest, 0, values.Count - 1), 1);

            if (nearest < 0)
            {
                return;
            }

            if (nearest == this.SelectedRealIndex)
            {
                // Already on the right value, just settle the offset if the scroll stopped part way.
                this.ScrollToRealIndex(nearest, true);
                return;
            }

            this.SelectRealIndex(nearest);
        }

        #endregion

        #region Input

        /// <inheritdoc />
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);

            if (e.Handled || !this.IsEnabled)
            {
                return;
            }

            int lines = Math.Max(1, Math.Abs(e.Delta) / Mouse.MouseWheelDeltaForOneLine);
            bool moved = this.MoveSelection(e.Delta > 0 ? -lines : lines);

            // Only swallow the wheel when the wheel actually consumed it. At either end of the range the event is
            // left alone so an enclosing scroll viewer can keep scrolling the page.
            e.Handled = moved;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled && this.IsEnabled)
            {
                bool handled = true;

                switch (e.Key)
                {
                    case Key.Up:
                        this.MoveSelection(-1);
                        break;
                    case Key.Down:
                        this.MoveSelection(1);
                        break;
                    case Key.PageUp:
                        this.MoveSelection(-this.VisibleItemCount);
                        break;
                    case Key.PageDown:
                        this.MoveSelection(this.VisibleItemCount);
                        break;
                    case Key.Home:
                        this.MoveToEnd(false);
                        break;
                    case Key.End:
                        this.MoveToEnd(true);
                        break;
                    default:
                        handled = false;
                        break;
                }

                if (handled)
                {
                    // Swallowed before the base class so that its focus-driven navigation, which would try to move
                    // focus onto an individual entry, never runs.
                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        #endregion

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new DateSpinnerSelectorItem();
        }

        /// <inheritdoc />
        /// <remarks>
        /// The height is stamped onto the container here rather than bound from the item style. A container is
        /// prepared before it is connected to the visual tree, so a RelativeSource binding back to the wheel has
        /// nothing to resolve against and the container would fall back to sizing to its content. Uniform heights
        /// are what make the wheel's scroll arithmetic exact, so they cannot be left to a binding that might not
        /// resolve.
        /// </remarks>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            if (element is FrameworkElement container)
            {
                container.Height = this.ItemHeight;
            }
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is DateSpinnerSelectorItem;
        }
    }

    /// <summary>
    /// The container generated for each entry in a <see cref="DateSpinnerSelector"/>. It exists so the wheel's
    /// entries can be styled independently of ordinary <see cref="ListBoxItem"/> instances.
    /// </summary>
    public class DateSpinnerSelectorItem : ListBoxItem
    {
        /// <summary>
        /// Initializes static metadata for the <see cref="DateSpinnerSelectorItem"/> class.
        /// </summary>
        static DateSpinnerSelectorItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateSpinnerSelectorItem), new FrameworkPropertyMetadata(typeof(DateSpinnerSelectorItem)));
        }
    }
}
