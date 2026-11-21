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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A labeled group of commands within a <see cref="RibbonTabItem"/> (for example Clipboard or Font).
    /// The containing tab assigns the group's <see cref="Variant"/>; at <see cref="VariantSize.Collapsed"/>
    /// the group is shown as a single button that opens its content in a popup.
    /// </summary>
    [TemplatePart(Name = PartInlineHost, Type = typeof(Decorator))]
    [TemplatePart(Name = PartPopupHost, Type = typeof(Decorator))]
    [TemplatePart(Name = PartContentRoot, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartCollapsedPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartCollapsedButton, Type = typeof(System.Windows.Controls.Primitives.ToggleButton))]
    [TemplatePart(Name = PartLauncherButton, Type = typeof(ButtonBase))]
    [DefaultEvent(nameof(LauncherClick))]
    [DefaultProperty(nameof(Header))]
    public class RibbonGroup : HeaderedItemsControl, IRibbonKeyTipTarget
    {
        private const string PartInlineHost = "PART_InlineHost";
        private const string PartPopupHost = "PART_PopupHost";
        private const string PartContentRoot = "PART_ContentRoot";
        private const string PartCollapsedPopup = "PART_CollapsedPopup";
        private const string PartCollapsedButton = "PART_CollapsedButton";
        private const string PartLauncherButton = "PART_LauncherButton";

        private Decorator? _inlineHost;
        private Decorator? _popupHost;
        private FrameworkElement? _contentRoot;
        private Popup? _collapsedPopup;
        private System.Windows.Controls.Primitives.ToggleButton? _collapsedButton;
        private ButtonBase? _launcherButton;
        private readonly ObservableCollection<FrameworkElement> _overflowItems = new();

        #region Dependency properties

        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>
        /// Gets or sets the group identity referenced by <see cref="RibbonGroupVariant.GroupKey"/>.
        /// When unset, the header text is used.
        /// </summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Identifies the <see cref="LargeImage"/> dependency property.</summary>
        public static readonly DependencyProperty LargeImageProperty = RibbonItem.LargeImageProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>Gets or sets the image shown on the collapsed group button.</summary>
        [Category("Ribbon")]
        public ImageSource? LargeImage { get => (ImageSource?)GetValue(LargeImageProperty); set => SetValue(LargeImageProperty, value); }

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonGroup));

        /// <summary>Gets or sets a font glyph shown on the collapsed group button when no image is assigned.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>
        /// Identifies the <see cref="Variant"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
            nameof(Variant), typeof(VariantSize), typeof(RibbonGroup),
            new FrameworkPropertyMetadata(VariantSize.Large, FrameworkPropertyMetadataOptions.AffectsMeasure, OnVariantChanged));

        /// <summary>
        /// Gets the size the containing tab assigned to the group. Container managed.
        /// </summary>
        [Browsable(false)]
        public VariantSize Variant
        {
            get => (VariantSize)GetValue(VariantProperty);
            internal set => SetValue(VariantProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsCollapsible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCollapsibleProperty = DependencyProperty.Register(
            nameof(IsCollapsible), typeof(bool), typeof(RibbonGroup),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Gets or sets a value indicating whether the group may be reduced to a collapsed button.
        /// </summary>
        [Category("Layout")]
        [Description("Whether the group may collapse to a single popup button when space runs out.")]
        public bool IsCollapsible
        {
            get => (bool)GetValue(IsCollapsibleProperty);
            set => SetValue(IsCollapsibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ChildOverflowTarget"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ChildOverflowTargetProperty = DependencyProperty.Register(
            nameof(ChildOverflowTarget), typeof(RibbonOverflowTarget), typeof(RibbonGroup),
            new FrameworkPropertyMetadata(RibbonOverflowTarget.Tab, FrameworkPropertyMetadataOptions.AffectsParentMeasure),
            RibbonValidation.IsDefined<RibbonOverflowTarget>);

        /// <summary>
        /// Gets or sets where this group's commands go when they overflow in Simplified mode.
        /// </summary>
        [Category("Layout")]
        public RibbonOverflowTarget ChildOverflowTarget
        {
            get => (RibbonOverflowTarget)GetValue(ChildOverflowTargetProperty);
            set => SetValue(ChildOverflowTargetProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            nameof(IsDropDownOpen), typeof(bool), typeof(RibbonGroup),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged, CoerceIsDropDownOpen));

        /// <summary>
        /// Gets or sets a value indicating whether a collapsed group's popup is open.
        /// </summary>
        [Category("Common")]
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LauncherCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LauncherCommandProperty = DependencyProperty.Register(
            nameof(LauncherCommand), typeof(ICommand), typeof(RibbonGroup), new FrameworkPropertyMetadata(null, OnLauncherChanged));

        /// <summary>
        /// Gets or sets the command run by the dialog launcher in the group's corner.
        /// </summary>
        [Category("Action")]
        public ICommand? LauncherCommand
        {
            get => (ICommand?)GetValue(LauncherCommandProperty);
            set => SetValue(LauncherCommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LauncherCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LauncherCommandParameterProperty = DependencyProperty.Register(
            nameof(LauncherCommandParameter), typeof(object), typeof(RibbonGroup), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the launcher command parameter.
        /// </summary>
        [Category("Action")]
        public object? LauncherCommandParameter
        {
            get => GetValue(LauncherCommandParameterProperty);
            set => SetValue(LauncherCommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowLauncher"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowLauncherProperty = DependencyProperty.Register(
            nameof(ShowLauncher), typeof(bool), typeof(RibbonGroup), new FrameworkPropertyMetadata(false, OnLauncherChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the launcher is shown without a <see cref="LauncherCommand"/>
        /// (handle <see cref="LauncherClick"/> instead).
        /// </summary>
        [Category("Ribbon")]
        public bool ShowLauncher
        {
            get => (bool)GetValue(ShowLauncherProperty);
            set => SetValue(ShowLauncherProperty, value);
        }

        private static readonly DependencyPropertyKey HasLauncherPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasLauncher), typeof(bool), typeof(RibbonGroup), new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="HasLauncher"/> dependency property.</summary>
        public static readonly DependencyProperty HasLauncherProperty = HasLauncherPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether the dialog launcher is shown.</summary>
        [Browsable(false)]
        public bool HasLauncher => (bool)GetValue(HasLauncherProperty);

        /// <summary>
        /// Identifies the <see cref="LauncherToolTip"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LauncherToolTipProperty = DependencyProperty.Register(
            nameof(LauncherToolTip), typeof(object), typeof(RibbonGroup), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the launcher's tooltip, for example a <see cref="RibbonScreenTip"/> describing the dialog.
        /// </summary>
        [Category("Ribbon")]
        public object? LauncherToolTip
        {
            get => GetValue(LauncherToolTipProperty);
            set => SetValue(LauncherToolTipProperty, value);
        }

        private static readonly DependencyPropertyKey HasOverflowItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasOverflowItems), typeof(bool), typeof(RibbonGroup), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies the <see cref="HasOverflowItems"/> dependency property.</summary>
        public static readonly DependencyProperty HasOverflowItemsProperty = HasOverflowItemsPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether Simplified mode moved commands to this group's overflow drop-down.</summary>
        [Browsable(false)]
        public bool HasOverflowItems => (bool)GetValue(HasOverflowItemsProperty);

        /// <summary>
        /// Identifies the <see cref="IsOverflowOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOverflowOpenProperty = DependencyProperty.Register(
            nameof(IsOverflowOpen), typeof(bool), typeof(RibbonGroup), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Gets or sets a value indicating whether the group overflow drop-down is open.
        /// </summary>
        [Browsable(false)]
        public bool IsOverflowOpen
        {
            get => (bool)GetValue(IsOverflowOpenProperty);
            set => SetValue(IsOverflowOpenProperty, value);
        }

        /// <summary>
        /// Gets the generated presentations of this group's overflowed commands (Simplified mode, group target).
        /// </summary>
        public ReadOnlyObservableCollection<FrameworkElement> OverflowItems { get; }

        /// <summary>
        /// Identifies the <see cref="LauncherClick"/> routed event.
        /// </summary>
        public static readonly RoutedEvent LauncherClickEvent = EventManager.RegisterRoutedEvent(
            nameof(LauncherClick), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonGroup));

        /// <summary>
        /// Occurs when the dialog launcher is clicked.
        /// </summary>
        public event RoutedEventHandler LauncherClick
        {
            add => AddHandler(LauncherClickEvent, value);
            remove => RemoveHandler(LauncherClickEvent, value);
        }

        #endregion

        /// <summary>
        /// Gets the variant used to lay out the group's commands: <see cref="VariantSize.Large"/> while the
        /// group is collapsed (its popup shows the full content).
        /// </summary>
        internal VariantSize ContentVariant => Variant == VariantSize.Collapsed ? VariantSize.Large : Variant;

        /// <summary>
        /// Gets the items panel, once generated.
        /// </summary>
        internal RibbonGroupItemsPanel? ItemsPanelInstance { get; set; }

        /// <summary>
        /// Gets the identity used by variant rules: <see cref="Key"/> or the header text.
        /// </summary>
        internal string? LayoutKey => Key ?? Header as string;

        static RibbonGroup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonGroup), new FrameworkPropertyMetadata(typeof(RibbonGroup)));
            FocusableProperty.OverrideMetadata(typeof(RibbonGroup), new FrameworkPropertyMetadata(false));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonGroup"/> class.
        /// </summary>
        public RibbonGroup()
        {
            OverflowItems = new ReadOnlyObservableCollection<FrameworkElement>(_overflowItems);
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_collapsedPopup != null)
            {
                _collapsedPopup.Closed -= OnCollapsedPopupClosed;
                _collapsedPopup.PreviewKeyDown -= OnCollapsedPopupPreviewKeyDown;
            }

            if (_launcherButton != null)
            {
                _launcherButton.Click -= OnLauncherButtonClick;
            }

            // Ensure the content is not left in an old template's popup.
            if (_contentRoot != null && _popupHost != null && _popupHost.Child == _contentRoot)
            {
                _popupHost.Child = null;
            }

            base.OnApplyTemplate();

            _inlineHost = GetTemplateChild(PartInlineHost) as Decorator;
            _popupHost = GetTemplateChild(PartPopupHost) as Decorator;
            _contentRoot = GetTemplateChild(PartContentRoot) as FrameworkElement;
            _collapsedPopup = GetTemplateChild(PartCollapsedPopup) as Popup;
            _collapsedButton = GetTemplateChild(PartCollapsedButton) as System.Windows.Controls.Primitives.ToggleButton;
            _launcherButton = GetTemplateChild(PartLauncherButton) as ButtonBase;

            if (_collapsedPopup != null)
            {
                _collapsedPopup.Closed += OnCollapsedPopupClosed;
                _collapsedPopup.PreviewKeyDown += OnCollapsedPopupPreviewKeyDown;
            }

            if (_launcherButton != null)
            {
                _launcherButton.Click += OnLauncherButtonClick;
            }

            PlaceContent();
        }

        private static void OnVariantChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var group = (RibbonGroup)d;

            // A group leaving the collapsed state must not keep its content in the popup.
            if ((VariantSize)e.NewValue != VariantSize.Collapsed && group.IsDropDownOpen)
            {
                group.SetCurrentValue(IsDropDownOpenProperty, false);
            }

            group.ItemsPanelInstance?.InvalidateMeasure();
        }

        private static object CoerceIsDropDownOpen(DependencyObject d, object value)
        {
            var group = (RibbonGroup)d;
            return (bool)value && group.Variant == VariantSize.Collapsed;
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var group = (RibbonGroup)d;
            var opening = (bool)e.NewValue;

            if (!opening && group._contentRoot is { IsKeyboardFocusWithin: true })
            {
                group._collapsedButton?.Focus();
            }

            group.PlaceContent();

            if (opening && group._collapsedButton is { IsKeyboardFocused: true } && InputManager.Current.MostRecentInputDevice is KeyboardDevice)
            {
                group.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    group._contentRoot?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));
            }

            // Measurement of the panel ignores open popups; re-run layout once the popup is gone.
            if (!opening)
            {
                (VisualTreeHelper.GetParent(group) as UIElement)?.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Moves the content root between the inline host and the popup host. The content stays inline (hidden)
        /// while collapsed so measuring other variants never reparents; it moves only while the popup is open.
        /// </summary>
        private void PlaceContent()
        {
            if (_contentRoot == null || _inlineHost == null || _popupHost == null)
            {
                return;
            }

            var target = IsDropDownOpen ? _popupHost : _inlineHost;
            var other = IsDropDownOpen ? _inlineHost : _popupHost;

            if (target.Child == _contentRoot)
            {
                return;
            }

            // Detach before reparenting: one element never has two visual parents.
            if (other.Child == _contentRoot)
            {
                other.Child = null;
            }

            target.Child = _contentRoot;
            ItemsPanelInstance?.InvalidateMeasure();
        }

        private void OnCollapsedPopupClosed(object? sender, EventArgs e)
        {
            if (IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        private void OnCollapsedPopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape && e.OriginalSource is not MenuItem { IsSubmenuOpen: true })
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
                _collapsedButton?.Focus();
                e.Handled = true;
            }
        }

        private static void OnLauncherChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var group = (RibbonGroup)d;
            group.SetValue(HasLauncherPropertyKey, group.LauncherCommand != null || group.ShowLauncher);
        }

        private void OnLauncherButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            InvokeLauncher();
        }

        /// <summary>
        /// Runs the dialog launcher.
        /// </summary>
        public void InvokeLauncher()
        {
            RaiseEvent(new RoutedEventArgs(LauncherClickEvent, this));
            RibbonCommandHelper.Execute(LauncherCommand, LauncherCommandParameter, null, this);
            RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
        }

        /// <summary>
        /// Sets the overflow presentations shown in the group's overflow drop-down.
        /// </summary>
        internal ObservableCollection<FrameworkElement> MutableOverflowItems => _overflowItems;

        private static readonly DependencyPropertyKey IsInlineEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsInlineEmpty), typeof(bool), typeof(RibbonGroup), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies the <see cref="IsInlineEmpty"/> dependency property.</summary>
        public static readonly DependencyProperty IsInlineEmptyProperty = IsInlineEmptyPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether every command of the group moved to the tab overflow menu in Simplified
        /// mode, in which case the group takes no space.
        /// </summary>
        [Browsable(false)]
        public bool IsInlineEmpty => (bool)GetValue(IsInlineEmptyProperty);

        internal void SetIsInlineEmpty(bool value)
        {
            if (IsInlineEmpty != value)
            {
                SetValue(IsInlineEmptyPropertyKey, value);
            }
        }

        internal void SetHasOverflowItems(bool value)
        {
            if (HasOverflowItems != value)
            {
                SetValue(HasOverflowItemsPropertyKey, value);
            }
        }

        /// <inheritdoc />
        bool IRibbonKeyTipTarget.IsKeyTipTarget => Variant == VariantSize.Collapsed;

        /// <inheritdoc />
        string? IRibbonKeyTipTarget.KeyTipLabel => Header as string;

        /// <inheritdoc />
        UIElement? IRibbonKeyTipTarget.ActivateKeyTip()
        {
            SetCurrentValue(IsDropDownOpenProperty, true);
            return _contentRoot;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGroupAutomationPeer(this);
    }

    /// <summary>
    /// The small button used for a group's dialog launcher and for overflow and display-option buttons.
    /// It participates in key tips using its <see cref="System.Windows.Automation.AutomationProperties.NameProperty"/>.
    /// </summary>
    public class RibbonChromeButton : System.Windows.Controls.Primitives.ToggleButton, IRibbonKeyTipTarget
    {
        /// <summary>
        /// Identifies the <see cref="IsToggle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsToggleProperty = DependencyProperty.Register(
            nameof(IsToggle), typeof(bool), typeof(RibbonChromeButton), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the button toggles (drop-down openers) or acts as a push
        /// button (launchers).
        /// </summary>
        public bool IsToggle
        {
            get => (bool)GetValue(IsToggleProperty);
            set => SetValue(IsToggleProperty, value);
        }

        static RibbonChromeButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonChromeButton), new FrameworkPropertyMetadata(typeof(RibbonChromeButton)));
        }

        /// <inheritdoc />
        protected override void OnToggle()
        {
            if (IsToggle)
            {
                base.OnToggle();
            }
        }

        /// <inheritdoc />
        public bool IsKeyTipTarget => IsVisible && IsEnabled;

        /// <inheritdoc />
        public string? KeyTipLabel => System.Windows.Automation.AutomationProperties.GetName(this);

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            OnClick();
            return null;
        }
    }
}
