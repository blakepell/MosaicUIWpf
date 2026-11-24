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
using System.Collections.Specialized;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides data for <see cref="Ribbon.QuickAccessToolbarItemAdding"/>.
    /// </summary>
    public class QuickAccessToolbarItemAddingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuickAccessToolbarItemAddingEventArgs"/> class.
        /// </summary>
        public QuickAccessToolbarItemAddingEventArgs(string key, IRibbonControl source, FrameworkElement presentation)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(presentation);
            Key = key;
            Source = source;
            _presentation = presentation;
        }

        private FrameworkElement _presentation;

        /// <summary>Gets the command key being added.</summary>
        public string Key { get; }

        /// <summary>Gets the source command.</summary>
        public IRibbonControl Source { get; }

        /// <summary>
        /// Gets or sets the presentation that will be inserted; replace it to customize the QAT button. The
        /// replacement must not already have a parent.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The value already has a logical or visual parent.</exception>
        public FrameworkElement Presentation
        {
            get => _presentation;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (!ReferenceEquals(value, _presentation) && (value.Parent != null || VisualTreeHelper.GetParent(value) != null))
                {
                    throw new ArgumentException("The Quick Access Toolbar presentation must not already have a parent.", nameof(value));
                }

                _presentation = value;
            }
        }
    }

    /// <summary>
    /// Provides data for Quick Access Toolbar membership notifications.
    /// </summary>
    public class QuickAccessToolbarItemEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuickAccessToolbarItemEventArgs"/> class.
        /// </summary>
        public QuickAccessToolbarItemEventArgs(string key, FrameworkElement presentation)
        {
            Key = key;
            Presentation = presentation;
        }

        /// <summary>Gets the command key.</summary>
        public string Key { get; }

        /// <summary>Gets the Quick Access Toolbar presentation.</summary>
        public FrameworkElement Presentation { get; }
    }

    /// <summary>
    /// A Microsoft Office–style command Ribbon. The Ribbon coordinates tab selection, the effective layout
    /// mode, minimization, size collapse, contextual tabs, the Backstage, the Quick Access Toolbar, key tips
    /// and focus; it never executes business logic itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Ribbon is a focus scope, so routed commands from its buttons route to the document's focused
    /// element exactly like menu and toolbar commands, and mouse invocation returns keyboard focus to the
    /// document afterwards.
    /// </para>
    /// <para>
    /// Desired preferences (<see cref="IsMinimized"/>, <see cref="LayoutMode"/>) are kept separate from
    /// temporary constraints (<see cref="IsCollapsed"/>, computed from the host size).
    /// </para>
    /// </remarks>
    [TemplatePart(Name = PartTabHeaderItemsControl, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PartApplicationButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartTabRowToolbarPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartItemsPresenter, Type = typeof(ItemsPresenter))]
    [TemplatePart(Name = PartBody, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartBodyHost, Type = typeof(Decorator))]
    [TemplatePart(Name = PartMinimizedPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartMinimizedPopupHost, Type = typeof(Decorator))]
    [TemplatePart(Name = PartQatAbovePresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartQatBelowPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartFooterPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartDisplayOptionsButton, Type = typeof(ButtonBase))]
    [DefaultEvent(nameof(SelectionChanged))]
    [DefaultProperty(nameof(Items))]
    public class Ribbon : Selector
    {
        private const string PartTabHeaderItemsControl = "PART_TabHeaderItemsControl";
        private const string PartApplicationButton = "PART_ApplicationButton";
        private const string PartTabRowToolbarPresenter = "PART_TabRowToolbarPresenter";
        private const string PartItemsPresenter = "PART_ItemsPresenter";
        private const string PartBody = "PART_Body";
        private const string PartBodyHost = "PART_BodyHost";
        private const string PartMinimizedPopup = "PART_MinimizedPopup";
        private const string PartMinimizedPopupHost = "PART_MinimizedPopupHost";
        private const string PartQatAbovePresenter = "PART_QatAbovePresenter";
        private const string PartQatBelowPresenter = "PART_QatBelowPresenter";
        private const string PartFooterPresenter = "PART_FooterPresenter";
        private const string PartDisplayOptionsButton = "PART_DisplayOptionsButton";

        private readonly ObservableCollection<RibbonTabHeader> _headers = new();
        private readonly ObservableCollection<FrameworkElement> _qatItems = new();
        private readonly List<string> _qatKeys = new();
        private readonly RibbonQuickAccessToolbar _qat;

        private ButtonBase? _applicationButton;
        private FrameworkElement? _body;
        private Decorator? _bodyHost;
        private Decorator? _minimizedPopupHost;
        private Popup? _minimizedPopup;
        private ContentPresenter? _qatAbove;
        private ContentPresenter? _qatBelow;
        private ContentControl? _externalQatHost;
        private ButtonBase? _displayOptionsButton;
        private Window? _window;
        private KeyTipManager? _keyTips;
        private bool _updatingHeaders;

        #region Routed events and attached properties

        /// <summary>
        /// Identifies the CommandInvoked routed event, raised by every Ribbon command presentation after it
        /// performs its action. The Ribbon uses it to dismiss transient surfaces and restore document focus.
        /// </summary>
        public static readonly RoutedEvent CommandInvokedEvent = EventManager.RegisterRoutedEvent(
            "CommandInvoked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Ribbon));

        /// <summary>
        /// Occurs after any command within the Ribbon is invoked.
        /// </summary>
        public event RoutedEventHandler CommandInvoked
        {
            add => AddHandler(CommandInvokedEvent, value);
            remove => RemoveHandler(CommandInvokedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="LayoutMode"/> property. It is inherited by the Ribbon's content (including
        /// popups), so templates and panels can read it with <see cref="GetLayoutMode"/>.
        /// </summary>
        public static readonly DependencyProperty LayoutModeProperty = DependencyProperty.RegisterAttached(
            nameof(LayoutMode), typeof(RibbonLayoutMode), typeof(Ribbon),
            new FrameworkPropertyMetadata(RibbonLayoutMode.Classic,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnLayoutModeChanged),
            RibbonValidation.IsDefined<RibbonLayoutMode>);

        /// <summary>Gets the effective layout mode of an element inside a Ribbon.</summary>
        public static RibbonLayoutMode GetLayoutMode(DependencyObject element) => (RibbonLayoutMode)element.GetValue(LayoutModeProperty);

        /// <summary>Sets the layout mode. Set it on the <see cref="Ribbon"/>.</summary>
        public static void SetLayoutMode(DependencyObject element, RibbonLayoutMode value) => element.SetValue(LayoutModeProperty, value);

        /// <summary>
        /// Identifies the <see cref="Density"/> property, inherited by the Ribbon's content.
        /// </summary>
        public static readonly DependencyProperty DensityProperty = DependencyProperty.RegisterAttached(
            nameof(Density), typeof(UserInterfaceDensity), typeof(Ribbon),
            new FrameworkPropertyMetadata(UserInterfaceDensity.Standard,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnDensityChanged),
            RibbonValidation.IsDefined<UserInterfaceDensity>);

        /// <summary>Gets the effective density of an element inside a Ribbon.</summary>
        public static UserInterfaceDensity GetDensity(DependencyObject element) => (UserInterfaceDensity)element.GetValue(DensityProperty);

        /// <summary>Sets the density. Set it on the <see cref="Ribbon"/>.</summary>
        public static void SetDensity(DependencyObject element, UserInterfaceDensity value) => element.SetValue(DensityProperty, value);

        /// <summary>
        /// Toggles <see cref="IsMinimized"/>. Bound to Ctrl+F1.
        /// </summary>
        public static RoutedUICommand ToggleMinimizeCommand { get; } = new("Collapse the Ribbon", nameof(ToggleMinimizeCommand), typeof(Ribbon),
            new InputGestureCollection { new KeyGesture(Key.F1, ModifierKeys.Control) });

        #endregion

        #region Dependency properties

        /// <summary>Gets or sets the layout mode (a user preference).</summary>
        [Category("Ribbon")]
        [Description("Classic (multi-row groups) or Simplified (single row).")]
        public RibbonLayoutMode LayoutMode
        {
            get => (RibbonLayoutMode)GetValue(LayoutModeProperty);
            set => SetValue(LayoutModeProperty, value);
        }

        /// <summary>Gets or sets the command density (a user preference independent of the layout mode).</summary>
        [Category("Ribbon")]
        public UserInterfaceDensity Density
        {
            get => (UserInterfaceDensity)GetValue(DensityProperty);
            set => SetValue(DensityProperty, value);
        }

        /// <summary>Identifies the <see cref="CanChangeLayoutMode"/> dependency property.</summary>
        public static readonly DependencyProperty CanChangeLayoutModeProperty = DependencyProperty.Register(
            nameof(CanChangeLayoutMode), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true));

        /// <summary>Gets or sets a value indicating whether the user may switch between Classic and Simplified.</summary>
        [Category("Ribbon")]
        public bool CanChangeLayoutMode
        {
            get => (bool)GetValue(CanChangeLayoutModeProperty);
            set => SetValue(CanChangeLayoutModeProperty, value);
        }

        /// <summary>Identifies the <see cref="IsMinimized"/> dependency property.</summary>
        public static readonly DependencyProperty IsMinimizedProperty = DependencyProperty.Register(
            nameof(IsMinimized), typeof(bool), typeof(Ribbon),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsMinimizedChanged, CoerceIsMinimized));

        /// <summary>
        /// Gets or sets a value indicating whether only the tab headers are shown (a user preference). Selecting a
        /// tab while minimized opens its commands transiently.
        /// </summary>
        [Category("Ribbon")]
        public bool IsMinimized
        {
            get => (bool)GetValue(IsMinimizedProperty);
            set => SetValue(IsMinimizedProperty, value);
        }

        /// <summary>Identifies the <see cref="IsMinimizable"/> dependency property.</summary>
        public static readonly DependencyProperty IsMinimizableProperty = DependencyProperty.Register(
            nameof(IsMinimizable), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, (d, _) => d.CoerceValue(IsMinimizedProperty)));

        /// <summary>Gets or sets a value indicating whether the user may minimize the Ribbon.</summary>
        [Category("Ribbon")]
        public bool IsMinimizable
        {
            get => (bool)GetValue(IsMinimizableProperty);
            set => SetValue(IsMinimizableProperty, value);
        }

        /// <summary>Identifies the <see cref="IsMinimizedPopupOpen"/> dependency property.</summary>
        public static readonly DependencyProperty IsMinimizedPopupOpenProperty = DependencyProperty.Register(
            nameof(IsMinimizedPopupOpen), typeof(bool), typeof(Ribbon),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsMinimizedPopupOpenChanged, CoerceIsMinimizedPopupOpen));

        /// <summary>Gets or sets a value indicating whether a minimized Ribbon's selected tab is shown transiently.</summary>
        [Browsable(false)]
        public bool IsMinimizedPopupOpen
        {
            get => (bool)GetValue(IsMinimizedPopupOpenProperty);
            set => SetValue(IsMinimizedPopupOpenProperty, value);
        }

        private static readonly DependencyPropertyKey IsCollapsedPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsCollapsed), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(false, OnIsCollapsedChanged));

        /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property.</summary>
        public static readonly DependencyProperty IsCollapsedProperty = IsCollapsedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether the whole Ribbon is hidden because the host is smaller than
        /// <see cref="CollapseThresholdSize"/>. This is a temporary constraint and never changes <see cref="IsMinimized"/>.
        /// </summary>
        [Browsable(false)]
        public bool IsCollapsed => (bool)GetValue(IsCollapsedProperty);

        /// <summary>Identifies the <see cref="IsCollapsible"/> dependency property.</summary>
        public static readonly DependencyProperty IsCollapsibleProperty = DependencyProperty.Register(
            nameof(IsCollapsible), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true, (d, _) => ((Ribbon)d).UpdateIsCollapsed()));

        /// <summary>Gets or sets a value indicating whether the Ribbon hides itself below the threshold size.</summary>
        [Category("Ribbon")]
        public bool IsCollapsible
        {
            get => (bool)GetValue(IsCollapsibleProperty);
            set => SetValue(IsCollapsibleProperty, value);
        }

        /// <summary>Identifies the <see cref="CollapseThresholdSize"/> dependency property.</summary>
        public static readonly DependencyProperty CollapseThresholdSizeProperty = DependencyProperty.Register(
            nameof(CollapseThresholdSize), typeof(Size), typeof(Ribbon), new FrameworkPropertyMetadata(new Size(270, 170), (d, _) => ((Ribbon)d).UpdateIsCollapsed()),
            RibbonValidation.IsFiniteSize);

        /// <summary>
        /// Gets or sets the host size (Ribbon width × window height, in DIPs) below which the Ribbon collapses.
        /// </summary>
        [Category("Ribbon")]
        public Size CollapseThresholdSize
        {
            get => (Size)GetValue(CollapseThresholdSizeProperty);
            set => SetValue(CollapseThresholdSizeProperty, value);
        }

        /// <summary>Identifies the <see cref="Backstage"/> dependency property.</summary>
        public static readonly DependencyProperty BackstageProperty = DependencyProperty.Register(
            nameof(Backstage), typeof(RibbonBackstage), typeof(Ribbon), new FrameworkPropertyMetadata(null, OnBackstageChanged));

        /// <summary>Gets or sets the full-window File surface opened by the application button.</summary>
        [Category("Ribbon")]
        public RibbonBackstage? Backstage
        {
            get => (RibbonBackstage?)GetValue(BackstageProperty);
            set => SetValue(BackstageProperty, value);
        }

        /// <summary>Identifies the <see cref="ApplicationButtonContent"/> dependency property.</summary>
        public static readonly DependencyProperty ApplicationButtonContentProperty = DependencyProperty.Register(
            nameof(ApplicationButtonContent), typeof(object), typeof(Ribbon), new FrameworkPropertyMetadata("File"));

        /// <summary>Gets or sets the application button content.</summary>
        [Category("Ribbon")]
        public object? ApplicationButtonContent
        {
            get => GetValue(ApplicationButtonContentProperty);
            set => SetValue(ApplicationButtonContentProperty, value);
        }

        private static readonly DependencyPropertyKey IsBackstageOpenPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsBackstageOpen), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="IsBackstageOpen"/> dependency property.</summary>
        public static readonly DependencyProperty IsBackstageOpenProperty = IsBackstageOpenPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether the Backstage is open.</summary>
        [Browsable(false)]
        public bool IsBackstageOpen => (bool)GetValue(IsBackstageOpenProperty);

        /// <summary>
        /// Identifies the <see cref="QuickAccessToolbarHost"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty QuickAccessToolbarHostProperty = DependencyProperty.Register(
            nameof(QuickAccessToolbarHost), typeof(ContentControl), typeof(Ribbon),
            new FrameworkPropertyMetadata(null, OnQuickAccessToolbarHostChanged));

        /// <summary>
        /// Gets or sets an optional external host for the above-ribbon Quick Access Toolbar, for example in
        /// <see cref="WindowTitleBar.LeftContent"/>. Use an empty, dedicated host in the same window.
        /// When the location is Below, the toolbar returns to the Ribbon. Set to null to restore internal hosting.
        /// </summary>
        [Category("Quick Access Toolbar")]
        public ContentControl? QuickAccessToolbarHost
        {
            get => (ContentControl?)GetValue(QuickAccessToolbarHostProperty);
            set => SetValue(QuickAccessToolbarHostProperty, value);
        }

        private static void OnQuickAccessToolbarHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Ribbon)d).PlaceQuickAccessToolbar();
        }

        /// <summary>Identifies the <see cref="QuickAccessToolbarLocation"/> dependency property.</summary>
        public static readonly DependencyProperty QuickAccessToolbarLocationProperty = DependencyProperty.Register(
            nameof(QuickAccessToolbarLocation), typeof(QuickAccessToolbarLocation), typeof(Ribbon),
            new FrameworkPropertyMetadata(QuickAccessToolbarLocation.Above, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnQuickAccessToolbarLocationChanged),
            RibbonValidation.IsDefined<QuickAccessToolbarLocation>);

        /// <summary>Gets or sets where the Quick Access Toolbar is shown.</summary>
        [Category("Quick Access Toolbar")]
        public QuickAccessToolbarLocation QuickAccessToolbarLocation
        {
            get => (QuickAccessToolbarLocation)GetValue(QuickAccessToolbarLocationProperty);
            set => SetValue(QuickAccessToolbarLocationProperty, value);
        }

        /// <summary>Identifies the <see cref="QuickAccessToolbarMode"/> dependency property.</summary>
        public static readonly DependencyProperty QuickAccessToolbarModeProperty = DependencyProperty.Register(
            nameof(QuickAccessToolbarMode), typeof(QuickAccessToolbarMode), typeof(Ribbon),
            new FrameworkPropertyMetadata(QuickAccessToolbarMode.Visible, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnQuickAccessToolbarModeChanged),
            RibbonValidation.IsDefined<QuickAccessToolbarMode>);

        /// <summary>Gets or sets whether the Quick Access Toolbar is visible and whether the user may change that.</summary>
        [Category("Quick Access Toolbar")]
        public QuickAccessToolbarMode QuickAccessToolbarMode
        {
            get => (QuickAccessToolbarMode)GetValue(QuickAccessToolbarModeProperty);
            set => SetValue(QuickAccessToolbarModeProperty, value);
        }

        /// <summary>Identifies the <see cref="QuickAccessToolbarShowLabels"/> dependency property.</summary>
        public static readonly DependencyProperty QuickAccessToolbarShowLabelsProperty = DependencyProperty.Register(
            nameof(QuickAccessToolbarShowLabels), typeof(bool), typeof(Ribbon),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPreferenceChanged));

        /// <summary>Gets or sets a value indicating whether Quick Access Toolbar buttons show their labels.</summary>
        [Category("Quick Access Toolbar")]
        public bool QuickAccessToolbarShowLabels
        {
            get => (bool)GetValue(QuickAccessToolbarShowLabelsProperty);
            set => SetValue(QuickAccessToolbarShowLabelsProperty, value);
        }

        private static readonly DependencyPropertyKey IsQuickAccessToolbarVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsQuickAccessToolbarVisible), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true));

        /// <summary>Identifies the <see cref="IsQuickAccessToolbarVisible"/> dependency property.</summary>
        public static readonly DependencyProperty IsQuickAccessToolbarVisibleProperty = IsQuickAccessToolbarVisiblePropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether the Quick Access Toolbar is currently shown.</summary>
        [Browsable(false)]
        public bool IsQuickAccessToolbarVisible => (bool)GetValue(IsQuickAccessToolbarVisibleProperty);

        /// <summary>Identifies the <see cref="TabRowToolbarContent"/> dependency property.</summary>
        public static readonly DependencyProperty TabRowToolbarContentProperty = DependencyProperty.Register(
            nameof(TabRowToolbarContent), typeof(object), typeof(Ribbon), new FrameworkPropertyMetadata(null, OnLogicalContentChanged));

        /// <summary>Gets or sets optional content at the right end of the tab strip (for example Comments and Share buttons).</summary>
        [Category("Ribbon")]
        public object? TabRowToolbarContent
        {
            get => GetValue(TabRowToolbarContentProperty);
            set => SetValue(TabRowToolbarContentProperty, value);
        }

        /// <summary>Identifies the <see cref="FooterContent"/> dependency property.</summary>
        public static readonly DependencyProperty FooterContentProperty = DependencyProperty.Register(
            nameof(FooterContent), typeof(object), typeof(Ribbon), new FrameworkPropertyMetadata(null, OnLogicalContentChanged));

        /// <summary>Gets or sets optional content below the command card (for example a message bar).</summary>
        [Category("Ribbon")]
        public object? FooterContent
        {
            get => GetValue(FooterContentProperty);
            set => SetValue(FooterContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BodyBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BodyBackgroundProperty = DependencyProperty.Register(
            nameof(BodyBackground), typeof(Brush), typeof(Ribbon), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the background brush for the command area below the tab strip.
        /// </summary>
        [Category("Appearance")]
        public Brush? BodyBackground
        {
            get => (Brush?)GetValue(BodyBackgroundProperty);
            set => SetValue(BodyBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BodyBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BodyBorderBrushProperty = DependencyProperty.Register(
            nameof(BodyBorderBrush), typeof(Brush), typeof(Ribbon), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the border brush for the command area.
        /// </summary>
        [Category("Appearance")]
        public Brush? BodyBorderBrush
        {
            get => (Brush?)GetValue(BodyBorderBrushProperty);
            set => SetValue(BodyBorderBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BodyBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BodyBorderThicknessProperty = DependencyProperty.Register(
            nameof(BodyBorderThickness), typeof(Thickness), typeof(Ribbon), new FrameworkPropertyMetadata(new Thickness(1)),
            Border.BorderThicknessProperty.IsValidValue);

        /// <summary>
        /// Gets or sets the border thickness for each edge of the command area.
        /// </summary>
        [Category("Appearance")]
        public Thickness BodyBorderThickness
        {
            get => (Thickness)GetValue(BodyBorderThicknessProperty);
            set => SetValue(BodyBorderThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BodyMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BodyMarginProperty = DependencyProperty.Register(
            nameof(BodyMargin), typeof(Thickness), typeof(Ribbon), new FrameworkPropertyMetadata(new Thickness(6, 0, 6, 6)),
            MarginProperty.IsValidValue);

        /// <summary>
        /// Gets or sets the space around the command area.
        /// </summary>
        [Category("Appearance")]
        public Thickness BodyMargin
        {
            get => (Thickness)GetValue(BodyMarginProperty);
            set => SetValue(BodyMarginProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BodyCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BodyCornerRadiusProperty = DependencyProperty.Register(
            nameof(BodyCornerRadius), typeof(CornerRadius), typeof(Ribbon), new FrameworkPropertyMetadata(new CornerRadius(8)),
            Border.CornerRadiusProperty.IsValidValue);

        /// <summary>
        /// Gets or sets the corner radii of the command area border.
        /// </summary>
        [Category("Appearance")]
        public CornerRadius BodyCornerRadius
        {
            get => (CornerRadius)GetValue(BodyCornerRadiusProperty);
            set => SetValue(BodyCornerRadiusProperty, value);
        }

        /// <summary>Identifies the <see cref="ShowDisplayOptionsButton"/> dependency property.</summary>
        public static readonly DependencyProperty ShowDisplayOptionsButtonProperty = DependencyProperty.Register(
            nameof(ShowDisplayOptionsButton), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true));

        /// <summary>Gets or sets a value indicating whether the Ribbon display options button is shown.</summary>
        [Category("Ribbon")]
        public bool ShowDisplayOptionsButton
        {
            get => (bool)GetValue(ShowDisplayOptionsButtonProperty);
            set => SetValue(ShowDisplayOptionsButtonProperty, value);
        }

        /// <summary>Identifies the <see cref="TabIndicatorAnimation"/> dependency property.</summary>
        public static readonly DependencyProperty TabIndicatorAnimationProperty = DependencyProperty.Register(
            nameof(TabIndicatorAnimation), typeof(RibbonTabIndicatorAnimation), typeof(Ribbon),
            new FrameworkPropertyMetadata(RibbonTabIndicatorAnimation.Expand),
            RibbonValidation.IsDefined<RibbonTabIndicatorAnimation>);

        /// <summary>
        /// Gets or sets the animation the selected-tab indicator plays when the selected tab changes. Animations are
        /// skipped when Windows client-area animations are turned off. The default is
        /// <see cref="RibbonTabIndicatorAnimation.Expand"/>.
        /// </summary>
        [Category("Ribbon")]
        [Description("The animation the selected-tab underline plays when the selected tab changes.")]
        public RibbonTabIndicatorAnimation TabIndicatorAnimation
        {
            get => (RibbonTabIndicatorAnimation)GetValue(TabIndicatorAnimationProperty);
            set => SetValue(TabIndicatorAnimationProperty, value);
        }

        /// <summary>Identifies the <see cref="AreKeyTipsEnabled"/> dependency property.</summary>
        public static readonly DependencyProperty AreKeyTipsEnabledProperty = DependencyProperty.Register(
            nameof(AreKeyTipsEnabled), typeof(bool), typeof(Ribbon), new FrameworkPropertyMetadata(true));

        /// <summary>Gets or sets a value indicating whether Alt / F10 shows key tips.</summary>
        [Category("Ribbon")]
        public bool AreKeyTipsEnabled
        {
            get => (bool)GetValue(AreKeyTipsEnabledProperty);
            set => SetValue(AreKeyTipsEnabledProperty, value);
        }

        /// <summary>Identifies the <see cref="ContextualTabGroups"/> dependency property.</summary>
        public static readonly DependencyProperty ContextualTabGroupsProperty = DependencyProperty.Register(
            nameof(ContextualTabGroups), typeof(FreezableCollection<RibbonContextualTabGroup>), typeof(Ribbon),
            new FrameworkPropertyMetadata(null, OnContextualTabGroupsChanged));

        /// <summary>Gets the contextual tab groups. Tabs join a group through <see cref="RibbonTabItem.ContextualTabGroupKey"/>.</summary>
        [Category("Ribbon")]
        public FreezableCollection<RibbonContextualTabGroup> ContextualTabGroups
        {
            get => (FreezableCollection<RibbonContextualTabGroup>)GetValue(ContextualTabGroupsProperty);
            set => SetValue(ContextualTabGroupsProperty, value);
        }

        #endregion

        #region Events

        /// <summary>Occurs when <see cref="IsMinimized"/> changes.</summary>
        public event EventHandler? IsMinimizedChanged;

        /// <summary>Occurs when <see cref="IsCollapsed"/> changes.</summary>
        public event EventHandler? IsCollapsedChanged;

        /// <summary>Occurs when <see cref="LayoutMode"/> changes.</summary>
        public event EventHandler? LayoutModeChanged;

        /// <summary>Occurs before a command is added to the Quick Access Toolbar. Set <c>Cancel</c> to prevent it.</summary>
        public event EventHandler<QuickAccessToolbarItemAddingEventArgs>? QuickAccessToolbarItemAdding;

        /// <summary>Occurs after a command is added to the Quick Access Toolbar.</summary>
        public event EventHandler<QuickAccessToolbarItemEventArgs>? QuickAccessToolbarItemAdded;

        /// <summary>Occurs after a command is removed from the Quick Access Toolbar.</summary>
        public event EventHandler<QuickAccessToolbarItemEventArgs>? QuickAccessToolbarItemRemoved;

        /// <summary>Occurs when <see cref="QuickAccessToolbarLocation"/> changes.</summary>
        public event EventHandler? QuickAccessToolbarLocationChanged;

        /// <summary>Occurs when <see cref="QuickAccessToolbarMode"/> changes.</summary>
        public event EventHandler? QuickAccessToolbarModeChanged;

        /// <summary>
        /// Occurs when a persisted preference changes (mode, density, minimized state or Quick Access Toolbar),
        /// so hosts can save with <see cref="RibbonSerializer"/>.
        /// </summary>
        public event EventHandler? PreferencesChanged;

        #endregion

        /// <summary>Gets the generated tab headers, ordinary tabs first and contextual tabs last.</summary>
        public ReadOnlyObservableCollection<RibbonTabHeader> TabHeaders { get; }

        /// <summary>Gets the Quick Access Toolbar presentations, in order.</summary>
        public ReadOnlyObservableCollection<FrameworkElement> QuickAccessToolbarItems { get; }

        /// <summary>Gets the ordered command keys in the Quick Access Toolbar.</summary>
        public IReadOnlyList<string> QuickAccessToolbarKeys => _qatKeys;

        /// <summary>Gets the Quick Access Toolbar control.</summary>
        public RibbonQuickAccessToolbar QuickAccessToolbar => _qat;

        /// <summary>Gets the selected tab container.</summary>
        public RibbonTabItem? SelectedTab => SelectedIndex >= 0 ? ItemContainerGenerator.ContainerFromIndex(SelectedIndex) as RibbonTabItem ?? SelectedItem as RibbonTabItem : null;

        /// <summary>Gets the key tip service for this Ribbon, once loaded.</summary>
        public KeyTipManager? KeyTips => _keyTips;

        static Ribbon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Ribbon), new FrameworkPropertyMetadata(typeof(Ribbon)));
            FocusManager.IsFocusScopeProperty.OverrideMetadata(typeof(Ribbon), new FrameworkPropertyMetadata(true));
            FocusableProperty.OverrideMetadata(typeof(Ribbon), new FrameworkPropertyMetadata(false));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(Ribbon), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(Ribbon), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
            CommandManager.RegisterClassCommandBinding(typeof(Ribbon), new CommandBinding(ToggleMinimizeCommand, OnToggleMinimize, OnCanToggleMinimize));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ribbon"/> class.
        /// </summary>
        public Ribbon()
        {
            TabHeaders = new ReadOnlyObservableCollection<RibbonTabHeader>(_headers);
            QuickAccessToolbarItems = new ReadOnlyObservableCollection<FrameworkElement>(_qatItems);
            _qat = new RibbonQuickAccessToolbar(this) { ItemsSource = QuickAccessToolbarItems };
            AddLogicalChild(_qat);
            _qat.ContextMenuOpening += (sender, e) =>
            {
                if (_externalQatHost != null)
                {
                    OnRibbonContextMenuOpening(sender, e);
                }
            };

            SetCurrentValue(ContextualTabGroupsProperty, new FreezableCollection<RibbonContextualTabGroup>());
            ContextMenu = new RibbonContextMenu();

            AddHandler(CommandInvokedEvent, new RoutedEventHandler(OnCommandInvoked));
            AddHandler(ContextMenuOpeningEvent, new ContextMenuEventHandler(OnRibbonContextMenuOpening));
            ItemContainerGenerator.StatusChanged += (_, _) =>
            {
                if (ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                {
                    UpdateTabHeaders();
                }
            };

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += (_, _) => UpdateIsCollapsed();
        }

        #region Logical tree

        /// <inheritdoc />
        protected override System.Collections.IEnumerator LogicalChildren
        {
            get
            {
                var children = new List<object>();
                var baseChildren = base.LogicalChildren;
                while (baseChildren != null && baseChildren.MoveNext())
                {
                    children.Add(baseChildren.Current);
                }

                if (LogicalTreeHelper.GetParent(_qat) == this)
                {
                    children.Add(_qat);
                }
                if (TabRowToolbarContent is DependencyObject tabRow && LogicalTreeHelper.GetParent(tabRow) == this)
                {
                    children.Add(tabRow);
                }

                if (FooterContent is DependencyObject footer && LogicalTreeHelper.GetParent(footer) == this)
                {
                    children.Add(footer);
                }

                if (Backstage != null)
                {
                    children.Add(Backstage);
                }

                return children.GetEnumerator();
            }
        }

        private static void OnLogicalContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Optional regions are logical children of the Ribbon so resources, names and routed events resolve
            // through it; changing them never touches tab selection.
            var ribbon = (Ribbon)d;
            if (e.OldValue is DependencyObject oldChild && LogicalTreeHelper.GetParent(oldChild) == ribbon)
            {
                ribbon.RemoveLogicalChild(oldChild);
            }

            if (e.NewValue is DependencyObject newChild && LogicalTreeHelper.GetParent(newChild) == null)
            {
                ribbon.AddLogicalChild(newChild);
            }
        }

        #endregion

        #region Item containers and headers

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonTabItem;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new RibbonTabItem();

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is RibbonTabItem tab && tab != item && tab.Header == null)
            {
                tab.SetCurrentValue(HeaderedItemsControl.HeaderProperty, item);
            }
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateTabHeaders();
        }

        private IEnumerable<RibbonTabItem> Tabs()
        {
            for (var i = 0; i < Items.Count; i++)
            {
                if ((ItemContainerGenerator.ContainerFromIndex(i) ?? Items[i]) is RibbonTabItem tab)
                {
                    yield return tab;
                }
            }
        }

        private RibbonContextualTabGroup? FindContextualGroup(string? key)
        {
            return key == null ? null : ContextualTabGroups?.FirstOrDefault(g => g.Key == key);
        }

        /// <summary>
        /// Gets or sets the header whose selection indicator was shown last; the next selected header's indicator
        /// travels from it.
        /// </summary>
        internal RibbonTabHeader? IndicatorHeader { get; set; }

        /// <summary>
        /// Rebuilds header order and visibility, then applies the selection fallback policy.
        /// </summary>
        internal void UpdateTabHeaders()
        {
            if (_updatingHeaders)
            {
                return;
            }

            _updatingHeaders = true;
            try
            {
                var groups = ContextualTabGroups?.ToList() ?? new List<RibbonContextualTabGroup>();
                var tabs = Tabs().ToList();
                var ordered = tabs.Where(t => t.ContextualTabGroupKey == null)
                    .Concat(groups.SelectMany(g => tabs.Where(t => t.ContextualTabGroupKey == g.Key)))
                    .Concat(tabs.Where(t => t.ContextualTabGroupKey != null && FindContextualGroup(t.ContextualTabGroupKey) == null))
                    .ToList();

                // Drop headers of removed tabs.
                for (var i = _headers.Count - 1; i >= 0; i--)
                {
                    if (!ordered.Contains(_headers[i].Tab))
                    {
                        BindingOperations.ClearAllBindings(_headers[i]);
                        _headers[i].Tab.TabHeader = null;
                        _headers.RemoveAt(i);
                    }
                }

                string? previousGroupKey = null;
                for (var i = 0; i < ordered.Count; i++)
                {
                    var tab = ordered[i];
                    var header = tab.TabHeader ?? new RibbonTabHeader(tab);
                    var index = _headers.IndexOf(header);
                    if (index < 0)
                    {
                        _headers.Insert(i, header);
                    }
                    else if (index != i)
                    {
                        _headers.Move(index, i);
                    }

                    var group = FindContextualGroup(tab.ContextualTabGroupKey);
                    var visible = tab.Visibility == Visibility.Visible && (tab.ContextualTabGroupKey == null || group is { IsActive: true });
                    header.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    header.IsContextual = tab.ContextualTabGroupKey != null;
                    header.ContextualBrush = group?.Brush;
                    header.IsFirstContextual = header.IsContextual && visible && tab.ContextualTabGroupKey != previousGroupKey;
                    if (header.IsContextual && group != null)
                    {
                        header.ToolTip = group.Header;
                    }

                    if (visible)
                    {
                        previousGroupKey = tab.ContextualTabGroupKey;
                    }

                    header.IsSelected = tab.IsSelected;
                }
            }
            finally
            {
                _updatingHeaders = false;
            }

            EnsureValidSelection();
        }

        private static bool IsTabAvailable(RibbonTabItem tab) => tab.TabHeader is { Visibility: Visibility.Visible } && tab.IsEnabled;

        /// <summary>
        /// Selection fallback: when the selected tab is hidden (for example its context was deactivated), select
        /// the first visible ordinary tab, then any visible tab, otherwise clear selection.
        /// </summary>
        private void EnsureValidSelection()
        {
            var selected = SelectedTab;
            if (selected != null && IsTabAvailable(selected))
            {
                return;
            }

            if (selected != null)
            {
                selected.CloseTransientSurfaces();
            }

            var fallback = _headers.Select(h => h.Tab).FirstOrDefault(t => t.ContextualTabGroupKey == null && IsTabAvailable(t))
                           ?? _headers.Select(h => h.Tab).FirstOrDefault(IsTabAvailable);

            if (fallback != null)
            {
                SelectTab(fallback, openWhenMinimized: false);
            }
            else if (SelectedIndex != -1)
            {
                SetCurrentValue(SelectedIndexProperty, -1);
            }
        }

        private static void OnContextualTabGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            if (e.OldValue is FreezableCollection<RibbonContextualTabGroup> oldCollection)
            {
                oldCollection.Changed -= ribbon.OnContextualTabGroupsContentChanged;
            }

            if (e.NewValue is FreezableCollection<RibbonContextualTabGroup> newCollection)
            {
                newCollection.Changed += ribbon.OnContextualTabGroupsContentChanged;
            }

            ribbon.UpdateTabHeaders();
        }

        private void OnContextualTabGroupsContentChanged(object? sender, EventArgs e)
        {
            // Deactivating a context closes its surfaces and moves selection before anything re-renders.
            Dispatcher.VerifyAccess();
            UpdateTabHeaders();
        }

        #endregion

        #region Selection

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            foreach (var removed in e.RemovedItems)
            {
                if ((ItemContainerGenerator.ContainerFromItem(removed) ?? removed) is RibbonTabItem tab)
                {
                    tab.CloseTransientSurfaces();
                }
            }

            base.OnSelectionChanged(e);

            foreach (var header in _headers)
            {
                header.IsSelected = header.Tab.IsSelected;
            }

            if (SelectedTab is { } selected && AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected))
            {
                UIElementAutomationPeer.FromElement(selected.TabHeader ?? (UIElement)selected)?.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
            }
        }

        /// <summary>
        /// Selects a tab. When the Ribbon is minimized and <paramref name="openWhenMinimized"/> is set, the tab's
        /// commands open transiently.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="tab"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="tab"/> does not belong to this Ribbon.</exception>
        public void SelectTab(RibbonTabItem tab, bool openWhenMinimized = false)
        {
            ArgumentNullException.ThrowIfNull(tab);

            var index = ItemContainerGenerator.IndexFromContainer(tab);
            if (index < 0)
            {
                index = Items.IndexOf(tab);
            }

            if (index < 0)
            {
                throw new ArgumentException("The tab does not belong to this Ribbon.", nameof(tab));
            }

            if (SelectedIndex != index)
            {
                SetCurrentValue(SelectedIndexProperty, index);
            }

            if (openWhenMinimized && IsMinimized)
            {
                SetCurrentValue(IsMinimizedPopupOpenProperty, true);
            }
        }

        /// <summary>
        /// Selects the tab whose <see cref="RibbonTabItem.Key"/> (or, when it has no key, header text) matches
        /// <paramref name="key"/>.
        /// </summary>
        /// <returns><c>true</c> if a visible, enabled tab was found and selected.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool SelectTab(string key, bool openWhenMinimized = false)
        {
            if (FindTab(key) is not { } tab || !IsTabAvailable(tab))
            {
                return false;
            }

            SelectTab(tab, openWhenMinimized);
            return true;
        }

        /// <summary>
        /// Finds a tab by its <see cref="RibbonTabItem.Key"/>, falling back to its header text, or returns <c>null</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public RibbonTabItem? FindTab(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return Tabs().FirstOrDefault(t => string.Equals(t.LayoutKey, key, StringComparison.Ordinal));
        }

        internal void OnTabHeaderClicked(RibbonTabItem tab)
        {
            if (IsMinimized)
            {
                if (tab.IsSelected && IsMinimizedPopupOpen)
                {
                    SetCurrentValue(IsMinimizedPopupOpenProperty, false);
                    return;
                }

                SelectTab(tab, openWhenMinimized: true);
                return;
            }

            SelectTab(tab, openWhenMinimized: false);
        }

        /// <summary>
        /// Gets the key tip scope root for the selected tab's commands.
        /// </summary>
        internal UIElement? SelectedTabKeyTipScope => IsMinimized ? _body : SelectedTab;

        /// <summary>
        /// Moves keyboard focus into the selected tab's commands.
        /// </summary>
        public void FocusSelectedTabContent()
        {
            var tab = SelectedTab;
            if (tab == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => tab.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));
        }

        #endregion

        #region Template

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_applicationButton != null)
            {
                _applicationButton.Click -= OnApplicationButtonClick;
            }

            if (_minimizedPopup != null)
            {
                _minimizedPopup.Closed -= OnMinimizedPopupClosed;
                _minimizedPopup.PreviewKeyDown -= OnMinimizedPopupPreviewKeyDown;
            }

            if (_displayOptionsButton != null)
            {
                _displayOptionsButton.Click -= OnDisplayOptionsClick;
            }

            if (_qatAbove != null)
            {
                _qatAbove.Content = null;
            }

            if (_qatBelow != null)
            {
                _qatBelow.Content = null;
            }

            if (_body != null && _minimizedPopupHost?.Child == _body)
            {
                _minimizedPopupHost.Child = null;
            }

            base.OnApplyTemplate();

            _applicationButton = GetTemplateChild(PartApplicationButton) as ButtonBase;
            _body = GetTemplateChild(PartBody) as FrameworkElement;
            _bodyHost = GetTemplateChild(PartBodyHost) as Decorator;
            _minimizedPopup = GetTemplateChild(PartMinimizedPopup) as Popup;
            _minimizedPopupHost = GetTemplateChild(PartMinimizedPopupHost) as Decorator;
            _qatAbove = GetTemplateChild(PartQatAbovePresenter) as ContentPresenter;
            _qatBelow = GetTemplateChild(PartQatBelowPresenter) as ContentPresenter;
            _displayOptionsButton = GetTemplateChild(PartDisplayOptionsButton) as ButtonBase;

            if (_applicationButton != null)
            {
                _applicationButton.Click += OnApplicationButtonClick;
            }

            if (_minimizedPopup != null)
            {
                _minimizedPopup.Closed += OnMinimizedPopupClosed;
                _minimizedPopup.PreviewKeyDown += OnMinimizedPopupPreviewKeyDown;
            }

            if (_displayOptionsButton != null)
            {
                _displayOptionsButton.Click += OnDisplayOptionsClick;
            }

            PlaceBody();
            PlaceQuickAccessToolbar();
            UpdateTabHeaders();
        }

        /// <summary>
        /// Moves the command card between the inline host and the minimized popup. The card stays inline (hidden)
        /// while minimized and moves only while the popup is open.
        /// </summary>
        private void PlaceBody()
        {
            if (_body == null || _bodyHost == null || _minimizedPopupHost == null)
            {
                return;
            }

            var inPopup = IsMinimized && IsMinimizedPopupOpen;
            var target = inPopup ? _minimizedPopupHost : _bodyHost;
            var other = inPopup ? _bodyHost : _minimizedPopupHost;
            if (target.Child == _body)
            {
                return;
            }

            if (other.Child == _body)
            {
                other.Child = null;
            }

            target.Child = _body;
        }

        private void PlaceQuickAccessToolbar()
        {
            if (_qat == null)
            {
                return;
            }

            var external = QuickAccessToolbarLocation == QuickAccessToolbarLocation.Above ? QuickAccessToolbarHost : null;
            var target = QuickAccessToolbarLocation == QuickAccessToolbarLocation.Above ? _qatAbove : _qatBelow;
            if (external != null && external.Content != null && external.Content != _qat)
            {
                throw new InvalidOperationException("QuickAccessToolbarHost must be an empty, dedicated ContentControl.");
            }

            _keyTips?.Exit();

            // Detach before reparenting.
            if (_qatAbove?.Content == _qat)
            {
                _qatAbove.Content = null;
            }

            if (_qatBelow?.Content == _qat)
            {
                _qatBelow.Content = null;
            }

            if (_externalQatHost?.Content == _qat)
            {
                _externalQatHost.SetCurrentValue(ContentControl.ContentProperty, null);
            }

            _externalQatHost = external;
            _qat.ContextMenu = external != null ? new RibbonContextMenu() : null;
            if (external != null)
            {
                if (LogicalTreeHelper.GetParent(_qat) == this)
                {
                    RemoveLogicalChild(_qat);
                }

                external.SetCurrentValue(ContentControl.ContentProperty, _qat);
            }
            else
            {
                if (LogicalTreeHelper.GetParent(_qat) != this)
                {
                    AddLogicalChild(_qat);
                }

                target?.SetCurrentValue(ContentPresenter.ContentProperty, _qat);
            }
        }

        #endregion

        #region Lifecycle

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != _window)
            {
                DetachWindow();
                _window = window;
                if (_window != null)
                {
                    _window.SizeChanged += OnWindowSizeChanged;
                    _window.Deactivated += OnWindowDeactivated;
                    _window.LocationChanged += OnWindowLocationChanged;
                    _window.PreviewMouseDown += OnWindowPreviewMouseDown;
                    _window.KeyDown += OnWindowKeyDown;
                    _keyTips = new KeyTipManager(this, _window);
                }
            }

            UpdateTabHeaders();
            UpdateIsCollapsed();

            foreach (var diagnostic in ValidateKeys())
            {
                Trace.TraceWarning(diagnostic);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CloseTransientSurfaces();
            DetachWindow();
        }

        private void DetachWindow()
        {
            _keyTips?.Dispose();
            _keyTips = null;

            if (_window != null)
            {
                _window.SizeChanged -= OnWindowSizeChanged;
                _window.Deactivated -= OnWindowDeactivated;
                _window.LocationChanged -= OnWindowLocationChanged;
                _window.PreviewMouseDown -= OnWindowPreviewMouseDown;
                _window.KeyDown -= OnWindowKeyDown;
                _window = null;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e) => UpdateIsCollapsed();

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            CloseTransientSurfaces();
            _keyTips?.Exit();
        }

        private void OnWindowLocationChanged(object? sender, EventArgs e)
        {
            // The popup doesn't follow the window, so moving the window dismisses it.
            if (IsMinimizedPopupOpen)
            {
                SetCurrentValue(IsMinimizedPopupOpenProperty, false);
            }
        }

        /// <summary>
        /// Dismisses the minimized popup when the user presses a mouse button anywhere in the window outside the
        /// popup. The popup keeps <see cref="Popup.StaysOpen"/> set: a self-dismissing popup opened from a tab
        /// header's mouse-down would close again on the matching mouse-up, which lands on the header and so
        /// outside the popup. Presses on this Ribbon's tab headers are left to the headers (they switch tabs or
        /// close the popup), and presses inside any popup (the command card or one of its drop-downs) never reach
        /// the window's own visual tree.
        /// </summary>
        private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsMinimizedPopupOpen || e.OriginalSource is not DependencyObject source || _window == null)
            {
                return;
            }

            if (source is not Visual and not System.Windows.Media.Media3D.Visual3D)
            {
                source = LogicalTreeHelper.GetParent(source) ?? source;
            }

            if (source is Visual visual && PresentationSource.FromVisual(visual)?.RootVisual != _window)
            {
                return;
            }

            for (var current = source; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is RibbonTabHeader header && header.Tab.Ribbon == this)
                {
                    return;
                }
            }

            SetCurrentValue(IsMinimizedPopupOpenProperty, false);
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            // Escape closes the popup even when keyboard focus stayed in the window (for example on the tab header
            // that opened it); keys pressed inside the popup are handled by OnMinimizedPopupPreviewKeyDown.
            if (e.Key == Key.Escape && !e.Handled && IsMinimizedPopupOpen)
            {
                SetCurrentValue(IsMinimizedPopupOpenProperty, false);
                SelectedTab?.TabHeader?.Focus();
                e.Handled = true;
            }
        }

        private void UpdateIsCollapsed()
        {
            var threshold = CollapseThresholdSize;
            var height = _window?.ActualHeight ?? double.PositiveInfinity;
            var collapsed = IsCollapsible && IsLoaded && (ActualWidth < threshold.Width || height < threshold.Height);
            if (collapsed != IsCollapsed)
            {
                SetValue(IsCollapsedPropertyKey, collapsed);
            }
        }

        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            if ((bool)e.NewValue)
            {
                ribbon.CloseTransientSurfaces();
            }

            ribbon.IsCollapsedChanged?.Invoke(ribbon, EventArgs.Empty);
        }

        #endregion

        #region Minimize, mode and density

        private static object CoerceIsMinimized(DependencyObject d, object value)
        {
            return (bool)value && ((Ribbon)d).IsMinimizable;
        }

        private static void OnIsMinimizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            ribbon.SetCurrentValue(IsMinimizedPopupOpenProperty, false);
            ribbon.PlaceBody();
            ribbon.IsMinimizedChanged?.Invoke(ribbon, EventArgs.Empty);
            ribbon.PreferencesChanged?.Invoke(ribbon, EventArgs.Empty);
            ribbon.Notify((bool)e.NewValue ? "Ribbon collapsed" : "Ribbon pinned");
        }

        private static object CoerceIsMinimizedPopupOpen(DependencyObject d, object value)
        {
            return (bool)value && ((Ribbon)d).IsMinimized;
        }

        private static void OnIsMinimizedPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            if (!(bool)e.NewValue)
            {
                ribbon.SelectedTab?.CloseTransientSurfaces();
                if (ribbon._body is { IsKeyboardFocusWithin: true })
                {
                    ribbon.SelectedTab?.TabHeader?.Focus();
                }
            }

            ribbon.PlaceBody();
        }

        private void OnMinimizedPopupClosed(object? sender, EventArgs e)
        {
            if (IsMinimizedPopupOpen)
            {
                SetCurrentValue(IsMinimizedPopupOpenProperty, false);
            }
        }

        private void OnMinimizedPopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && !e.Handled)
            {
                SetCurrentValue(IsMinimizedPopupOpenProperty, false);
                SelectedTab?.TabHeader?.Focus();
                e.Handled = true;
            }
        }

        private static void OnLayoutModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Ribbon ribbon)
            {
                return;
            }

            // Dismiss transient surfaces before relayout/reparenting.
            ribbon.CloseTransientSurfaces();
            ribbon.LayoutModeChanged?.Invoke(ribbon, EventArgs.Empty);
            ribbon.PreferencesChanged?.Invoke(ribbon, EventArgs.Empty);
            ribbon.Notify(e.NewValue is RibbonLayoutMode.Simplified ? "Simplified ribbon" : "Classic ribbon");
        }

        private static void OnDensityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ribbon ribbon)
            {
                ribbon.CloseTransientSurfaces();
                ribbon.PreferencesChanged?.Invoke(ribbon, EventArgs.Empty);
            }
        }

        private static void OnPreferenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Ribbon)d).PreferencesChanged?.Invoke(d, EventArgs.Empty);
        }

        private static void OnToggleMinimize(object sender, ExecutedRoutedEventArgs e)
        {
            var ribbon = (Ribbon)sender;
            ribbon.SetCurrentValue(IsMinimizedProperty, !ribbon.IsMinimized);
            e.Handled = true;
        }

        private static void OnCanToggleMinimize(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ((Ribbon)sender).IsMinimizable;
            e.Handled = true;
        }

        #endregion

        #region Transient surfaces and focus

        /// <summary>
        /// Closes every transient command surface: the minimized popup, collapsed-group popups, overflow
        /// menus and drop-downs of the selected tab and the Quick Access Toolbar.
        /// </summary>
        public void CloseTransientSurfaces()
        {
            foreach (var tab in Tabs())
            {
                tab.CloseTransientSurfaces();
            }

            CloseDropDowns(this);
            CloseDropDowns(_qat);
            if (_body != null)
            {
                CloseDropDowns(_body);
            }

            SetCurrentValue(IsMinimizedPopupOpenProperty, false);
            _qat.SetCurrentValue(RibbonQuickAccessToolbar.IsMenuOpenProperty, false);
        }

        private static void CloseDropDowns(DependencyObject root)
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                switch (child)
                {
                    case RibbonMenuButton { IsDropDownOpen: true } menu:
                        menu.SetCurrentValue(RibbonMenuButton.IsDropDownOpenProperty, false);
                        break;
                    case RibbonGallery { IsDropDownOpen: true } gallery:
                        gallery.SetCurrentValue(RibbonGallery.IsDropDownOpenProperty, false);
                        break;
                    case ComboBox { IsDropDownOpen: true } combo:
                        combo.SetCurrentValue(ComboBox.IsDropDownOpenProperty, false);
                        break;
                }

                CloseDropDowns(child);
            }
        }

        private void OnCommandInvoked(object sender, RoutedEventArgs e)
        {
            // Close surfaces after the command's own handlers ran.
            CloseTransientSurfaces();
            _keyTips?.Exit();

            // Mouse invocation returns focus to the document; editors and keyboard users keep theirs.
            if (InputManager.Current.MostRecentInputDevice is MouseDevice or StylusDevice)
            {
                RestoreDocumentFocus();
            }
        }

        /// <summary>
        /// Returns keyboard focus to the element that is focused in the Ribbon's parent focus scope (normally
        /// the document), if it is still valid.
        /// </summary>
        public bool RestoreDocumentFocus()
        {
            var parent = VisualTreeHelper.GetParent(this) ?? LogicalTreeHelper.GetParent(this);
            if (parent == null)
            {
                return false;
            }

            var scope = FocusManager.GetFocusScope(parent);
            if (scope == null || FocusManager.GetFocusedElement(scope) is not UIElement target)
            {
                return false;
            }

            if (!IsValidFocusTarget(target) || IsAncestorOf(target))
            {
                return false;
            }

            return target.Focus();
        }

        internal static bool IsValidFocusTarget(IInputElement? element)
        {
            return element is UIElement { IsVisible: true, IsEnabled: true, Focusable: true } ui && PresentationSource.FromVisual(ui) != null;
        }

        #endregion

        #region Backstage

        private static void OnBackstageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            if (e.OldValue is RibbonBackstage oldBackstage)
            {
                oldBackstage.SetCurrentValue(RibbonBackstage.IsOpenProperty, false);
                oldBackstage.OwnerRibbon = null;
                ribbon.RemoveLogicalChild(oldBackstage);
            }

            if (e.NewValue is RibbonBackstage newBackstage)
            {
                ribbon.AddLogicalChild(newBackstage);
                newBackstage.OwnerRibbon = ribbon;
            }
        }

        private void OnApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            OpenBackstage();
        }

        /// <summary>
        /// Opens the Backstage, if one is assigned.
        /// </summary>
        public void OpenBackstage()
        {
            Backstage?.SetCurrentValue(RibbonBackstage.IsOpenProperty, true);
        }

        /// <summary>
        /// Closes the Backstage, if it is open, and returns focus to the element focused before it opened.
        /// Unlike the back button and Escape, this works even when <see cref="RibbonBackstage.CanClose"/> is <c>false</c>.
        /// </summary>
        public void CloseBackstage()
        {
            if (Backstage is { IsOpen: true } backstage)
            {
                backstage.Close(restoreFocus: true);
            }
        }

        internal void OnBackstageOpening()
        {
            CloseTransientSurfaces();
            _keyTips?.Exit();
            SetValue(IsBackstageOpenPropertyKey, true);
        }

        internal void OnBackstageClosed()
        {
            SetValue(IsBackstageOpenPropertyKey, false);
        }

        /// <summary>
        /// Gets the element the Backstage covers: the nearest <see cref="RibbonContainerPanel"/>, otherwise the
        /// window content.
        /// </summary>
        internal UIElement? BackstageHostElement
        {
            get
            {
                DependencyObject? current = this;
                while (current != null)
                {
                    if (current is RibbonContainerPanel panel)
                    {
                        return panel;
                    }

                    current = VisualTreeHelper.GetParent(current);
                }

                return Window.GetWindow(this)?.Content as UIElement;
            }
        }

        #endregion

        #region Command registry and Quick Access Toolbar

        /// <summary>
        /// Enumerates every keyed command in the Ribbon (including unrealized tabs and drop-down items). The
        /// registry is derived from the logical tree on demand, so it never holds visual-parent relationships.
        /// </summary>
        public IEnumerable<IRibbonControl> EnumerateCommands()
        {
            var stack = new Stack<object>();
            foreach (var tab in Tabs())
            {
                stack.Push(tab);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is IRibbonControl rc && current is DependencyObject dobj && RibbonItem.GetSourceKey(dobj) == null && !string.IsNullOrEmpty(rc.Key))
                {
                    yield return rc;
                }

                if (current is DependencyObject d)
                {
                    foreach (var child in LogicalTreeHelper.GetChildren(d))
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        /// <summary>
        /// Finds a command by key, or returns <c>null</c>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public IRibbonControl? FindCommand(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return EnumerateCommands().FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.Ordinal));
        }

        /// <summary>
        /// Validates the identity registry and returns a diagnostic for each duplicate key. Duplicates are
        /// rejected for Quick Access Toolbar and persistence lookups (the first registration wins).
        /// </summary>
        public IReadOnlyList<string> ValidateKeys()
        {
            return EnumerateCommands()
                .GroupBy(c => c.Key!, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => $"Ribbon: duplicate command key '{g.Key}' is used by {g.Count()} controls ({string.Join(", ", g.Select(c => c.GetType().Name))}). Keys must be unique; the first registration is used.")
                .ToList();
        }

        private string? ResolveKey(object keyOrControl) => keyOrControl switch
        {
            string s => s,
            IRibbonControl rc => rc.Key,
            DependencyObject d => RibbonItem.GetSourceKey(d) ?? RibbonItem.GetKey(d),
            _ => null
        };

        /// <summary>
        /// Returns whether the command (a key or a control) can be added to the Quick Access Toolbar.
        /// </summary>
        /// <remarks>
        /// A command can be added when the toolbar is available, the command has a key, it is not already in the
        /// toolbar, and its <see cref="RibbonItem.CanAddToQuickAccessToolbarProperty"/> is not <c>false</c>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keyOrControl"/> is <c>null</c>.</exception>
        public bool CanAddToQuickAccessToolbar(object keyOrControl)
        {
            ArgumentNullException.ThrowIfNull(keyOrControl);
            var key = ResolveKey(keyOrControl);
            return QuickAccessToolbarMode != QuickAccessToolbarMode.None
                   && !string.IsNullOrEmpty(key)
                   && !_qatKeys.Contains(key)
                   && FindCommand(key) is { } command
                   && command is not RibbonControlGroup
                   && (command is not DependencyObject d || RibbonItem.GetCanAddToQuickAccessToolbar(d));
        }

        /// <summary>
        /// Returns whether the command (a key, control or Quick Access Toolbar presentation) is in the toolbar.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="keyOrControl"/> is <c>null</c>.</exception>
        public bool CanRemoveFromQuickAccessToolbar(object keyOrControl)
        {
            ArgumentNullException.ThrowIfNull(keyOrControl);
            var key = ResolveKey(keyOrControl);
            return key != null && _qatKeys.Contains(key);
        }

        /// <summary>
        /// Adds a command to the Quick Access Toolbar. Raises the cancellable <see cref="QuickAccessToolbarItemAdding"/>
        /// first; adding to a hidden toolbar reveals it.
        /// </summary>
        /// <returns><c>true</c> if the command was added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keyOrControl"/> is <c>null</c>.</exception>
        public bool AddToQuickAccessToolbar(object keyOrControl)
        {
            ArgumentNullException.ThrowIfNull(keyOrControl);
            return InsertQuickAccessToolbarItem(ResolveKey(keyOrControl), _qatKeys.Count, raiseEvents: true);
        }

        private bool InsertQuickAccessToolbarItem(string? key, int index, bool raiseEvents)
        {
            if (key == null || !CanAddToQuickAccessToolbar(key) || FindCommand(key) is not { } source)
            {
                return false;
            }

            var presentation = source.CreatePresentation(RibbonPresentationKind.QuickAccessToolbar);
            if (presentation == null)
            {
                return false;
            }

            if (raiseEvents)
            {
                var args = new QuickAccessToolbarItemAddingEventArgs(key, source, presentation);
                QuickAccessToolbarItemAdding?.Invoke(this, args);
                if (args.Cancel)
                {
                    BindingOperations.ClearAllBindings(presentation);
                    return false;
                }

                if (!ReferenceEquals(args.Presentation, presentation))
                {
                    // The handler supplied its own presentation; release the generated one.
                    BindingOperations.ClearAllBindings(presentation);
                    presentation = args.Presentation;
                }
            }

            RibbonItem.SetSourceKey(presentation, key);
            if (source is DependencyObject sourceElement)
            {
                RibbonItem.SetIsInQuickAccessToolbar(sourceElement, true);
            }

            index = Math.Clamp(index, 0, _qatKeys.Count);
            _qatKeys.Insert(index, key);
            _qatItems.Insert(index, presentation);

            if (QuickAccessToolbarMode == QuickAccessToolbarMode.Hidden)
            {
                SetCurrentValue(QuickAccessToolbarModeProperty, QuickAccessToolbarMode.Visible);
            }

            if (raiseEvents)
            {
                QuickAccessToolbarItemAdded?.Invoke(this, new QuickAccessToolbarItemEventArgs(key, presentation));
                PreferencesChanged?.Invoke(this, EventArgs.Empty);
                Notify($"{source.Label ?? key} added to the Quick Access Toolbar");
            }

            return true;
        }

        /// <summary>
        /// Removes a command from the Quick Access Toolbar.
        /// </summary>
        /// <returns><c>true</c> if the command was removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keyOrControl"/> is <c>null</c>.</exception>
        public bool RemoveFromQuickAccessToolbar(object keyOrControl)
        {
            ArgumentNullException.ThrowIfNull(keyOrControl);
            var key = ResolveKey(keyOrControl);
            if (key == null)
            {
                return false;
            }

            var index = _qatKeys.IndexOf(key);
            if (index < 0)
            {
                return false;
            }

            var presentation = _qatItems[index];
            _qatKeys.RemoveAt(index);
            _qatItems.RemoveAt(index);
            BindingOperations.ClearAllBindings(presentation);
            ClearIsInQuickAccessToolbar(key);

            QuickAccessToolbarItemRemoved?.Invoke(this, new QuickAccessToolbarItemEventArgs(key, presentation));
            PreferencesChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Replaces the Quick Access Toolbar contents with the given keys (used by restoration). Unknown keys
        /// are skipped and reported.
        /// </summary>
        internal void RestoreQuickAccessToolbar(IEnumerable<string> keys, List<string> diagnostics)
        {
            while (_qatKeys.Count > 0)
            {
                var presentation = _qatItems[^1];
                var removedKey = _qatKeys[^1];
                _qatKeys.RemoveAt(_qatKeys.Count - 1);
                _qatItems.RemoveAt(_qatItems.Count - 1);
                BindingOperations.ClearAllBindings(presentation);
                ClearIsInQuickAccessToolbar(removedKey);
            }

            foreach (var key in keys)
            {
                if (!InsertQuickAccessToolbarItem(key, _qatKeys.Count, raiseEvents: false))
                {
                    diagnostics.Add($"Quick Access Toolbar command '{key}' was skipped: it is unknown, duplicated or cannot be presented.");
                }
            }
        }

        private void ClearIsInQuickAccessToolbar(string key)
        {
            if (FindCommand(key) is DependencyObject source)
            {
                RibbonItem.SetIsInQuickAccessToolbar(source, false);
            }
        }

        private static void OnQuickAccessToolbarLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            ribbon.CloseTransientSurfaces();
            ribbon.PlaceQuickAccessToolbar();
            ribbon.QuickAccessToolbarLocationChanged?.Invoke(ribbon, EventArgs.Empty);
            ribbon.PreferencesChanged?.Invoke(ribbon, EventArgs.Empty);
        }

        private static void OnQuickAccessToolbarModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ribbon = (Ribbon)d;
            ribbon.SetValue(IsQuickAccessToolbarVisiblePropertyKey, (QuickAccessToolbarMode)e.NewValue == QuickAccessToolbarMode.Visible);
            ribbon.QuickAccessToolbarModeChanged?.Invoke(ribbon, EventArgs.Empty);
            ribbon.PreferencesChanged?.Invoke(ribbon, EventArgs.Empty);
        }

        #endregion

        #region Context menu and display options

        private void OnRibbonContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var menu = (sender as FrameworkElement)?.ContextMenu;
            if (menu == null)
            {
                return;
            }

            // Leave editors their own clipboard menus.
            if (e.OriginalSource is DependencyObject source && FindAncestor<TextBoxBase>(source) != null)
            {
                e.Handled = true;
                return;
            }

            menu.Items.Clear();
            var target = e.OriginalSource is DependencyObject d ? FindCommandElement(d) : null;
            if (target != null)
            {
                var key = RibbonItem.GetSourceKey(target) ?? RibbonItem.GetKey(target);
                if (key != null && CanRemoveFromQuickAccessToolbar(key))
                {
                    menu.Items.Add(CreateMenuItem("Remove from Quick Access Toolbar", () => RemoveFromQuickAccessToolbar(key)));
                }
                else if (key != null && CanAddToQuickAccessToolbar(key))
                {
                    menu.Items.Add(CreateMenuItem("Add to Quick Access Toolbar", () => AddToQuickAccessToolbar(key)));
                }

                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new Separator());
                }
            }

            foreach (var item in BuildDisplayMenuItems(includeDensity: false))
            {
                menu.Items.Add(item);
            }
        }

        private static T? FindAncestor<T>(DependencyObject? d) where T : class
        {
            while (d != null)
            {
                if (d is T t)
                {
                    return t;
                }

                d = d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            }

            return null;
        }

        private DependencyObject? FindCommandElement(DependencyObject d)
        {
            DependencyObject? current = d;
            while (current != null && current != this)
            {
                if (RibbonItem.GetSourceKey(current) != null || (current is IRibbonControl rc && !string.IsNullOrEmpty(rc.Key)))
                {
                    return current;
                }

                current = current is Visual ? VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        private static MenuItem CreateMenuItem(string header, Action action, bool? isChecked = null)
        {
            var item = new RibbonMenuItem { Header = header };
            if (isChecked.HasValue)
            {
                item.IsCheckable = true;
                item.IsChecked = isChecked.Value;
            }

            item.Click += (_, _) => action();
            return item;
        }

        /// <summary>
        /// Builds the Ribbon display options menu entries (layout mode, minimize, Quick Access Toolbar, density).
        /// </summary>
        internal List<Control> BuildDisplayMenuItems(bool includeDensity)
        {
            var items = new List<Control>();

            if (CanChangeLayoutMode)
            {
                items.Add(CreateMenuItem("Classic ribbon", () => SetCurrentValue(LayoutModeProperty, RibbonLayoutMode.Classic), LayoutMode == RibbonLayoutMode.Classic));
                items.Add(CreateMenuItem("Simplified ribbon", () => SetCurrentValue(LayoutModeProperty, RibbonLayoutMode.Simplified), LayoutMode == RibbonLayoutMode.Simplified));
                items.Add(new Separator());
            }

            if (IsMinimizable)
            {
                items.Add(CreateMenuItem("Always show ribbon", () => SetCurrentValue(IsMinimizedProperty, false), !IsMinimized));
                items.Add(CreateMenuItem("Show tabs only", () => SetCurrentValue(IsMinimizedProperty, true), IsMinimized));
                items.Add(new Separator());
            }

            if (QuickAccessToolbarMode != QuickAccessToolbarMode.None)
            {
                items.Add(CreateMenuItem("Show Quick Access Toolbar",
                    () => SetCurrentValue(QuickAccessToolbarModeProperty, IsQuickAccessToolbarVisible ? QuickAccessToolbarMode.Hidden : QuickAccessToolbarMode.Visible),
                    IsQuickAccessToolbarVisible));

                if (IsQuickAccessToolbarVisible)
                {
                    items.Add(QuickAccessToolbarLocation == QuickAccessToolbarLocation.Above
                        ? CreateMenuItem("Show Quick Access Toolbar below the ribbon", () => SetCurrentValue(QuickAccessToolbarLocationProperty, QuickAccessToolbarLocation.Below))
                        : CreateMenuItem("Show Quick Access Toolbar above the ribbon", () => SetCurrentValue(QuickAccessToolbarLocationProperty, QuickAccessToolbarLocation.Above)));
                    items.Add(CreateMenuItem("Show command labels", () => SetCurrentValue(QuickAccessToolbarShowLabelsProperty, !QuickAccessToolbarShowLabels), QuickAccessToolbarShowLabels));
                }
            }

            if (includeDensity)
            {
                items.Add(new Separator());
                var density = new RibbonMenuItem { Header = "Density" };
                foreach (var value in Enum.GetValues<UserInterfaceDensity>())
                {
                    var v = value;
                    density.Items.Add(CreateMenuItem(v.ToString(), () => SetCurrentValue(DensityProperty, v), Density == v));
                }

                items.Add(density);
            }

            if (items.Count > 0 && items[^1] is Separator)
            {
                items.RemoveAt(items.Count - 1);
            }

            return items;
        }

        private void OnDisplayOptionsClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (_displayOptionsButton == null)
            {
                return;
            }

            var menu = new RibbonContextMenu
            {
                PlacementTarget = _displayOptionsButton,
                Placement = PlacementMode.Bottom
            };

            foreach (var item in BuildDisplayMenuItems(includeDensity: true))
            {
                menu.Items.Add(item);
            }

            menu.Closed += (_, _) => (_displayOptionsButton as System.Windows.Controls.Primitives.ToggleButton)?.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, false);
            menu.IsOpen = true;
        }

        #endregion

        #region Automation

        private void Notify(string message)
        {
            if (UIElementAutomationPeer.FromElement(this) is { } peer)
            {
                peer.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.MostRecent, message, "MosaicRibbon");
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonAutomationPeer(this);

        #endregion
    }
}
