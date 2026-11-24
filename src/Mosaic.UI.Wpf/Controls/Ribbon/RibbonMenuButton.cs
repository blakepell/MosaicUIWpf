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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A menu entry inside a Ribbon drop-down, overflow menu or Backstage list. Behaves like a
    /// <see cref="MenuItem"/> and adds Ribbon identity, icons and key tip support.
    /// </summary>
    public class RibbonMenuItem : MenuItem, IRibbonControl
    {
        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>Identifies the <see cref="SmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty SmallImageProperty = RibbonItem.SmallImageProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>Identifies the <see cref="UseAdaptiveSmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty UseAdaptiveSmallImageProperty = RibbonItem.UseAdaptiveSmallImageProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>Identifies the <see cref="GlyphBrush"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphBrushProperty = RibbonItem.GlyphBrushProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonMenuItem));

        /// <summary>Gets or sets the stable command key.</summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the label used for Quick Access Toolbar and automation when <see cref="HeaderedItemsControl.Header"/> is not text.</summary>
        [Category("Ribbon")]
        public string? Label { get => (string?)GetValue(LabelProperty) ?? Header as string; set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets the 16×16 image.</summary>
        [Category("Ribbon")]
        public ImageSource? SmallImage { get => (ImageSource?)GetValue(SmallImageProperty); set => SetValue(SmallImageProperty, value); }

        /// <summary>Gets or sets whether <see cref="SmallImage"/> uses <see cref="AdaptiveImage"/>. The default is <see langword="false"/>.</summary>
        [Category("Appearance")]
        public bool UseAdaptiveSmallImage { get => (bool)GetValue(UseAdaptiveSmallImageProperty); set => SetValue(UseAdaptiveSmallImageProperty, value); }

        /// <summary>Gets or sets a font glyph used when no image is assigned.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Gets or sets an optional glyph color.</summary>
        [Category("Ribbon")]
        public Brush? GlyphBrush { get => (Brush?)GetValue(GlyphBrushProperty); set => SetValue(GlyphBrushProperty, value); }

        /// <summary>Gets or sets an optional second line of explanatory text displayed under the header.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        internal Action? ForwardClick { get; set; }

        static RibbonMenuItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonMenuItem), new FrameworkPropertyMetadata(typeof(RibbonMenuItem)));
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            if (ForwardClick != null)
            {
                ForwardClick();
                return;
            }

            base.OnClick();

            // StaysOpenOnClick items (for example a list of check boxes) keep their menu open, so they must not ask
            // the Ribbon to dismiss its transient surfaces.
            if (!StaysOpenOnClick)
            {
                RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
            }
        }

        /// <summary>
        /// Activates the item as if clicked, honoring <see cref="UIElement.IsEnabled"/>.
        /// </summary>
        public void PerformClick()
        {
            if (IsEnabled)
            {
                OnClick();
            }
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is MenuItem or Separator;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new RibbonMenuItem();

        /// <inheritdoc />
        public bool SupportsLargeVariant => false;

        /// <inheritdoc />
        public bool IsKeyTipTarget => true;

        /// <inheritdoc />
        public string? KeyTipLabel => Label;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            if (HasItems)
            {
                IsSubmenuOpen = true;
                return null;
            }

            PerformClick();
            return null;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            if (kind == RibbonPresentationKind.QuickAccessToolbar)
            {
                return RibbonPresentationFactory.CreateButton(this, PerformClick);
            }

            var mirror = new RibbonMenuItem { ForwardClick = HasItems ? null : PerformClick };
            RibbonPresentationFactory.BindVisuals(mirror, this);
            RibbonPresentationFactory.Bind(mirror, HeaderProperty, this, HeaderProperty);
            RibbonPresentationFactory.Bind(mirror, IsCheckableProperty, this, IsCheckableProperty);
            RibbonPresentationFactory.Bind(mirror, IsCheckedProperty, this, IsCheckedProperty);
            RibbonPresentationFactory.Bind(mirror, InputGestureTextProperty, this, InputGestureTextProperty);

            foreach (var child in RibbonMenuButton.MirrorItems(this))
            {
                mirror.Items.Add(child);
            }

            return mirror;
        }
    }

    /// <summary>
    /// A Ribbon command that opens a drop-down of <see cref="RibbonMenuItem"/>s, galleries or other content.
    /// </summary>
    [TemplatePart(Name = PartToggleButton, Type = typeof(System.Windows.Controls.Primitives.ToggleButton))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [DefaultProperty(nameof(Items))]
    public class RibbonMenuButton : ItemsControl, IRibbonControl
    {
        private const string PartToggleButton = "PART_ToggleButton";
        private const string PartPopup = "PART_Popup";

        /// <summary>The drop-down toggle part.</summary>
        protected System.Windows.Controls.Primitives.ToggleButton? ToggleButtonPart { get; private set; }

        /// <summary>The drop-down popup part.</summary>
        protected Popup? PopupPart { get; private set; }

        #region Shared Ribbon properties

        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="LargeImage"/> dependency property.</summary>
        public static readonly DependencyProperty LargeImageProperty = RibbonItem.LargeImageProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="SmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty SmallImageProperty = RibbonItem.SmallImageProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="UseAdaptiveSmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty UseAdaptiveSmallImageProperty = RibbonItem.UseAdaptiveSmallImageProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="GlyphBrush"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphBrushProperty = RibbonItem.GlyphBrushProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="PreferredVariant"/> dependency property.</summary>
        public static readonly DependencyProperty PreferredVariantProperty = RibbonItem.PreferredVariantProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
        public static readonly DependencyProperty VariantProperty = RibbonItem.VariantProperty.AddOwner(typeof(RibbonMenuButton));

        /// <summary>Gets or sets the stable command key.</summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the display label.</summary>
        [Category("Ribbon")]
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets the large image.</summary>
        [Category("Ribbon")]
        public ImageSource? LargeImage { get => (ImageSource?)GetValue(LargeImageProperty); set => SetValue(LargeImageProperty, value); }

        /// <summary>Gets or sets the small image.</summary>
        [Category("Ribbon")]
        public ImageSource? SmallImage { get => (ImageSource?)GetValue(SmallImageProperty); set => SetValue(SmallImageProperty, value); }

        /// <summary>Gets or sets whether <see cref="SmallImage"/> uses <see cref="AdaptiveImage"/>. The default is <see langword="false"/>.</summary>
        [Category("Appearance")]
        public bool UseAdaptiveSmallImage { get => (bool)GetValue(UseAdaptiveSmallImageProperty); set => SetValue(UseAdaptiveSmallImageProperty, value); }

        /// <summary>Gets or sets a font glyph used when no image is assigned.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Gets or sets an optional glyph color.</summary>
        [Category("Ribbon")]
        public Brush? GlyphBrush { get => (Brush?)GetValue(GlyphBrushProperty); set => SetValue(GlyphBrushProperty, value); }

        /// <summary>Gets or sets the screen tip description.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        /// <summary>Gets or sets the most expanded variant used when space allows.</summary>
        [Category("Ribbon")]
        public VariantSize PreferredVariant { get => (VariantSize)GetValue(PreferredVariantProperty); set => SetValue(PreferredVariantProperty, value); }

        /// <summary>Gets the container-assigned variant.</summary>
        [Browsable(false)]
        public VariantSize Variant => (VariantSize)GetValue(VariantProperty);

        #endregion

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            nameof(IsDropDownOpen), typeof(bool), typeof(RibbonMenuButton),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the drop-down is open.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the drop-down is open.")]
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxDropDownHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxDropDownHeightProperty = DependencyProperty.Register(
            nameof(MaxDropDownHeight), typeof(double), typeof(RibbonMenuButton), new FrameworkPropertyMetadata(480.0),
            RibbonValidation.IsPositiveOrInfinity);

        /// <summary>
        /// Gets or sets the maximum height of the drop-down before it scrolls (greater than zero; infinity removes the limit).
        /// </summary>
        [Category("Layout")]
        public double MaxDropDownHeight
        {
            get => (double)GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownOpened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownOpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownOpened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonMenuButton));

        /// <summary>
        /// Occurs when the drop-down opens.
        /// </summary>
        public event RoutedEventHandler DropDownOpened
        {
            add => AddHandler(DropDownOpenedEvent, value);
            remove => RemoveHandler(DropDownOpenedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownClosed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownClosedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownClosed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonMenuButton));

        /// <summary>
        /// Occurs when the drop-down closes.
        /// </summary>
        public event RoutedEventHandler DropDownClosed
        {
            add => AddHandler(DropDownClosedEvent, value);
            remove => RemoveHandler(DropDownClosedEvent, value);
        }

        static RibbonMenuButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(typeof(RibbonMenuButton)));
            FocusableProperty.OverrideMetadata(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(false));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(RibbonMenuButton), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonMenuButton"/> class.
        /// </summary>
        public RibbonMenuButton()
        {
            RibbonScreenTip.AttachDefault(this);
            AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(OnMenuItemClick));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (PopupPart != null)
            {
                PopupPart.Opened -= OnPopupOpened;
                PopupPart.Closed -= OnPopupClosed;
                PopupPart.PreviewKeyDown -= OnPopupPreviewKeyDown;
            }

            if (ToggleButtonPart != null)
            {
                ToggleButtonPart.PreviewKeyDown -= OnTogglePreviewKeyDown;
                ToggleButtonPart.Click -= OnToggleClick;
            }

            base.OnApplyTemplate();

            ToggleButtonPart = GetTemplateChild(PartToggleButton) as System.Windows.Controls.Primitives.ToggleButton;
            PopupPart = GetTemplateChild(PartPopup) as Popup;

            if (PopupPart != null)
            {
                PopupPart.Opened += OnPopupOpened;
                PopupPart.Closed += OnPopupClosed;
                PopupPart.PreviewKeyDown += OnPopupPreviewKeyDown;
            }

            if (ToggleButtonPart != null)
            {
                ToggleButtonPart.PreviewKeyDown += OnTogglePreviewKeyDown;
                ToggleButtonPart.Click += OnToggleClick;
            }
        }

        private static void OnToggleClick(object sender, RoutedEventArgs e)
        {
            // The drop-down toggle is not a command; keep its click from reaching ButtonBase.Click listeners.
            e.Handled = true;
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (RibbonMenuButton)d;
            if (!(bool)e.NewValue && button.PopupPart?.Child != null && button.PopupPart.Child.IsKeyboardFocusWithin)
            {
                // Return focus to the invoker before the popup root goes away so focus never lands on an
                // invalid intermediate element.
                button.FocusInvoker();
            }
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(DropDownOpenedEvent, this));

            if (ToggleButtonPart is { IsKeyboardFocused: true } && InputManager.Current.MostRecentInputDevice is KeyboardDevice)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(FocusFirstItem));
            }
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            if (IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }

            RaiseEvent(new RoutedEventArgs(DropDownClosedEvent, this));
        }

        /// <summary>
        /// Moves keyboard focus to the first focusable element in the drop-down.
        /// </summary>
        internal void FocusFirstItem()
        {
            PopupPart?.Child?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        /// <summary>
        /// Moves keyboard focus back to the drop-down invoker.
        /// </summary>
        protected internal virtual void FocusInvoker()
        {
            ToggleButtonPart?.Focus();
        }

        private void OnTogglePreviewKeyDown(object sender, KeyEventArgs e)
        {
            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            if (!IsDropDownOpen && (key == System.Windows.Input.Key.Down || key == System.Windows.Input.Key.F4 || (key == System.Windows.Input.Key.Down && Keyboard.Modifiers == ModifierKeys.Alt)))
            {
                SetCurrentValue(IsDropDownOpenProperty, true);
                Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(FocusFirstItem));
                e.Handled = true;
            }
        }

        private void OnPopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                // A nested submenu handles Escape itself; only close when focus is at this level.
                if (e.OriginalSource is MenuItem { IsSubmenuOpen: true })
                {
                    return;
                }

                SetCurrentValue(IsDropDownOpenProperty, false);
                FocusInvoker();
                e.Handled = true;
            }
        }

        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem { StaysOpenOnClick: true })
            {
                return;
            }

            SetCurrentValue(IsDropDownOpenProperty, false);
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new RibbonMenuItem();

        /// <summary>
        /// Creates mirrored overflow presentations for the items of a menu. Data items (from
        /// <see cref="ItemsControl.ItemsSource"/>) are mirrored as <see cref="RibbonMenuItem"/>s that carry the
        /// item as their data context and reuse the owner's item template, container style and display member path,
        /// so a data-bound menu is just as complete in overflow and the Quick Access Toolbar.
        /// </summary>
        internal static IEnumerable<FrameworkElement> MirrorItems(ItemsControl owner)
        {
            foreach (var item in owner.Items)
            {
                switch (item)
                {
                    case Separator:
                        yield return new Separator();
                        break;
                    case IRibbonControl rc when rc.CreatePresentation(RibbonPresentationKind.Overflow) is { } presentation:
                        yield return presentation;
                        break;
                    case UIElement:
                        // Arbitrary content (for example a color picker) has a single parent and cannot be mirrored.
                        break;
                    case not null:
                        yield return MirrorDataItem(owner, item);
                        break;
                }
            }
        }

        private static RibbonMenuItem MirrorDataItem(ItemsControl owner, object item)
        {
            var row = new RibbonMenuItem
            {
                DataContext = item,
                HeaderTemplate = owner.ItemTemplate,
                HeaderTemplateSelector = owner.ItemTemplateSelector
            };

            if (owner.ItemContainerStyle is { } style && style.TargetType.IsInstanceOfType(row))
            {
                row.Style = style;
            }

            // Mirror the generator's header rule: DisplayMemberPath, else the item itself, unless the style set one.
            if (!string.IsNullOrEmpty(owner.DisplayMemberPath))
            {
                row.SetBinding(HeaderedItemsControl.HeaderProperty, new System.Windows.Data.Binding(owner.DisplayMemberPath));
            }
            else if (row.ReadLocalValue(HeaderedItemsControl.HeaderProperty) == DependencyProperty.UnsetValue && row.Header == null)
            {
                row.Header = item;
            }

            return row;
        }

        /// <inheritdoc />
        public bool SupportsLargeVariant => true;

        /// <inheritdoc />
        public bool IsKeyTipTarget => true;

        /// <inheritdoc />
        public string? KeyTipLabel => Label;

        /// <inheritdoc />
        public virtual UIElement? ActivateKeyTip()
        {
            if (!IsEnabled)
            {
                return null;
            }

            SetCurrentValue(IsDropDownOpenProperty, true);
            return PopupPart?.Child;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            if (kind == RibbonPresentationKind.Overflow)
            {
                var header = new RibbonMenuItem();
                RibbonPresentationFactory.BindVisuals(header, this);
                RibbonPresentationFactory.Bind(header, HeaderedItemsControl.HeaderProperty, this, LabelProperty);
                foreach (var child in MirrorItems(this))
                {
                    header.Items.Add(child);
                }

                return header;
            }

            var menu = new RibbonMenuButton();
            RibbonPresentationFactory.BindVisuals(menu, this);
            foreach (var child in MirrorItems(this))
            {
                menu.Items.Add(child);
            }

            return menu;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMenuButtonAutomationPeer(this);
    }

    /// <summary>
    /// A Ribbon command with a primary action and a separate drop-down (for example Paste). Clicking the
    /// primary surface raises <see cref="Click"/> and executes <see cref="Command"/>; opening the drop-down
    /// never invokes the primary action.
    /// </summary>
    [TemplatePart(Name = PartPrimaryButton, Type = typeof(ButtonBase))]
    [DefaultEvent(nameof(Click))]
    public class RibbonSplitButton : RibbonMenuButton, ICommandSource
    {
        private const string PartPrimaryButton = "PART_PrimaryButton";

        private ButtonBase? _primary;

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = ButtonBase.CommandProperty.AddOwner(typeof(RibbonSplitButton));

        /// <summary>
        /// Gets or sets the primary-action command.
        /// </summary>
        [Category("Action")]
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = ButtonBase.CommandParameterProperty.AddOwner(typeof(RibbonSplitButton));

        /// <summary>
        /// Gets or sets the primary-action command parameter.
        /// </summary>
        [Category("Action")]
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandTarget"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty = ButtonBase.CommandTargetProperty.AddOwner(typeof(RibbonSplitButton));

        /// <summary>
        /// Gets or sets the routed command target for the primary action.
        /// </summary>
        [Category("Action")]
        public IInputElement? CommandTarget
        {
            get => (IInputElement?)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsCheckable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCheckableProperty = DependencyProperty.Register(
            nameof(IsCheckable), typeof(bool), typeof(RibbonSplitButton), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the primary action toggles <see cref="IsChecked"/> (for example
        /// Bullets or Highlight in Word). The toggle happens before <see cref="Command"/> executes.
        /// </summary>
        [Category("Common")]
        [Description("Whether the primary action toggles IsChecked.")]
        public bool IsCheckable
        {
            get => (bool)GetValue(IsCheckableProperty);
            set => SetValue(IsCheckableProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsChecked"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            nameof(IsChecked), typeof(bool), typeof(RibbonSplitButton),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsCheckedChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the primary action is checked. Shown only when <see cref="IsCheckable"/>
        /// is <c>true</c>; can be bound to view model state either way.
        /// </summary>
        [Category("Common")]
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Click"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonSplitButton));

        /// <summary>
        /// Occurs when the primary action surface (not the drop-down indicator) is activated.
        /// </summary>
        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        static RibbonSplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonSplitButton), new FrameworkPropertyMetadata(typeof(RibbonSplitButton)));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_primary != null)
            {
                _primary.Click -= OnPrimaryClick;
            }

            base.OnApplyTemplate();

            _primary = GetTemplateChild(PartPrimaryButton) as ButtonBase;
            if (_primary != null)
            {
                _primary.Click += OnPrimaryClick;
            }
        }

        private void OnPrimaryClick(object sender, RoutedEventArgs e)
        {
            // The template's primary button executes the command itself (it is bound to Command) right after this
            // Click handler, so toggling here keeps the documented order: toggle, then command. We also surface the
            // split button's own Click so drop-down clicks can never be confused with it.
            e.Handled = true;
            ToggleIfCheckable();
            RaisePrimaryClick();
        }

        private void ToggleIfCheckable()
        {
            if (IsCheckable)
            {
                SetCurrentValue(IsCheckedProperty, !IsChecked);
            }
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (RibbonSplitButton)d;
            if (button.IsCheckable && UIElementAutomationPeer.FromElement(button) is { } peer)
            {
                peer.RaisePropertyChangedEvent(
                    System.Windows.Automation.TogglePatternIdentifiers.ToggleStateProperty,
                    (bool)e.OldValue ? System.Windows.Automation.ToggleState.On : System.Windows.Automation.ToggleState.Off,
                    (bool)e.NewValue ? System.Windows.Automation.ToggleState.On : System.Windows.Automation.ToggleState.Off);
            }
        }

        private void RaisePrimaryClick()
        {
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
        }

        /// <summary>
        /// Invokes the primary action, honoring command availability.
        /// </summary>
        public void PerformClick()
        {
            if (!IsEnabled || !RibbonCommandHelper.CanExecute(Command, CommandParameter, CommandTarget, this))
            {
                return;
            }

            ToggleIfCheckable();
            RibbonCommandHelper.Execute(Command, CommandParameter, CommandTarget, this);
            RaisePrimaryClick();
        }

        /// <inheritdoc />
        public override FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            if (kind == RibbonPresentationKind.QuickAccessToolbar)
            {
                if (!IsCheckable)
                {
                    return RibbonPresentationFactory.CreateButton(this, PerformClick);
                }

                var toggle = new RibbonToggleButton { ForwardClick = PerformClick };
                RibbonPresentationFactory.BindVisuals(toggle, this);
                RibbonPresentationFactory.Bind(toggle, System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, this, IsCheckedProperty);
                return toggle;
            }

            // Overflow: the primary action first, then the drop-down entries as a submenu.
            var header = (RibbonMenuItem)base.CreatePresentation(kind)!;
            var primary = header.Items.Count == 0 ? header : RibbonPresentationFactory.CreateMenuItem(this, PerformClick);
            if (header.Items.Count == 0)
            {
                header.ForwardClick = PerformClick;
            }
            else
            {
                header.Items.Insert(0, new Separator());
                header.Items.Insert(0, primary);
            }

            if (IsCheckable)
            {
                primary.IsCheckable = true;
                RibbonPresentationFactory.Bind(primary, MenuItem.IsCheckedProperty, this, IsCheckedProperty);
            }

            return header;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonSplitButtonAutomationPeer(this);
    }
}
