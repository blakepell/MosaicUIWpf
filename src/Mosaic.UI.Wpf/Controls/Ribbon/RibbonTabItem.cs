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
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// An ordered Classic group transition for a tab: reduce the group identified by <see cref="GroupKey"/>
    /// to <see cref="Variant"/>. Consecutive rules sharing a non-empty <see cref="SetName"/> apply atomically.
    /// </summary>
    public class RibbonGroupVariant
    {
        /// <summary>Gets or sets the <see cref="RibbonGroup.Key"/> (or header text) of the target group.</summary>
        public string? GroupKey { get; set; }

        /// <summary>Gets or sets the variant to reduce the group to.</summary>
        public VariantSize Variant { get; set; } = VariantSize.Medium;

        /// <summary>Gets or sets the name of the atomic transition set this rule belongs to.</summary>
        public string? SetName { get; set; }
    }

    /// <summary>
    /// An ordered Simplified transition for a tab: reduce the command identified by <see cref="ItemKey"/> to
    /// <see cref="Variant"/>; <see cref="VariantSize.Collapsed"/> moves it to overflow.
    /// </summary>
    public class RibbonControlVariant
    {
        /// <summary>Gets or sets the <see cref="RibbonItem.KeyProperty"/> of the target command.</summary>
        public string? ItemKey { get; set; }

        /// <summary>Gets or sets the variant to reduce the command to.</summary>
        public VariantSize Variant { get; set; } = VariantSize.Small;
    }

    /// <summary>
    /// Associates contextual tabs (for example Picture Format) with an activation state. Tabs reference a
    /// group through <see cref="RibbonTabItem.ContextualTabGroupKey"/>; they are shown only while
    /// <see cref="IsActive"/> is true.
    /// </summary>
    public class RibbonContextualTabGroup : Freezable
    {
        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            nameof(Key), typeof(string), typeof(RibbonContextualTabGroup), new PropertyMetadata(null));

        /// <summary>Gets or sets the identity tabs reference.</summary>
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(string), typeof(RibbonContextualTabGroup), new PropertyMetadata(null));

        /// <summary>Gets or sets the context name exposed to automation and tooltips (for example "Picture Tools").</summary>
        public string? Header { get => (string?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

        /// <summary>Identifies the <see cref="Brush"/> dependency property.</summary>
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            nameof(Brush), typeof(Brush), typeof(RibbonContextualTabGroup), new PropertyMetadata(null));

        /// <summary>Gets or sets the accent used for the group's tab headers. When unset the theme accent is used.</summary>
        public Brush? Brush { get => (Brush?)GetValue(BrushProperty); set => SetValue(BrushProperty, value); }

        /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive), typeof(bool), typeof(RibbonContextualTabGroup), new PropertyMetadata(false));

        /// <summary>Gets or sets a value indicating whether the context is active and its tabs are shown.</summary>
        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

        /// <inheritdoc />
        protected override Freezable CreateInstanceCore() => new RibbonContextualTabGroup();
    }

    /// <summary>
    /// A task-oriented Ribbon tab containing <see cref="RibbonGroup"/>s. Its header is presented in the
    /// Ribbon's tab strip; its groups are laid out by <see cref="RibbonGroupsPanel"/>.
    /// </summary>
    [TemplatePart(Name = PartScrollLeft, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartScrollRight, Type = typeof(ButtonBase))]
    [DefaultProperty(nameof(Header))]
    public class RibbonTabItem : HeaderedItemsControl
    {
        private const string PartScrollLeft = "PART_ScrollLeft";
        private const string PartScrollRight = "PART_ScrollRight";
        private const double ScrollStep = 160;

        private ButtonBase? _scrollLeft;
        private ButtonBase? _scrollRight;
        private readonly ObservableCollection<FrameworkElement> _overflowItems = new();
        private readonly Dictionary<UIElement, FrameworkElement> _presentations = new();

        /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
        public static readonly DependencyProperty IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(typeof(RibbonTabItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal, OnIsSelectedChanged));

        /// <summary>Gets or sets a value indicating whether the tab is selected.</summary>
        [Category("Appearance")]
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonTabItem));

        /// <summary>
        /// Gets or sets the tab identity used by <see cref="Ribbon.FindTab"/> and by persisted preferences. When unset,
        /// the header text is used.
        /// </summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Identifies the <see cref="ContextualTabGroupKey"/> dependency property.</summary>
        public static readonly DependencyProperty ContextualTabGroupKeyProperty = DependencyProperty.Register(
            nameof(ContextualTabGroupKey), typeof(string), typeof(RibbonTabItem), new FrameworkPropertyMetadata(null, OnHeaderStateChanged));

        /// <summary>Gets or sets the <see cref="RibbonContextualTabGroup.Key"/> this tab belongs to; <c>null</c> for ordinary tabs.</summary>
        [Category("Ribbon")]
        public string? ContextualTabGroupKey { get => (string?)GetValue(ContextualTabGroupKeyProperty); set => SetValue(ContextualTabGroupKeyProperty, value); }

        /// <summary>Gets the ordered Classic group transitions. Empty means right-to-left default reduction.</summary>
        public Collection<RibbonGroupVariant> GroupVariants { get; } = new();

        /// <summary>Gets the ordered Simplified command transitions applied before the default reduction.</summary>
        public Collection<RibbonControlVariant> ControlVariants { get; } = new();

        /// <summary>Gets the generated presentations of commands in the tab overflow menu (Simplified mode).</summary>
        public ReadOnlyObservableCollection<FrameworkElement> OverflowItems { get; }

        private static readonly DependencyPropertyKey HasOverflowItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasOverflowItems), typeof(bool), typeof(RibbonTabItem), new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="HasOverflowItems"/> dependency property.</summary>
        public static readonly DependencyProperty HasOverflowItemsProperty = HasOverflowItemsPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether any command is in the tab overflow menu.</summary>
        [Browsable(false)]
        public bool HasOverflowItems => (bool)GetValue(HasOverflowItemsProperty);

        /// <summary>Identifies the <see cref="IsOverflowOpen"/> dependency property.</summary>
        public static readonly DependencyProperty IsOverflowOpenProperty = DependencyProperty.Register(
            nameof(IsOverflowOpen), typeof(bool), typeof(RibbonTabItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>Gets or sets a value indicating whether the tab overflow menu is open.</summary>
        [Browsable(false)]
        public bool IsOverflowOpen { get => (bool)GetValue(IsOverflowOpenProperty); set => SetValue(IsOverflowOpenProperty, value); }

        private static readonly DependencyPropertyKey CanScrollLeftPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(CanScrollLeft), typeof(bool), typeof(RibbonTabItem), new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="CanScrollLeft"/> dependency property.</summary>
        public static readonly DependencyProperty CanScrollLeftProperty = CanScrollLeftPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether groups are scrolled out of view to the left.</summary>
        [Browsable(false)]
        public bool CanScrollLeft => (bool)GetValue(CanScrollLeftProperty);

        private static readonly DependencyPropertyKey CanScrollRightPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(CanScrollRight), typeof(bool), typeof(RibbonTabItem), new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="CanScrollRight"/> dependency property.</summary>
        public static readonly DependencyProperty CanScrollRightProperty = CanScrollRightPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether groups extend beyond the right edge.</summary>
        [Browsable(false)]
        public bool CanScrollRight => (bool)GetValue(CanScrollRightProperty);

        /// <summary>Gets the owning Ribbon.</summary>
        public Ribbon? Ribbon => ItemsControlFromItemContainer(this) as Ribbon;

        /// <summary>Gets the identity used by <see cref="Ribbon.FindTab"/> and persistence: <see cref="Key"/> or the header text.</summary>
        internal string? LayoutKey => Key ?? Header as string;

        /// <summary>Gets the generated header presented in the tab strip.</summary>
        internal RibbonTabHeader? TabHeader { get; set; }

        internal RibbonGroupsPanel? GroupsPanel { get; set; }

        static RibbonTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(typeof(RibbonTabItem)));
            FocusableProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(false));
            HeaderProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(null, OnHeaderStateChanged));
            VisibilityProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(Visibility.Visible, OnHeaderStateChanged));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.Continue));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonTabItem"/> class.
        /// </summary>
        public RibbonTabItem()
        {
            OverflowItems = new ReadOnlyObservableCollection<FrameworkElement>(_overflowItems);
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new RibbonGroup();

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_scrollLeft != null)
            {
                _scrollLeft.Click -= OnScrollLeft;
            }

            if (_scrollRight != null)
            {
                _scrollRight.Click -= OnScrollRight;
            }

            base.OnApplyTemplate();

            _scrollLeft = GetTemplateChild(PartScrollLeft) as ButtonBase;
            _scrollRight = GetTemplateChild(PartScrollRight) as ButtonBase;

            if (_scrollLeft != null)
            {
                _scrollLeft.Click += OnScrollLeft;
            }

            if (_scrollRight != null)
            {
                _scrollRight.Click += OnScrollRight;
            }
        }

        private void OnScrollLeft(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            GroupsPanel?.ScrollBy(-ScrollStep);
        }

        private void OnScrollRight(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            GroupsPanel?.ScrollBy(ScrollStep);
        }

        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (e.OriginalSource is UIElement element && element != this)
            {
                GroupsPanel?.BringIntoView(element);
            }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tab = (RibbonTabItem)d;
            tab.RaiseEvent(new RoutedEventArgs((bool)e.NewValue ? Selector.SelectedEvent : Selector.UnselectedEvent, tab));
            if (!(bool)e.NewValue)
            {
                tab.SetCurrentValue(IsOverflowOpenProperty, false);
            }

            tab.TabHeader?.SetCurrentValue(RibbonTabHeader.IsSelectedProperty, e.NewValue);
        }

        private static void OnHeaderStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonTabItem)d).Ribbon?.UpdateTabHeaders();
        }

        internal void SetScrollState(bool left, bool right)
        {
            if (CanScrollLeft != left)
            {
                SetValue(CanScrollLeftPropertyKey, left);
            }

            if (CanScrollRight != right)
            {
                SetValue(CanScrollRightPropertyKey, right);
            }
        }

        /// <summary>
        /// Closes the tab's transient surfaces (overflow menu and collapsed-group popups).
        /// </summary>
        internal void CloseTransientSurfaces()
        {
            SetCurrentValue(IsOverflowOpenProperty, false);
            foreach (var item in Items)
            {
                if (item is RibbonGroup g)
                {
                    g.SetCurrentValue(RibbonGroup.IsDropDownOpenProperty, false);
                    g.SetCurrentValue(RibbonGroup.IsOverflowOpenProperty, false);
                }
            }
        }

        /// <summary>
        /// Synchronizes the generated overflow presentations with the layout engine's decision. Presentations
        /// are cached per source so state and handlers survive relayout; stale ones are released.
        /// </summary>
        internal void UpdateOverflow(List<UIElement> tabItems, Dictionary<RibbonGroup, List<UIElement>> groupItems)
        {
            var desiredTab = tabItems.Select(GetPresentation).OfType<FrameworkElement>().ToList();
            var desiredGroups = groupItems.ToDictionary(kv => kv.Key, kv => kv.Value.Select(GetPresentation).OfType<FrameworkElement>().ToList());

            // Removal pass first across every destination, so a presentation moving between the tab and a
            // group is detached before it is attached elsewhere.
            RemoveExtras(_overflowItems, desiredTab);
            foreach (var item in Items)
            {
                if (item is RibbonGroup g)
                {
                    RemoveExtras(g.MutableOverflowItems, desiredGroups.TryGetValue(g, out var list) ? list : new List<FrameworkElement>());
                }
            }

            SyncOrder(_overflowItems, desiredTab);
            foreach (var (g, list) in desiredGroups)
            {
                SyncOrder(g.MutableOverflowItems, list);
            }

            // Release presentations of commands that are back inline.
            var live = new HashSet<UIElement>(tabItems.Concat(groupItems.Values.SelectMany(v => v)));
            foreach (var stale in _presentations.Keys.Where(k => !live.Contains(k)).ToList())
            {
                BindingOperations.ClearAllBindings(_presentations[stale]);
                _presentations.Remove(stale);
            }

            SetValue(HasOverflowItemsPropertyKey, _overflowItems.Count > 0);
            if (_overflowItems.Count == 0)
            {
                SetCurrentValue(IsOverflowOpenProperty, false);
            }
        }

        private FrameworkElement? GetPresentation(UIElement source)
        {
            if (_presentations.TryGetValue(source, out var existing))
            {
                return existing;
            }

            if (source is IRibbonControl rc && rc.CreatePresentation(RibbonPresentationKind.Overflow) is { } created)
            {
                _presentations[source] = created;
                return created;
            }

            return null;
        }

        private static void RemoveExtras(ObservableCollection<FrameworkElement> target, List<FrameworkElement> desired)
        {
            for (var i = target.Count - 1; i >= 0; i--)
            {
                if (!desired.Contains(target[i]))
                {
                    target.RemoveAt(i);
                }
            }
        }

        private static void SyncOrder(ObservableCollection<FrameworkElement> target, List<FrameworkElement> desired)
        {
            for (var i = 0; i < desired.Count; i++)
            {
                var index = target.IndexOf(desired[i]);
                if (index == i)
                {
                    continue;
                }

                if (index >= 0)
                {
                    target.Move(index, i);
                }
                else
                {
                    target.Insert(i, desired[i]);
                }
            }

            while (target.Count > desired.Count)
            {
                target.RemoveAt(target.Count - 1);
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabItemAutomationPeer(this);
    }

    /// <summary>
    /// The header of a <see cref="RibbonTabItem"/> in the tab strip. Generated by the <see cref="Ribbon"/>.
    /// </summary>
    /// <remarks>
    /// When the selection changes, the selection indicator (<c>PART_Indicator</c>) plays the animation chosen by
    /// <see cref="Ribbon.TabIndicatorAnimation"/>. The animation is skipped when Windows client-area animations are
    /// turned off.
    /// </remarks>
    [TemplatePart(Name = PartIndicator, Type = typeof(FrameworkElement))]
    public class RibbonTabHeader : ContentControl, IRibbonKeyTipTarget
    {
        private const string PartIndicator = "PART_Indicator";

        /// <summary>Duration of the indicator travel between tabs.</summary>
        private static readonly Duration IndicatorTravelDuration = new(TimeSpan.FromMilliseconds(360));

        /// <summary>Duration of the indicator growing in place when there is no previous tab to travel from.</summary>
        private static readonly Duration IndicatorGrowDuration = new(TimeSpan.FromMilliseconds(220));

        /// <summary>Duration of the expand-and-snap-back animation.</summary>
        private static readonly Duration IndicatorExpandDuration = new(TimeSpan.FromMilliseconds(380));

        private static readonly DependencyProperty IndicatorProgressProperty = DependencyProperty.Register(
            "IndicatorProgress", typeof(double), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(1.0, OnIndicatorProgressChanged));

        private FrameworkElement? _indicator;
        private Thickness _indicatorRestMargin;
        private double _fromLeft;
        private double _fromRight;
        private double _toWidth;
        private bool _isTravel;
        private bool _isExpand;

        /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(false, OnIsSelectedChanged));

        /// <summary>Gets a value indicating whether the header's tab is selected.</summary>
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

        /// <summary>Identifies the <see cref="ContextualBrush"/> dependency property.</summary>
        public static readonly DependencyProperty ContextualBrushProperty = DependencyProperty.Register(
            nameof(ContextualBrush), typeof(Brush), typeof(RibbonTabHeader),
            new FrameworkPropertyMetadata(null, (d, e) => d.SetValue(HasContextualBrushPropertyKey, e.NewValue != null)));

        /// <summary>Gets the accent of the header's contextual tab group, or <c>null</c> for ordinary tabs.</summary>
        public Brush? ContextualBrush { get => (Brush?)GetValue(ContextualBrushProperty); internal set => SetValue(ContextualBrushProperty, value); }

        private static readonly DependencyPropertyKey HasContextualBrushPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasContextualBrush), typeof(bool), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="HasContextualBrush"/> dependency property.</summary>
        public static readonly DependencyProperty HasContextualBrushProperty = HasContextualBrushPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether a contextual accent is assigned (otherwise the theme accent is used).</summary>
        public bool HasContextualBrush => (bool)GetValue(HasContextualBrushProperty);

        /// <summary>Identifies the <see cref="IsContextual"/> dependency property.</summary>
        public static readonly DependencyProperty IsContextualProperty = DependencyProperty.Register(
            nameof(IsContextual), typeof(bool), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(false));

        /// <summary>Gets a value indicating whether the header belongs to a contextual tab.</summary>
        public bool IsContextual { get => (bool)GetValue(IsContextualProperty); internal set => SetValue(IsContextualProperty, value); }

        /// <summary>Identifies the <see cref="IsFirstContextual"/> dependency property.</summary>
        public static readonly DependencyProperty IsFirstContextualProperty = DependencyProperty.Register(
            nameof(IsFirstContextual), typeof(bool), typeof(RibbonTabHeader), new FrameworkPropertyMetadata(false));

        /// <summary>Gets a value indicating whether this is the first header of a contextual group (draws a divider).</summary>
        public bool IsFirstContextual { get => (bool)GetValue(IsFirstContextualProperty); internal set => SetValue(IsFirstContextualProperty, value); }

        /// <summary>Gets the tab this header represents.</summary>
        public RibbonTabItem Tab { get; }

        static RibbonTabHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTabHeader), new FrameworkPropertyMetadata(typeof(RibbonTabHeader)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonTabHeader"/> class.
        /// </summary>
        /// <param name="tab">The tab the header represents.</param>
        public RibbonTabHeader(RibbonTabItem tab)
        {
            Tab = tab;
            tab.TabHeader = this;
            SetBinding(ContentProperty, new Binding { Source = tab, Path = new PropertyPath(HeaderedItemsControl.HeaderProperty) });
            SetBinding(ContentTemplateProperty, new Binding { Source = tab, Path = new PropertyPath(HeaderedItemsControl.HeaderTemplateProperty) });
            SetBinding(RibbonKeyTipService.KeyTipProperty, new Binding { Source = tab, Path = new PropertyPath(RibbonKeyTipService.KeyTipProperty) });
            IsSelected = tab.IsSelected;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            ResetIndicator();

            base.OnApplyTemplate();

            _indicator = GetTemplateChild(PartIndicator) as FrameworkElement;
            _indicatorRestMargin = _indicator?.Margin ?? default;
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var header = (RibbonTabHeader)d;
            if (!(bool)e.NewValue)
            {
                header.ResetIndicator();
                return;
            }

            if (header.Tab.Ribbon is not { } ribbon)
            {
                return;
            }

            var previous = ribbon.IndicatorHeader;
            ribbon.IndicatorHeader = header;
            if (!ReferenceEquals(previous, header))
            {
                header.AnimateIndicator(previous);
            }
        }

        /// <summary>
        /// Animates this header's indicator into place, traveling from <paramref name="previous"/>'s indicator
        /// when both are laid out in the same tab strip, otherwise growing from the center.
        /// </summary>
        private void AnimateIndicator(RibbonTabHeader? previous)
        {
            ResetIndicator();

            var mode = Tab.Ribbon?.TabIndicatorAnimation ?? RibbonTabIndicatorAnimation.Expand;

            // No animation for the initial layout, hidden strips, or when animations are turned off.
            if (mode == RibbonTabIndicatorAnimation.None || _indicator is not { ActualWidth: > 0 } indicator || !IsLoaded || !IsVisible
                || !SystemParameters.ClientAreaAnimation || RenderCapability.Tier == 0)
            {
                previous?.ResetIndicator();
                return;
            }

            _toWidth = indicator.ActualWidth;
            _isTravel = false;
            _isExpand = mode == RibbonTabIndicatorAnimation.Expand;

            if (_isExpand)
            {
                // Expand starts on the new tab immediately; the previous indicator simply disappears.
                previous?.ResetIndicator();
            }
            else if (previous is { _indicator: { ActualWidth: > 0 } from, IsLoaded: true, IsVisible: true })
            {
                try
                {
                    // Where the previous indicator is drawn now (mid-flight if it was still animating), in this
                    // indicator's coordinates.
                    var origin = from.TransformToVisual(indicator).Transform(new Point(0, 0));
                    _fromLeft = origin.X;
                    _fromRight = origin.X + from.ActualWidth;
                    _isTravel = Math.Abs(origin.Y) < 1;
                }
                catch (InvalidOperationException)
                {
                    // Not in the same visual tree (for example, the previous header was just removed).
                }

                previous.ResetIndicator();
            }

            if (!_isTravel && !_isExpand)
            {
                _fromLeft = _fromRight = _toWidth / 2;
            }

            var duration = _isExpand ? IndicatorExpandDuration : _isTravel ? IndicatorTravelDuration : IndicatorGrowDuration;

            // The base value stays 1 (at rest), so when the animation ends and is removed the indicator rests in place.
            BeginAnimation(IndicatorProgressProperty, new DoubleAnimation(0, 1, duration)
            {
                FillBehavior = FillBehavior.Stop
            });
        }

        private void ResetIndicator()
        {
            BeginAnimation(IndicatorProgressProperty, null);
            SetValue(IndicatorProgressProperty, 1.0);
        }

        private static void OnIndicatorProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonTabHeader)d).ApplyIndicatorProgress((double)e.NewValue);
        }

        private void ApplyIndicatorProgress(double t)
        {
            if (_indicator == null)
            {
                return;
            }

            if (t >= 1 || _toWidth <= 0)
            {
                _indicator.ClearValue(MarginProperty);
                return;
            }

            double left, right;
            if (_isExpand)
            {
                // Expand outward in both directions, then snap back to the tab's width with a quick settle.
                var reach = Math.Max(10, _toWidth * 0.45);
                var amount = t < 0.32
                    ? EaseOutCubic(t / 0.32)
                    : 1 - EaseOutQuint((t - 0.32) / 0.68);
                left = -reach * amount;
                right = _toWidth + (reach * amount);
            }
            else if (_isTravel)
            {
                // The leading edge races ahead (stretching the indicator across the gap); the trailing edge starts a
                // beat later and catches up, snapping the indicator to the new tab's width.
                var lead = EaseOutCubic(Math.Clamp(t / 0.62, 0, 1));
                var trail = EaseInOutCubic(Math.Clamp((t - 0.18) / 0.82, 0, 1));
                var movingRight = _fromLeft < 0;
                left = Lerp(_fromLeft, 0, movingRight ? trail : lead);
                right = Lerp(_fromRight, _toWidth, movingRight ? lead : trail);
            }
            else
            {
                var grow = EaseOutCubic(t);
                left = Lerp(_fromLeft, 0, grow);
                right = Lerp(_fromRight, _toWidth, grow);
            }

            // Margins (not a scale transform) move the edges so the rounded ends keep their shape while stretched;
            // negative margins let the indicator extend past the header without being clipped.
            right = Math.Max(right, left + 0.5);
            _indicator.Margin = new Thickness(
                _indicatorRestMargin.Left + left,
                _indicatorRestMargin.Top,
                _indicatorRestMargin.Right + (_toWidth - right),
                _indicatorRestMargin.Bottom);
        }

        private static double Lerp(double from, double to, double t) => from + ((to - from) * t);

        private static double EaseOutCubic(double t) => 1 - Math.Pow(1 - t, 3);

        private static double EaseOutQuint(double t) => 1 - Math.Pow(1 - t, 5);

        private static double EaseInOutCubic(double t) => t < 0.5 ? 4 * t * t * t : 1 - (Math.Pow((-2 * t) + 2, 3) / 2);

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            var ribbon = Tab.Ribbon;
            if (ribbon == null)
            {
                return;
            }

            if (e.ClickCount == 2 && ribbon.IsMinimizable)
            {
                ribbon.SetCurrentValue(Ribbon.IsMinimizedProperty, !ribbon.IsMinimized);
                ribbon.SelectTab(Tab, openWhenMinimized: false);
            }
            else
            {
                ribbon.OnTabHeaderClicked(Tab);
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Space)
            {
                Tab.Ribbon?.OnTabHeaderClicked(Tab);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Down && Tab.Ribbon is { } ribbon)
            {
                // Down moves from the tab strip into the selected tab's commands.
                ribbon.SelectTab(Tab, openWhenMinimized: true);
                ribbon.FocusSelectedTabContent();
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);

            // Selection follows keyboard focus along the tab strip (not while minimized, where selecting
            // would open a popup on every arrow press).
            if (InputManager.Current.MostRecentInputDevice is KeyboardDevice && Tab.Ribbon is { IsMinimized: false } ribbon)
            {
                ribbon.SelectTab(Tab, openWhenMinimized: false);
            }
        }

        /// <inheritdoc />
        public bool IsKeyTipTarget => IsVisible;

        /// <inheritdoc />
        public string? KeyTipLabel => Tab.Header as string;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            var ribbon = Tab.Ribbon;
            if (ribbon == null)
            {
                return null;
            }

            ribbon.SelectTab(Tab, openWhenMinimized: true);
            return ribbon.SelectedTabKeyTipScope;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabHeaderAutomationPeer(this);
    }
}
