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

using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A Ribbon gallery (for example Styles). At the large variant a row of items is shown inline with
    /// line-scroll and expand buttons; at smaller variants it becomes a drop-down button. The expanded
    /// view lists the same data items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Highlight is distinct from selection: pointing at an item (or moving through the expanded list with
    /// the keyboard) raises <see cref="ItemHighlighted"/> for live preview without changing
    /// <see cref="Selector.SelectedItem"/>. Selection commits once, on click or Enter.
    /// </para>
    /// <para>
    /// The expanded view shares data items, so populate the gallery through <see cref="ItemsControl.ItemsSource"/>
    /// (or with non-element items). Element items can only be shown inline.
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PartLineUp, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartLineDown, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartPopupList, Type = typeof(ListBox))]
    [DefaultEvent(nameof(ItemHighlighted))]
    public class RibbonGallery : ListBox, IRibbonControl, ICommandSource
    {
        private const string PartScrollViewer = "PART_ScrollViewer";
        private const string PartLineUp = "PART_LineUp";
        private const string PartLineDown = "PART_LineDown";
        private const string PartPopup = "PART_Popup";
        private const string PartPopupList = "PART_PopupList";

        private ScrollViewer? _scrollViewer;
        private ButtonBase? _lineUp;
        private ButtonBase? _lineDown;
        private Popup? _popup;
        private ListBox? _popupList;
        private bool _syncingPopup;
        private ListBoxItem? _pressedPopupItem;

        #region Shared Ribbon properties

        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>Identifies the <see cref="LargeImage"/> dependency property.</summary>
        public static readonly DependencyProperty LargeImageProperty = RibbonItem.LargeImageProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>Identifies the <see cref="SmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty SmallImageProperty = RibbonItem.SmallImageProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>Identifies the <see cref="PreferredVariant"/> dependency property.</summary>
        public static readonly DependencyProperty PreferredVariantProperty = RibbonItem.PreferredVariantProperty.AddOwner(typeof(RibbonGallery),
            new FrameworkPropertyMetadata(VariantSize.Large, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
        public static readonly DependencyProperty VariantProperty = RibbonItem.VariantProperty.AddOwner(typeof(RibbonGallery));

        /// <summary>Gets or sets the stable command key.</summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the label shown on the drop-down button variants.</summary>
        [Category("Ribbon")]
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets the large image.</summary>
        [Category("Ribbon")]
        public ImageSource? LargeImage { get => (ImageSource?)GetValue(LargeImageProperty); set => SetValue(LargeImageProperty, value); }

        /// <summary>Gets or sets the small image.</summary>
        [Category("Ribbon")]
        public ImageSource? SmallImage { get => (ImageSource?)GetValue(SmallImageProperty); set => SetValue(SmallImageProperty, value); }

        /// <summary>Gets or sets a font glyph used when no image is assigned.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Gets or sets the screen tip description.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        /// <summary>Gets or sets the most expanded variant (defaults to the inline <see cref="VariantSize.Large"/> gallery).</summary>
        [Category("Ribbon")]
        public VariantSize PreferredVariant { get => (VariantSize)GetValue(PreferredVariantProperty); set => SetValue(PreferredVariantProperty, value); }

        /// <summary>Gets the container-assigned variant.</summary>
        [Browsable(false)]
        public VariantSize Variant => (VariantSize)GetValue(VariantProperty);

        #endregion

        /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the user picks an item (a click or Enter on an inline item, or a
        /// commit in the expanded view). The parameter is <see cref="CommandParameter"/> when set, otherwise the
        /// picked item. Programmatic selection changes never execute it.
        /// </summary>
        [Category("Action")]
        public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        /// <summary>Gets or sets a fixed command parameter. When <c>null</c>, the picked item is passed.</summary>
        [Category("Action")]
        public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

        /// <summary>Identifies the <see cref="CommandTarget"/> dependency property.</summary>
        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
            nameof(CommandTarget), typeof(IInputElement), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        /// <summary>Gets or sets the routed command target.</summary>
        [Category("Action")]
        public IInputElement? CommandTarget { get => (IInputElement?)GetValue(CommandTargetProperty); set => SetValue(CommandTargetProperty, value); }

        /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register(
            nameof(ItemWidth), typeof(double), typeof(RibbonGallery),
            new FrameworkPropertyMetadata(72.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure, OnInlineWidthInputsChanged),
            RibbonValidation.IsPositiveFinite);

        /// <summary>Gets or sets the width of each gallery cell. Must be a finite value greater than zero.</summary>
        [Category("Layout")]
        public double ItemWidth { get => (double)GetValue(ItemWidthProperty); set => SetValue(ItemWidthProperty, value); }

        /// <summary>Identifies the <see cref="ItemHeight"/> dependency property.</summary>
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            nameof(ItemHeight), typeof(double), typeof(RibbonGallery), new FrameworkPropertyMetadata(38.0, FrameworkPropertyMetadataOptions.AffectsMeasure),
            RibbonValidation.IsPositiveFinite);

        /// <summary>Gets or sets the height of each gallery cell. Must be a finite value greater than zero.</summary>
        [Category("Layout")]
        public double ItemHeight { get => (double)GetValue(ItemHeightProperty); set => SetValue(ItemHeightProperty, value); }

        /// <summary>Identifies the <see cref="InlineColumns"/> dependency property.</summary>
        public static readonly DependencyProperty InlineColumnsProperty = DependencyProperty.Register(
            nameof(InlineColumns), typeof(int), typeof(RibbonGallery),
            new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure, OnInlineWidthInputsChanged),
            RibbonValidation.IsAtLeastOne);

        /// <summary>Gets or sets the number of columns shown inline at the large variant (at least one).</summary>
        [Category("Layout")]
        public int InlineColumns { get => (int)GetValue(InlineColumnsProperty); set => SetValue(InlineColumnsProperty, value); }

        /// <summary>Identifies the <see cref="DropDownColumns"/> dependency property.</summary>
        public static readonly DependencyProperty DropDownColumnsProperty = DependencyProperty.Register(
            nameof(DropDownColumns), typeof(int), typeof(RibbonGallery), new FrameworkPropertyMetadata(5, OnInlineWidthInputsChanged),
            RibbonValidation.IsAtLeastOne);

        /// <summary>Gets or sets the number of columns in the expanded view (at least one).</summary>
        [Category("Layout")]
        public int DropDownColumns { get => (int)GetValue(DropDownColumnsProperty); set => SetValue(DropDownColumnsProperty, value); }

        private static readonly DependencyPropertyKey InlineWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(InlineWidth), typeof(double), typeof(RibbonGallery), new FrameworkPropertyMetadata(288.0));

        /// <summary>Identifies the <see cref="InlineWidth"/> dependency property.</summary>
        public static readonly DependencyProperty InlineWidthProperty = InlineWidthPropertyKey.DependencyProperty;

        /// <summary>Gets the width of the inline item area (columns × item width).</summary>
        [Browsable(false)]
        public double InlineWidth => (double)GetValue(InlineWidthProperty);

        private static readonly DependencyPropertyKey DropDownItemsWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(DropDownItemsWidth), typeof(double), typeof(RibbonGallery), new FrameworkPropertyMetadata(360.0));

        /// <summary>Identifies the <see cref="DropDownItemsWidth"/> dependency property.</summary>
        public static readonly DependencyProperty DropDownItemsWidthProperty = DropDownItemsWidthPropertyKey.DependencyProperty;

        /// <summary>Gets the width of the expanded item area (drop-down columns × item width).</summary>
        [Browsable(false)]
        public double DropDownItemsWidth => (double)GetValue(DropDownItemsWidthProperty);

        /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            nameof(IsDropDownOpen), typeof(bool), typeof(RibbonGallery),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        /// <summary>Gets or sets a value indicating whether the expanded view is open.</summary>
        [Category("Common")]
        public bool IsDropDownOpen { get => (bool)GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

        /// <summary>Identifies the <see cref="MaxDropDownHeight"/> dependency property.</summary>
        public static readonly DependencyProperty MaxDropDownHeightProperty = DependencyProperty.Register(
            nameof(MaxDropDownHeight), typeof(double), typeof(RibbonGallery), new FrameworkPropertyMetadata(360.0),
            RibbonValidation.IsPositiveOrInfinity);

        /// <summary>Gets or sets the maximum height of the expanded item list (greater than zero; infinity removes the limit).</summary>
        [Category("Layout")]
        public double MaxDropDownHeight { get => (double)GetValue(MaxDropDownHeightProperty); set => SetValue(MaxDropDownHeightProperty, value); }

        /// <summary>Identifies the <see cref="DropDownFooter"/> dependency property.</summary>
        public static readonly DependencyProperty DropDownFooterProperty = DependencyProperty.Register(
            nameof(DropDownFooter), typeof(object), typeof(RibbonGallery), new FrameworkPropertyMetadata(null));

        /// <summary>Gets or sets content (typically <see cref="RibbonMenuItem"/>s) shown under the expanded list.</summary>
        [Category("Common")]
        public object? DropDownFooter { get => GetValue(DropDownFooterProperty); set => SetValue(DropDownFooterProperty, value); }

        private static readonly DependencyPropertyKey HighlightedItemPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HighlightedItem), typeof(object), typeof(RibbonGallery), new FrameworkPropertyMetadata(null, OnHighlightedItemChanged));

        /// <summary>Identifies the <see cref="HighlightedItem"/> dependency property.</summary>
        public static readonly DependencyProperty HighlightedItemProperty = HighlightedItemPropertyKey.DependencyProperty;

        /// <summary>Gets the item currently highlighted for preview, or <c>null</c>.</summary>
        [Browsable(false)]
        public object? HighlightedItem => GetValue(HighlightedItemProperty);

        /// <summary>Identifies the <see cref="ItemHighlighted"/> routed event.</summary>
        public static readonly RoutedEvent ItemHighlightedEvent = EventManager.RegisterRoutedEvent(
            nameof(ItemHighlighted), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object?>), typeof(RibbonGallery));

        /// <summary>
        /// Occurs when the highlighted (previewed) item changes. <c>NewValue</c> is <c>null</c> when the
        /// highlight ends, at which point a live preview should revert to the selection.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<object?> ItemHighlighted
        {
            add => AddHandler(ItemHighlightedEvent, value);
            remove => RemoveHandler(ItemHighlightedEvent, value);
        }

        static RibbonGallery()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonGallery), new FrameworkPropertyMetadata(typeof(RibbonGallery)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonGallery"/> class.
        /// </summary>
        public RibbonGallery()
        {
            RibbonScreenTip.AttachDefault(this);
            UpdateInlineWidth();
        }

        private static void OnInlineWidthInputsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RibbonGallery)d).UpdateInlineWidth();
        }

        private void UpdateInlineWidth()
        {
            SetValue(InlineWidthPropertyKey, Math.Max(1, InlineColumns) * ItemWidth);
            SetValue(DropDownItemsWidthPropertyKey, Math.Max(1, DropDownColumns) * ItemWidth);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_lineUp != null)
            {
                _lineUp.Click -= OnLineUp;
            }

            if (_lineDown != null)
            {
                _lineDown.Click -= OnLineDown;
            }

            if (_popup != null)
            {
                _popup.Opened -= OnPopupOpened;
                _popup.Closed -= OnPopupClosed;
            }

            if (_popupList != null)
            {
                _popupList.SelectionChanged -= OnPopupListSelectionChanged;
                _popupList.PreviewMouseLeftButtonDown -= OnPopupListMouseDown;
                _popupList.PreviewMouseLeftButtonUp -= OnPopupListMouseUp;
                _popupList.PreviewKeyDown -= OnPopupListKeyDown;
                _popupList.MouseMove -= OnPopupListMouseMove;
                _popupList.MouseLeave -= OnListMouseLeave;
            }

            base.OnApplyTemplate();

            _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
            _lineUp = GetTemplateChild(PartLineUp) as ButtonBase;
            _lineDown = GetTemplateChild(PartLineDown) as ButtonBase;
            _popup = GetTemplateChild(PartPopup) as Popup;
            _popupList = GetTemplateChild(PartPopupList) as ListBox;

            if (_lineUp != null)
            {
                _lineUp.Click += OnLineUp;
            }

            if (_lineDown != null)
            {
                _lineDown.Click += OnLineDown;
            }

            if (_popup != null)
            {
                _popup.Opened += OnPopupOpened;
                _popup.Closed += OnPopupClosed;
            }

            if (_popupList != null)
            {
                _popupList.SelectionChanged += OnPopupListSelectionChanged;
                _popupList.PreviewMouseLeftButtonDown += OnPopupListMouseDown;
                _popupList.PreviewMouseLeftButtonUp += OnPopupListMouseUp;
                _popupList.PreviewKeyDown += OnPopupListKeyDown;
                _popupList.MouseMove += OnPopupListMouseMove;
                _popupList.MouseLeave += OnListMouseLeave;
            }
        }

        private void OnLineUp(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _scrollViewer?.ScrollToVerticalOffset(Math.Max(0, _scrollViewer.VerticalOffset - ItemHeight));
        }

        private void OnLineDown(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _scrollViewer?.ScrollToVerticalOffset(Math.Min(_scrollViewer.ScrollableHeight, _scrollViewer.VerticalOffset + ItemHeight));
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gallery = (RibbonGallery)d;
            if ((bool)e.NewValue)
            {
                gallery.SyncPopupList();
            }
            else
            {
                gallery.SetValue(HighlightedItemPropertyKey, null);
            }
        }

        private void SyncPopupList()
        {
            if (_popupList == null)
            {
                return;
            }

            _syncingPopup = true;
            try
            {
                _popupList.ItemsSource = ItemsSource ?? (Items.Cast<object>().Any(i => i is UIElement) ? null : Items);
                _popupList.SelectedItem = SelectedItem;
            }
            finally
            {
                _syncingPopup = false;
            }
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (_popupList == null)
                {
                    return;
                }

                var target = _popupList.SelectedItem != null
                    ? _popupList.ItemContainerGenerator.ContainerFromItem(_popupList.SelectedItem) as UIElement
                    : null;

                if (target != null)
                {
                    target.Focus();
                }
                else
                {
                    _popupList.Focus();
                }
            }));
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            _pressedPopupItem = null;
            if (IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        private void OnPopupListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The expanded list's selection is only a highlight; it must not surface as a gallery selection.
            e.Handled = true;
            if (!_syncingPopup && _popupList != null)
            {
                SetValue(HighlightedItemPropertyKey, _popupList.SelectedItem);
            }
        }

        private void OnPopupListMouseDown(object sender, MouseButtonEventArgs e)
        {
            _pressedPopupItem = _popupList != null ? ItemsControl.ContainerFromElement(_popupList, (DependencyObject)e.OriginalSource) as ListBoxItem : null;
        }

        private void OnPopupListMouseUp(object sender, MouseButtonEventArgs e)
        {
            // Commit only a complete click on one item. A release without a matching press (for example the
            // release of the click that opened the drop-down over the inline gallery) must not pick an item.
            var pressed = _pressedPopupItem;
            _pressedPopupItem = null;
            if (_popupList != null && pressed != null
                && ItemsControl.ContainerFromElement(_popupList, (DependencyObject)e.OriginalSource) is ListBoxItem container
                && ReferenceEquals(container, pressed))
            {
                Commit(_popupList.ItemContainerGenerator.ItemFromContainer(container));
            }
        }

        private void OnPopupListKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter or System.Windows.Input.Key.Space when _popupList?.SelectedItem != null:
                    Commit(_popupList.SelectedItem);
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Escape:
                    SetCurrentValue(IsDropDownOpenProperty, false);
                    Focus();
                    e.Handled = true;
                    break;
            }
        }

        private void OnPopupListMouseMove(object sender, MouseEventArgs e)
        {
            if (_popupList != null)
            {
                Highlight(_popupList, e.OriginalSource as DependencyObject);
            }
        }

        private void OnListMouseLeave(object sender, MouseEventArgs e)
        {
            SetValue(HighlightedItemPropertyKey, null);
        }

        private void Highlight(ItemsControl owner, DependencyObject? source)
        {
            object? item = null;
            if (source != null && ItemsControl.ContainerFromElement(owner, source) is ListBoxItem container)
            {
                item = owner.ItemContainerGenerator.ItemFromContainer(container);
            }

            if (!Equals(item, HighlightedItem))
            {
                SetValue(HighlightedItemPropertyKey, item);
            }
        }

        private void Commit(object? item)
        {
            if (item == null || item == DependencyProperty.UnsetValue)
            {
                return;
            }

            SetCurrentValue(IsDropDownOpenProperty, false);
            SetValue(HighlightedItemPropertyKey, null);
            SetCurrentValue(SelectedItemProperty, item);
            ExecuteCommand(item);
            RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
        }

        private void ExecuteCommand(object? item)
        {
            if (Command != null && item != null)
            {
                RibbonCommandHelper.Execute(Command, CommandParameter ?? item, CommandTarget, this);
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled && !IsDropDownOpen && e.Key == System.Windows.Input.Key.Enter && SelectedItem != null)
            {
                ExecuteCommand(SelectedItem);
                RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
                e.Handled = true;
            }
        }

        private static void OnHighlightedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gallery = (RibbonGallery)d;
            gallery.RaiseEvent(new RoutedPropertyChangedEventArgs<object?>(e.OldValue, e.NewValue, ItemHighlightedEvent));
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!IsDropDownOpen)
            {
                Highlight(this, e.OriginalSource as DependencyObject);
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (!IsDropDownOpen)
            {
                SetValue(HighlightedItemPropertyKey, null);
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonUp(e);
            if (ContainerFromElement(this, (DependencyObject)e.OriginalSource) is ListBoxItem container)
            {
                SetValue(HighlightedItemPropertyKey, null);
                var item = ItemContainerGenerator.ItemFromContainer(container);
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    ExecuteCommand(item == DependencyProperty.UnsetValue ? SelectedItem : item);
                    RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
                }));
            }
        }

        /// <inheritdoc />
        public bool SupportsLargeVariant => true;

        /// <inheritdoc />
        public bool IsKeyTipTarget => true;

        /// <inheritdoc />
        public string? KeyTipLabel => Label;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            SetCurrentValue(IsDropDownOpenProperty, true);
            return null;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            var clone = new RibbonGallery
            {
                ItemsSource = ItemsSource,
                ItemTemplate = ItemTemplate,
                ItemTemplateSelector = ItemTemplateSelector,
                DisplayMemberPath = DisplayMemberPath,
                ItemWidth = ItemWidth,
                ItemHeight = ItemHeight,
                DropDownColumns = DropDownColumns,
                PreferredVariant = VariantSize.Medium
            };

            RibbonItem.SetVariantBehavior(clone, ItemVariantBehavior.AlwaysMedium);
            RibbonItem.SetVariant(clone, kind == RibbonPresentationKind.Overflow ? VariantSize.Medium : VariantSize.Small);
            RibbonPresentationFactory.BindVisuals(clone, this);
            RibbonPresentationFactory.Bind(clone, CommandProperty, this, CommandProperty);
            RibbonPresentationFactory.Bind(clone, CommandParameterProperty, this, CommandParameterProperty);
            RibbonPresentationFactory.Bind(clone, CommandTargetProperty, this, CommandTargetProperty);
            clone.SetBinding(SelectedItemProperty, new Binding { Source = this, Path = new PropertyPath(SelectedItemProperty), Mode = BindingMode.TwoWay });
            clone.ItemHighlighted += (_, e) => SetValue(HighlightedItemPropertyKey, e.NewValue);

            if (kind == RibbonPresentationKind.Overflow)
            {
                clone.Margin = new Thickness(24, 1, 4, 1);
                clone.HorizontalAlignment = HorizontalAlignment.Left;
            }

            return clone;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGalleryAutomationPeer(this);
    }
}
