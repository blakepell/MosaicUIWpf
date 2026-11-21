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
using System.Windows.Documents;
using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The File tab of the Ribbon tab strip. Opens the <see cref="Ribbon.Backstage"/>.
    /// </summary>
    public class RibbonApplicationButton : Button, IRibbonKeyTipTarget
    {
        static RibbonApplicationButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonApplicationButton), new FrameworkPropertyMetadata(typeof(RibbonApplicationButton)));
        }

        /// <inheritdoc />
        public bool IsKeyTipTarget => IsVisible && IsEnabled;

        /// <inheritdoc />
        public string? KeyTipLabel => Content as string;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            OnClick();
            return null;
        }
    }

    /// <summary>
    /// A selectable page in the <see cref="RibbonBackstage"/> navigation pane (for example Home, New, Open).
    /// Its <see cref="ContentControl.Content"/> is shown in the Backstage content area when selected.
    /// </summary>
    public class RibbonBackstageTabItem : HeaderedContentControl
    {
        /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
        public static readonly DependencyProperty IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(typeof(RibbonBackstageTabItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));

        /// <summary>Gets or sets a value indicating whether the page is selected.</summary>
        public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonBackstageTabItem));

        /// <summary>Gets or sets the navigation icon glyph.</summary>
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        static RibbonBackstageTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonBackstageTabItem), new FrameworkPropertyMetadata(typeof(RibbonBackstageTabItem)));
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (RibbonBackstageTabItem)d;
            item.RaiseEvent(new RoutedEventArgs((bool)e.NewValue ? Selector.SelectedEvent : Selector.UnselectedEvent, item));
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            SetCurrentValue(IsSelectedProperty, true);
            Focus();
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key is Key.Enter or Key.Space)
            {
                SetCurrentValue(IsSelectedProperty, true);
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (e.OriginalSource == this && InputManager.Current.MostRecentInputDevice is KeyboardDevice)
            {
                SetCurrentValue(IsSelectedProperty, true);
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonBackstageTabItemAutomationPeer(this);
    }

    /// <summary>
    /// A command in the <see cref="RibbonBackstage"/> navigation pane (for example Save or Close). By default
    /// clicking it closes the Backstage after the command runs.
    /// </summary>
    public class RibbonBackstageButton : Button
    {
        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonBackstageButton));

        /// <summary>Gets or sets the navigation icon glyph.</summary>
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Identifies the <see cref="ClosesBackstage"/> dependency property.</summary>
        public static readonly DependencyProperty ClosesBackstageProperty = DependencyProperty.Register(
            nameof(ClosesBackstage), typeof(bool), typeof(RibbonBackstageButton), new FrameworkPropertyMetadata(true));

        /// <summary>Gets or sets a value indicating whether clicking the button closes the Backstage.</summary>
        public bool ClosesBackstage { get => (bool)GetValue(ClosesBackstageProperty); set => SetValue(ClosesBackstageProperty, value); }

        static RibbonBackstageButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonBackstageButton), new FrameworkPropertyMetadata(typeof(RibbonBackstageButton)));
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            var backstage = ItemsControl.ItemsControlFromItemContainer(this) as RibbonBackstage;
            if (ClosesBackstage && backstage != null)
            {
                // Close first so a command that opens a dialog does so over the document, with focus restored.
                backstage.Close(restoreFocus: true);
            }

            base.OnClick();
        }
    }

    /// <summary>
    /// The full-window File surface. It covers the nearest <see cref="RibbonContainerPanel"/> (or the window
    /// content) through the adorner layer and enters and exits with a modern Office–style slide and fade.
    /// Focus is remembered on open and restored, if still valid, on close.
    /// </summary>
    [TemplatePart(Name = PartBackButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartNavigation, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartContent, Type = typeof(FrameworkElement))]
    [DefaultProperty(nameof(Items))]
    public class RibbonBackstage : Selector
    {
        private const string PartBackButton = "PART_BackButton";
        private const string PartNavigation = "PART_Navigation";
        private const string PartContent = "PART_Content";

        // Motion profile: Office 365's File view eases its navigation pane in from the left while the page fades
        // and drifts in from the right; dismissal is a shorter reverse with ease-in. Decelerating cubic curves
        // match that feel; durations are configurable below.
        private const double NavigationOffset = -56;
        private const double ContentOffset = 40;

        private ButtonBase? _backButton;
        private FrameworkElement? _navigation;
        private FrameworkElement? _content;
        private RibbonBackstageAdorner? _adorner;
        private IInputElement? _restoreFocus;
        private bool _restoreFocusOnClose = true;
        private int _animationVersion;

        /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
            nameof(IsOpen), typeof(bool), typeof(RibbonBackstage),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

        /// <summary>Gets or sets a value indicating whether the Backstage is open.</summary>
        [Category("Common")]
        public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

        /// <summary>Identifies the <see cref="CanClose"/> dependency property.</summary>
        public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register(
            nameof(CanClose), typeof(bool), typeof(RibbonBackstage), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the back button and Escape can close the Backstage. Setting
        /// <see cref="IsOpen"/> in code always works.
        /// </summary>
        [Category("Common")]
        public bool CanClose { get => (bool)GetValue(CanCloseProperty); set => SetValue(CanCloseProperty, value); }

        private static readonly DependencyPropertyKey SelectedContentPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SelectedContent), typeof(object), typeof(RibbonBackstage), new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="SelectedContent"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedContentProperty = SelectedContentPropertyKey.DependencyProperty;

        /// <summary>Gets the content of the selected <see cref="RibbonBackstageTabItem"/>.</summary>
        [Browsable(false)]
        public object? SelectedContent => GetValue(SelectedContentProperty);

        /// <summary>Identifies the <see cref="IsAnimationEnabled"/> dependency property.</summary>
        public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
            nameof(IsAnimationEnabled), typeof(bool), typeof(RibbonBackstage), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether entry and exit are animated. Animation is also skipped when
        /// Windows client-area animations are turned off.
        /// </summary>
        [Category("Appearance")]
        public bool IsAnimationEnabled { get => (bool)GetValue(IsAnimationEnabledProperty); set => SetValue(IsAnimationEnabledProperty, value); }

        /// <summary>Identifies the <see cref="OpenDuration"/> dependency property.</summary>
        public static readonly DependencyProperty OpenDurationProperty = DependencyProperty.Register(
            nameof(OpenDuration), typeof(TimeSpan), typeof(RibbonBackstage), new FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(333)),
            RibbonValidation.IsNonNegativeTimeSpan);

        /// <summary>Gets or sets the duration of the entry animation.</summary>
        [Category("Appearance")]
        public TimeSpan OpenDuration { get => (TimeSpan)GetValue(OpenDurationProperty); set => SetValue(OpenDurationProperty, value); }

        /// <summary>Identifies the <see cref="CloseDuration"/> dependency property.</summary>
        public static readonly DependencyProperty CloseDurationProperty = DependencyProperty.Register(
            nameof(CloseDuration), typeof(TimeSpan), typeof(RibbonBackstage), new FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(167)),
            RibbonValidation.IsNonNegativeTimeSpan);

        /// <summary>Gets or sets the duration of the exit animation.</summary>
        [Category("Appearance")]
        public TimeSpan CloseDuration { get => (TimeSpan)GetValue(CloseDurationProperty); set => SetValue(CloseDurationProperty, value); }

        /// <summary>Identifies the <see cref="Opened"/> routed event.</summary>
        public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent(nameof(Opened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonBackstage));

        /// <summary>Occurs when the Backstage has opened (after its entry animation starts).</summary>
        public event RoutedEventHandler Opened { add => AddHandler(OpenedEvent, value); remove => RemoveHandler(OpenedEvent, value); }

        /// <summary>Identifies the <see cref="Closed"/> routed event.</summary>
        public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RibbonBackstage));

        /// <summary>Occurs when the Backstage has closed and focus has been restored.</summary>
        public event RoutedEventHandler Closed { add => AddHandler(ClosedEvent, value); remove => RemoveHandler(ClosedEvent, value); }

        /// <summary>Gets the owning Ribbon.</summary>
        public Ribbon? OwnerRibbon { get; internal set; }

        static RibbonBackstage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonBackstage), new FrameworkPropertyMetadata(typeof(RibbonBackstage)));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(RibbonBackstage), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(RibbonBackstage), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(RibbonBackstage), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
            FocusableProperty.OverrideMetadata(typeof(RibbonBackstage), new FrameworkPropertyMetadata(false));
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride() => new RibbonBackstageTabItem();

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_backButton != null)
            {
                _backButton.Click -= OnBackClick;
            }

            base.OnApplyTemplate();

            _backButton = GetTemplateChild(PartBackButton) as ButtonBase;
            _navigation = GetTemplateChild(PartNavigation) as FrameworkElement;
            _content = GetTemplateChild(PartContent) as FrameworkElement;

            if (_backButton != null)
            {
                _backButton.Click += OnBackClick;
            }
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (CanClose)
            {
                Close(restoreFocus: true);
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape && CanClose && !e.Handled)
            {
                Close(restoreFocus: true);
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            var container = SelectedIndex >= 0 ? (ItemContainerGenerator.ContainerFromIndex(SelectedIndex) ?? Items[SelectedIndex]) as RibbonBackstageTabItem : null;
            SetValue(SelectedContentPropertyKey, container?.Content);

            if (IsOpen && _content != null && IsAnimationActive)
            {
                // A short fade-through when switching pages, as Office does.
                _content.BeginAnimation(OpacityProperty, new DoubleAnimation(0.4, 1, TimeSpan.FromMilliseconds(150)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }
        }

        /// <summary>
        /// Closes the Backstage.
        /// </summary>
        /// <param name="restoreFocus">Whether to return focus to the element focused before it opened.</param>
        public void Close(bool restoreFocus)
        {
            _restoreFocusOnClose = restoreFocus;
            SetCurrentValue(IsOpenProperty, false);
        }

        private bool IsAnimationActive => IsAnimationEnabled && SystemParameters.ClientAreaAnimation && RenderCapability.Tier > 0;

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var backstage = (RibbonBackstage)d;
            if ((bool)e.NewValue)
            {
                backstage.Show();
            }
            else
            {
                backstage.Hide();
            }
        }

        private void Show()
        {
            var host = OwnerRibbon?.BackstageHostElement;
            var layer = host != null ? AdornerLayer.GetAdornerLayer(host) : null;
            if (host == null || layer == null)
            {
                // Nowhere to present; keep the state honest.
                SetCurrentValue(IsOpenProperty, false);
                return;
            }

            OwnerRibbon?.OnBackstageOpening();

            if (Keyboard.FocusedElement is { } focused && !(focused is DependencyObject fd && IsAncestorOf(fd)))
            {
                _restoreFocus = focused;
            }

            _restoreFocusOnClose = true;

            if (DataContext == null && OwnerRibbon != null && ReadLocalValue(DataContextProperty) == DependencyProperty.UnsetValue)
            {
                SetCurrentValue(DataContextProperty, OwnerRibbon.DataContext);
            }

            if (_adorner == null)
            {
                _adorner = new RibbonBackstageAdorner(host, this);
                layer.Add(_adorner);
            }

            if (SelectedIndex < 0)
            {
                SelectFirstPage();
            }

            ApplyTemplate();
            UpdateLayout();
            AnimateIn();
            RaiseEvent(new RoutedEventArgs(OpenedEvent, this));

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (IsOpen)
                {
                    var target = SelectedIndex >= 0 ? (ItemContainerGenerator.ContainerFromIndex(SelectedIndex) ?? Items[SelectedIndex]) as UIElement : null;
                    if (target == null || !target.Focus())
                    {
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                    }
                }
            }));
        }

        private void SelectFirstPage()
        {
            for (var i = 0; i < Items.Count; i++)
            {
                if ((ItemContainerGenerator.ContainerFromIndex(i) ?? Items[i]) is RibbonBackstageTabItem { IsEnabled: true })
                {
                    SetCurrentValue(SelectedIndexProperty, i);
                    return;
                }
            }
        }

        private void Hide()
        {
            if (_adorner == null)
            {
                return;
            }

            var version = ++_animationVersion;
            if (!IsAnimationActive)
            {
                Detach();
                return;
            }

            var duration = new Duration(CloseDuration);
            var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

            if (_navigation != null)
            {
                EnsureTranslate(_navigation).BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, NavigationOffset, duration) { EasingFunction = ease });
            }

            _content?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, duration) { EasingFunction = ease });

            var fade = new DoubleAnimation(0, duration) { EasingFunction = ease };
            fade.Completed += (_, _) =>
            {
                if (version == _animationVersion && !IsOpen)
                {
                    Detach();
                }
            };
            BeginAnimation(OpacityProperty, fade);
        }

        private void AnimateIn()
        {
            ++_animationVersion;
            if (!IsAnimationActive)
            {
                BeginAnimation(OpacityProperty, null);
                Opacity = 1;
                return;
            }

            var duration = new Duration(OpenDuration);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(OpenDuration.TotalMilliseconds * 0.6))) { EasingFunction = ease });

            if (_navigation != null)
            {
                EnsureTranslate(_navigation).BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(NavigationOffset, 0, duration) { EasingFunction = ease });
            }

            if (_content != null)
            {
                var delay = TimeSpan.FromMilliseconds(OpenDuration.TotalMilliseconds * 0.2);
                EnsureTranslate(_content).BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(ContentOffset, 0, duration) { EasingFunction = ease, BeginTime = delay });
                _content.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease, BeginTime = delay });
            }
        }

        private static TranslateTransform EnsureTranslate(FrameworkElement element)
        {
            if (element.RenderTransform is TranslateTransform { IsFrozen: false } existing)
            {
                return existing;
            }

            var transform = new TranslateTransform();
            element.RenderTransform = transform;
            return transform;
        }

        private void Detach()
        {
            if (_adorner != null)
            {
                var layer = VisualTreeHelper.GetParent(_adorner) as AdornerLayer;
                _adorner.Release();
                layer?.Remove(_adorner);
                _adorner = null;
            }

            BeginAnimation(OpacityProperty, null);
            OwnerRibbon?.OnBackstageClosed();

            var restore = _restoreFocus;
            _restoreFocus = null;
            if (_restoreFocusOnClose)
            {
                // Revalidate: the document or invoker may have gone away while the Backstage was open.
                if (Ribbon.IsValidFocusTarget(restore))
                {
                    restore!.Focus();
                }
                else if (OwnerRibbon != null && !OwnerRibbon.RestoreDocumentFocus())
                {
                    OwnerRibbon.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                }
            }

            RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonBackstageAutomationPeer(this);
    }

    /// <summary>
    /// Presents a <see cref="RibbonBackstage"/> over its host element in the adorner layer.
    /// </summary>
    internal sealed class RibbonBackstageAdorner : Adorner
    {
        private RibbonBackstage? _child;

        public RibbonBackstageAdorner(UIElement adornedElement, RibbonBackstage child) : base(adornedElement)
        {
            _child = child;
            AddVisualChild(child);
        }

        public void Release()
        {
            if (_child != null)
            {
                RemoveVisualChild(_child);
                _child = null;
            }
        }

        protected override int VisualChildrenCount => _child == null ? 0 : 1;

        protected override Visual GetVisualChild(int index) => _child ?? throw new ArgumentOutOfRangeException(nameof(index));

        protected override Size MeasureOverride(Size constraint)
        {
            var size = AdornedElement.RenderSize;
            _child?.Measure(size);
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _child?.Arrange(new Rect(AdornedElement.RenderSize));
            return finalSize;
        }
    }
}
