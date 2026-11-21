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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Layout helpers shared by the Ribbon panels.
    /// </summary>
    internal static class RibbonLayoutHelper
    {
        /// <summary>
        /// Measures an element after invalidating its whole subtree. A variant change re-triggers template
        /// setters deep inside a control; measuring it directly from a parent's measure pass (before the layout
        /// queue reaches those parts) would otherwise reuse cached sizes of the unchanged intermediate parts and
        /// return a stale size.
        /// </summary>
        public static void MeasureFresh(UIElement element, Size constraint)
        {
            InvalidateMeasureDeep(element);
            element.Measure(constraint);
        }

        private static void InvalidateMeasureDeep(DependencyObject d)
        {
            if (d is UIElement element)
            {
                element.InvalidateMeasure();
            }

            var count = VisualTreeHelper.GetChildrenCount(d);
            for (var i = 0; i < count; i++)
            {
                InvalidateMeasureDeep(VisualTreeHelper.GetChild(d, i));
            }
        }
    }

    /// <summary>
    /// Lays out the commands of one <see cref="RibbonGroup"/>. In Classic mode large commands take a full
    /// column and other commands stack up to three per column; in Simplified mode commands form one row and
    /// overflowed commands are skipped. Used as the group's items panel.
    /// </summary>
    public class RibbonGroupItemsPanel : Panel
    {
        private const double ColumnGap = 2;
        private const double SimplifiedGap = 2;

        private readonly List<Column> _columns = new();

        private sealed class Column
        {
            public readonly List<UIElement> Items = new();
            public double Width;
            public bool IsFullHeight;
        }

        /// <summary>
        /// Gets the group that owns this panel.
        /// </summary>
        protected RibbonGroup? Owner => ItemsControl.GetItemsOwner(this) as RibbonGroup;

        /// <inheritdoc />
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (Owner is { } owner)
            {
                owner.ItemsPanelInstance = this;
            }
        }

        internal static bool ParticipatesInLayout(UIElement child)
        {
            // Overflowed commands are Hidden by the layout engine but still part of the layout model.
            return child.Visibility != Visibility.Collapsed || RibbonItem.GetIsOverflowed(child);
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            // The owner link is established here: IsItemsHost is not yet set when the panel is first parented.
            if (Owner is { } owner && owner.ItemsPanelInstance != this)
            {
                owner.ItemsPanelInstance = this;
            }

            var density = Ribbon.GetDensity(this);
            return Ribbon.GetLayoutMode(this) == RibbonLayoutMode.Simplified
                ? MeasureSimplified(density)
                : MeasureClassic(density);
        }

        private Size MeasureClassic(UserInterfaceDensity density)
        {
            _columns.Clear();

            var rowHeight = RibbonMetrics.GetRowHeight(density);
            var height = RibbonMetrics.GetClassicContentHeight(density);
            var groupVariant = Owner?.ContentVariant ?? VariantSize.Large;
            Column? current = null;

            void Flush()
            {
                if (current != null)
                {
                    _columns.Add(current);
                    current = null;
                }
            }

            foreach (UIElement child in InternalChildren)
            {
                if (child == null || !ParticipatesInLayout(child))
                {
                    continue;
                }

                if (child is RibbonSeparator)
                {
                    Flush();
                    child.Measure(new Size(double.PositiveInfinity, height));
                    _columns.Add(new Column { IsFullHeight = true, Width = child.DesiredSize.Width, Items = { child } });
                    continue;
                }

                var variant = RibbonItem.ResolveClassic(child, groupVariant);
                var changed = RibbonItem.GetVariant(child) != variant;
                if (changed)
                {
                    RibbonItem.SetVariant(child, variant);
                }

                var constraint = new Size(double.PositiveInfinity, variant == VariantSize.Large ? height : rowHeight);
                if (changed)
                {
                    RibbonLayoutHelper.MeasureFresh(child, constraint);
                }

                if (variant == VariantSize.Large)
                {
                    Flush();
                    child.Measure(constraint);
                    _columns.Add(new Column { IsFullHeight = true, Width = child.DesiredSize.Width, Items = { child } });
                    continue;
                }

                child.Measure(constraint);
                current ??= new Column();
                current.Items.Add(child);
                current.Width = Math.Max(current.Width, child.DesiredSize.Width);

                if (current.Items.Count == 3)
                {
                    Flush();
                }
            }

            Flush();

            var width = _columns.Sum(c => c.Width) + (Math.Max(0, _columns.Count - 1) * ColumnGap);
            return new Size(width, height);
        }

        private Size MeasureSimplified(UserInterfaceDensity density)
        {
            var itemHeight = RibbonMetrics.GetSimplifiedItemHeight(density);
            double width = 0;
            var count = 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null || !ParticipatesInLayout(child))
                {
                    continue;
                }

                if (child is RibbonSeparator)
                {
                    child.Measure(new Size(0, 0));
                    continue;
                }

                child.Measure(new Size(double.PositiveInfinity, itemHeight));
                if (RibbonItem.GetIsOverflowed(child))
                {
                    continue;
                }

                width += child.DesiredSize.Width;
                count++;
            }

            width += Math.Max(0, count - 1) * SimplifiedGap;
            return new Size(width, itemHeight);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Ribbon.GetLayoutMode(this) == RibbonLayoutMode.Simplified)
            {
                double x = 0;
                foreach (UIElement child in InternalChildren)
                {
                    if (child == null)
                    {
                        continue;
                    }

                    if (child is RibbonSeparator || RibbonItem.GetIsOverflowed(child) || !ParticipatesInLayout(child))
                    {
                        child.Arrange(new Rect(0, 0, 0, 0));
                        continue;
                    }

                    child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                    x += child.DesiredSize.Width + SimplifiedGap;
                }

                return finalSize;
            }

            var density = Ribbon.GetDensity(this);
            var rowHeight = RibbonMetrics.GetRowHeight(density);
            var height = Math.Max(finalSize.Height, RibbonMetrics.GetClassicContentHeight(density));
            double left = 0;

            foreach (var column in _columns)
            {
                if (column.IsFullHeight)
                {
                    column.Items[0].Arrange(new Rect(left, 0, column.Width, height));
                }
                else
                {
                    // Distribute rows evenly over the group height (two rows sit like Office's Font group).
                    var n = column.Items.Count;
                    var spacing = n == 3 ? RibbonMetrics.RowGap : (height - (n * rowHeight)) / (n + 1);
                    var y = n == 3 ? 0 : spacing;

                    foreach (var item in column.Items)
                    {
                        item.Arrange(new Rect(left, y, item.DesiredSize.Width, rowHeight));
                        y += rowHeight + spacing;
                    }
                }

                left += column.Width + ColumnGap;
            }

            return finalSize;
        }
    }

    /// <summary>
    /// The adaptive layout engine for a tab's groups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each pass decides on a width model: starting from the most expanded legal configuration it applies an
    /// ordered, finite list of transitions until the groups fit. The decision is a pure function of the
    /// available width and the measured widths, so layout terminates, cannot oscillate, and expands again in
    /// reverse order as space returns. When required content still cannot fit, the strip scrolls horizontally
    /// rather than clipping commands.
    /// </para>
    /// <para>
    /// Widths of variants other than the current one are probed once and cached; the final configuration is
    /// applied in a single step, so a steady-state pass changes nothing and never re-invalidates layout.
    /// </para>
    /// </remarks>
    public class RibbonGroupsPanel : Panel
    {
        // Simplified chrome around a group's commands: content margin (3 + 3) plus the separator (1 + 4 + 4).
        private const double SimplifiedGroupChrome = 15;
        private const double SimplifiedItemGap = 2;
        private const double GroupOverflowButtonWidth = 22;

        private readonly Dictionary<UIElement, Dictionary<VariantSize, double>> _widths = new();
        private double _offset;
        private double _extent;
        private RibbonLayoutMode? _lastMode;
        private UserInterfaceDensity? _lastDensity;

        /// <summary>
        /// Gets the tab that owns this panel.
        /// </summary>
        protected RibbonTabItem? Owner => ItemsControl.GetItemsOwner(this) as RibbonTabItem;

        /// <summary>
        /// Gets the number of layout transitions applied in the last measure pass (diagnostics).
        /// </summary>
        public int LastTransitionCount { get; private set; }


        /// <inheritdoc />
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            if (Owner is { } owner)
            {
                owner.GroupsPanel = this;
            }
        }

        /// <summary>
        /// Scrolls the group strip horizontally by <paramref name="delta"/> units.
        /// </summary>
        public void ScrollBy(double delta)
        {
            _offset += delta;
            InvalidateArrange();
        }

        /// <summary>
        /// Discards cached widths, for example after content changed in a way that does not change the width
        /// of the currently displayed variant.
        /// </summary>
        public void InvalidateWidthCache()
        {
            _widths.Clear();
            InvalidateMeasure();
        }

        /// <summary>
        /// Scrolls the strip so that <paramref name="element"/> is fully visible.
        /// </summary>
        internal void BringIntoView(UIElement element)
        {
            if (_extent <= ActualWidth || !IsAncestorOf(element))
            {
                return;
            }

            var bounds = element.TransformToAncestor(this).TransformBounds(new Rect(element.RenderSize));
            if (bounds.Left < 0)
            {
                ScrollBy(bounds.Left - 8);
            }
            else if (bounds.Right > ActualWidth)
            {
                ScrollBy(bounds.Right - ActualWidth + 8);
            }
        }

        private List<UIElement> VisibleChildren()
        {
            var list = new List<UIElement>();
            foreach (UIElement child in InternalChildren)
            {
                if (child != null && child.Visibility != Visibility.Collapsed)
                {
                    list.Add(child);
                }
            }

            return list;
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Owner is { } owner && owner.GroupsPanel != this)
            {
                owner.GroupsPanel = this;
            }

            var children = VisibleChildren();
            var groups = children.OfType<RibbonGroup>().ToList();
            var mode = Ribbon.GetLayoutMode(this);
            var density = Ribbon.GetDensity(this);
            var height = availableSize.Height;
            LastTransitionCount = 0;

            if (_lastDensity != density)
            {
                _widths.Clear();
                _lastDensity = density;
            }

            if (_lastMode != mode)
            {
                _widths.Clear();
                if (mode == RibbonLayoutMode.Classic)
                {
                    ClearSimplifiedState();
                }

                _lastMode = mode;
            }

            // Never reparent or resize under an open collapsed-group popup; the popup closes before relayout.
            if (groups.Any(g => g.IsDropDownOpen) || double.IsInfinity(availableSize.Width))
            {
                _extent = MeasureAll(children, height);
                return new Size(Math.Min(_extent, availableSize.Width), DesiredHeight(children));
            }

            PruneWidthCache(groups);
            _extent = mode == RibbonLayoutMode.Classic
                ? MeasureClassic(children, groups, availableSize.Width, height)
                : MeasureSimplified(children, groups, availableSize.Width, height, density);

            return new Size(Math.Min(_extent, availableSize.Width), DesiredHeight(children));
        }

        private void PruneWidthCache(List<RibbonGroup> groups)
        {
            if (_widths.Count == 0)
            {
                return;
            }

            var live = new HashSet<UIElement>(groups);
            foreach (var g in groups)
            {
                live.UnionWith(g.Items.OfType<UIElement>());
            }

            foreach (var stale in _widths.Keys.Where(k => !live.Contains(k)).ToList())
            {
                _widths.Remove(stale);
            }
        }

        private static double DesiredHeight(List<UIElement> children) => children.Count == 0 ? 0 : children.Max(c => c.DesiredSize.Height);

        private static double MeasureAll(List<UIElement> children, double height)
        {
            double total = 0;
            foreach (var child in children)
            {
                child.Measure(new Size(double.PositiveInfinity, height));
                total += child.DesiredSize.Width;
            }

            return total;
        }

        /// <summary>
        /// Records the measured width of an element's current variant. When it differs from the cached value
        /// the element's content changed, so its other cached variants are discarded too.
        /// </summary>
        private void Observe(UIElement element, VariantSize current, double width)
        {
            if (!_widths.TryGetValue(element, out var map))
            {
                _widths[element] = map = new Dictionary<VariantSize, double>();
            }
            else if (map.TryGetValue(current, out var known) && Math.Abs(known - width) > 0.5)
            {
                map.Clear();
            }

            map[current] = width;
        }

        /// <summary>
        /// Returns the width an element has at <paramref name="variant"/>, probing (and restoring) only when
        /// that width is not yet known.
        /// </summary>
        private double Probe(UIElement element, VariantSize variant, Func<VariantSize> get, Action<VariantSize> set, Size constraint)
        {
            if (_widths.TryGetValue(element, out var map) && map.TryGetValue(variant, out var width))
            {
                return width;
            }

            var current = get();
            set(variant);
            MeasureFresh(element, constraint);
            width = element.DesiredSize.Width;
            set(current);
            MeasureFresh(element, constraint);

            if (!_widths.TryGetValue(element, out map))
            {
                _widths[element] = map = new Dictionary<VariantSize, double>();
            }

            map[variant] = width;
            return width;
        }

        private static void MeasureFresh(UIElement element, Size constraint) => RibbonLayoutHelper.MeasureFresh(element, constraint);

        #region Classic

        private double MeasureClassic(List<UIElement> children, List<RibbonGroup> groups, double available, double height)
        {
            var constraint = new Size(double.PositiveInfinity, height);
            double fixedWidth = 0;
            foreach (var child in children)
            {
                if (child is RibbonGroup g)
                {
                    MeasureFresh(g, constraint);
                    Observe(g, g.Variant, g.DesiredSize.Width);
                }
                else
                {
                    child.Measure(constraint);
                    fixedWidth += child.DesiredSize.Width;
                }
            }

            double Width(RibbonGroup g, VariantSize v) => Probe(g, v, () => g.Variant, x => g.Variant = x, constraint);

            var plan = groups.ToDictionary(g => g, _ => VariantSize.Large);
            var total = fixedWidth + groups.Sum(g => Width(g, VariantSize.Large));

            if (total > available)
            {
                foreach (var set in BuildClassicTransitions(groups))
                {
                    var changes = set.Where(t => t.Variant > plan[t.Group] && (t.Variant != VariantSize.Collapsed || t.Group.IsCollapsible)).ToList();
                    if (changes.Count == 0)
                    {
                        continue;
                    }

                    var next = total + changes.Sum(t => Width(t.Group, t.Variant) - Width(t.Group, plan[t.Group]));

                    // A transition that does not save space (for example collapsing an already narrow group) is
                    // skipped, keeping the sequence strictly decreasing and therefore terminating.
                    if (next >= total)
                    {
                        continue;
                    }

                    foreach (var (group, variant) in changes)
                    {
                        plan[group] = variant;
                    }

                    LastTransitionCount++;
                    total = next;
                    if (total <= available)
                    {
                        break;
                    }
                }
            }

            foreach (var (group, variant) in plan)
            {
                if (group.Variant != variant)
                {
                    group.Variant = variant;
                    MeasureFresh(group, constraint);
                }
            }

            return MeasureAll(children, height);
        }

        /// <summary>
        /// Builds the ordered Classic transition sets: the tab's explicit <see cref="RibbonTabItem.GroupVariants"/>
        /// (consecutive entries sharing a <see cref="RibbonGroupVariant.SetName"/> apply atomically) followed by
        /// collapsing groups right to left; without explicit rules, every group is reduced right to left to
        /// medium, then small, then collapsed.
        /// </summary>
        private IEnumerable<List<(RibbonGroup Group, VariantSize Variant)>> BuildClassicTransitions(List<RibbonGroup> groups)
        {
            var rules = Owner?.GroupVariants;
            if (rules is { Count: > 0 })
            {
                List<(RibbonGroup, VariantSize)>? set = null;
                string? setName = null;

                foreach (var rule in rules)
                {
                    var group = groups.FirstOrDefault(g => string.Equals(g.LayoutKey, rule.GroupKey, StringComparison.Ordinal));
                    if (group == null)
                    {
                        continue;
                    }

                    if (set != null && (string.IsNullOrEmpty(rule.SetName) || rule.SetName != setName))
                    {
                        yield return set;
                        set = null;
                    }

                    set ??= new List<(RibbonGroup, VariantSize)>();
                    set.Add((group, rule.Variant));
                    setName = rule.SetName;
                }

                if (set != null)
                {
                    yield return set;
                }
            }
            else
            {
                foreach (var variant in new[] { VariantSize.Medium, VariantSize.Small })
                {
                    for (var i = groups.Count - 1; i >= 0; i--)
                    {
                        yield return new List<(RibbonGroup, VariantSize)> { (groups[i], variant) };
                    }
                }
            }

            for (var i = groups.Count - 1; i >= 0; i--)
            {
                yield return new List<(RibbonGroup, VariantSize)> { (groups[i], VariantSize.Collapsed) };
            }
        }

        #endregion

        #region Simplified

        private sealed class Entry
        {
            public required RibbonGroup Group;
            public required UIElement Item;
            public VariantSize Variant;
            public bool Overflowed;
        }

        private double MeasureSimplified(List<UIElement> children, List<RibbonGroup> groups, double available, double height, UserInterfaceDensity density)
        {
            var itemConstraint = new Size(double.PositiveInfinity, RibbonMetrics.GetSimplifiedItemHeight(density));
            var constraint = new Size(double.PositiveInfinity, height);

            double fixedWidth = 0;
            foreach (var child in children)
            {
                if (child is RibbonGroup g)
                {
                    if (g.Variant != VariantSize.Large)
                    {
                        g.Variant = VariantSize.Large;
                    }
                }
                else
                {
                    child.Measure(constraint);
                    fixedWidth += child.DesiredSize.Width;
                }
            }

            var entries = new List<Entry>();
            foreach (var g in groups)
            {
                foreach (var item in g.Items)
                {
                    if (item is not UIElement element || element is RibbonSeparator || !RibbonGroupItemsPanel.ParticipatesInLayout(element))
                    {
                        continue;
                    }

                    MeasureFresh(element, itemConstraint);
                    Observe(element, RibbonItem.GetVariant(element), element.DesiredSize.Width);

                    var preferred = RibbonItem.GetPreferredVariant(element);
                    var variant = RibbonItem.Constrain(element, preferred == VariantSize.Small ? VariantSize.Small : VariantSize.Medium);
                    entries.Add(new Entry
                    {
                        Group = g,
                        Item = element,
                        Variant = variant == VariantSize.Large ? VariantSize.Medium : variant,
                        Overflowed = RibbonItem.GetCollapseBehavior(element) == ItemCollapseBehavior.Always && element is IRibbonControl
                    });
                }
            }

            double ItemWidth(Entry e) => Probe(e.Item, e.Variant, () => RibbonItem.GetVariant(e.Item), v => RibbonItem.SetVariant(e.Item, v), itemConstraint);

            double Total()
            {
                double total = fixedWidth;
                foreach (var g in groups)
                {
                    var inline = entries.Where(e => e.Group == g && !e.Overflowed).ToList();
                    var groupButton = g.ChildOverflowTarget == RibbonOverflowTarget.Group && entries.Any(e => e.Group == g && e.Overflowed);
                    if (inline.Count == 0 && !groupButton)
                    {
                        continue;
                    }

                    total += SimplifiedGroupChrome + inline.Sum(ItemWidth) + (Math.Max(0, inline.Count - 1) * SimplifiedItemGap);
                    if (groupButton)
                    {
                        total += GroupOverflowButtonWidth;
                    }
                }

                return total + TabOverflowReserve(entries);
            }

            var estimate = Total();
            if (estimate > available)
            {
                foreach (var step in BuildSimplifiedSteps(entries))
                {
                    if (!step())
                    {
                        continue;
                    }

                    LastTransitionCount++;
                    estimate = Total();
                    if (estimate <= available)
                    {
                        break;
                    }
                }
            }

            Apply(entries, groups);
            foreach (var g in groups)
            {
                MeasureFresh(g, constraint);
            }

            Owner?.UpdateOverflow(
                entries.Where(e => e.Overflowed && e.Group.ChildOverflowTarget == RibbonOverflowTarget.Tab).Select(e => e.Item).ToList(),
                groups.ToDictionary(g => g, g => entries.Where(e => e.Overflowed && e.Group == g && g.ChildOverflowTarget == RibbonOverflowTarget.Group).Select(e => e.Item).ToList()));

            return MeasureAll(children, height) + TabOverflowReserve(entries);
        }

        private static double TabOverflowReserve(List<Entry> entries)
        {
            return entries.Any(e => e.Overflowed && e.Group.ChildOverflowTarget == RibbonOverflowTarget.Tab) ? RibbonMetrics.TabOverflowButtonWidth : 0;
        }

        private static void Apply(List<Entry> entries, List<RibbonGroup> groups)
        {
            foreach (var e in entries)
            {
                if (RibbonItem.GetVariant(e.Item) != e.Variant)
                {
                    RibbonItem.SetVariant(e.Item, e.Variant);
                }

                if (RibbonItem.GetIsOverflowed(e.Item) != e.Overflowed)
                {
                    RibbonItem.SetIsOverflowed(e.Item, e.Overflowed);
                }
            }

            foreach (var g in groups)
            {
                var hasGroupOverflow = g.ChildOverflowTarget == RibbonOverflowTarget.Group && entries.Any(e => e.Group == g && e.Overflowed);
                g.SetHasOverflowItems(hasGroupOverflow);
                g.SetIsInlineEmpty(!hasGroupOverflow && entries.Any(e => e.Group == g) && entries.Where(e => e.Group == g).All(e => e.Overflowed));
            }
        }

        /// <summary>
        /// Builds the ordered Simplified reduction steps: explicit <see cref="RibbonTabItem.ControlVariants"/>,
        /// then right-to-left medium→small, then right-to-left overflow of <see cref="ItemCollapseBehavior.Default"/>
        /// commands. Each step returns whether it changed anything.
        /// </summary>
        private IEnumerable<Func<bool>> BuildSimplifiedSteps(List<Entry> entries)
        {
            var rules = Owner?.ControlVariants;
            if (rules != null)
            {
                foreach (var rule in rules)
                {
                    var entry = entries.FirstOrDefault(e => string.Equals(RibbonItem.GetKey(e.Item), rule.ItemKey, StringComparison.Ordinal));
                    if (entry == null)
                    {
                        continue;
                    }

                    yield return () => ApplyRule(entry, rule.Variant);
                }
            }

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                yield return () => ApplyRule(entry, VariantSize.Small);
            }

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                yield return () => ApplyRule(entry, VariantSize.Collapsed);
            }
        }

        private static bool ApplyRule(Entry entry, VariantSize variant)
        {
            if (entry.Overflowed)
            {
                return false;
            }

            if (variant == VariantSize.Collapsed)
            {
                // Only commands that can produce an overflow presentation may leave the row, so every
                // overflowed command has a reachable destination.
                if (RibbonItem.GetCollapseBehavior(entry.Item) != ItemCollapseBehavior.Default || entry.Item is not IRibbonControl)
                {
                    return false;
                }

                entry.Overflowed = true;
                return true;
            }

            var constrained = RibbonItem.Constrain(entry.Item, variant);
            if (constrained <= entry.Variant)
            {
                return false;
            }

            entry.Variant = constrained;
            return true;
        }

        private void ClearSimplifiedState()
        {
            foreach (UIElement child in InternalChildren)
            {
                if (child is not RibbonGroup g)
                {
                    continue;
                }

                g.SetHasOverflowItems(false);
                g.SetIsInlineEmpty(false);
                foreach (var item in g.Items)
                {
                    if (item is UIElement element && RibbonItem.GetIsOverflowed(element))
                    {
                        RibbonItem.SetIsOverflowed(element, false);
                    }
                }
            }

            Owner?.UpdateOverflow(new List<UIElement>(), new Dictionary<RibbonGroup, List<UIElement>>());
        }

        #endregion

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var maxOffset = Math.Max(0, _extent - finalSize.Width);
            _offset = Math.Clamp(_offset, 0, maxOffset);

            var x = -_offset;
            foreach (UIElement child in InternalChildren)
            {
                if (child == null || child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                x += child.DesiredSize.Width;
            }

            Owner?.SetScrollState(_offset > 0.5, _offset < maxOffset - 0.5);
            return finalSize;
        }

        /// <inheritdoc />
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            return new RectangleGeometry(new Rect(RenderSize));
        }
    }
}
