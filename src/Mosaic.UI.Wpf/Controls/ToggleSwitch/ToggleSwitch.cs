/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Windows.Automation.Peers;
using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a toggle switch control that allows users to switch between two states.
    /// </summary>
    [DefaultEvent(nameof(Toggled))]
    [DefaultProperty(nameof(IsOn))]
    [TemplatePart(Name = PartThumb, Type = typeof(Border))]
    [TemplatePart(Name = PartOnTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PartOffTextBlock, Type = typeof(TextBlock))]
    public class ToggleSwitch : Control
    {
        private const string PartThumb = "PART_Thumb";
        private const string PartOnTextBlock = "PART_OnTextBlock";
        private const string PartOffTextBlock = "PART_OffTextBlock";

        private Border? _thumb;
        private Canvas? _canvasParent;
        private TextBlock? _onTextBlock;
        private TextBlock? _offTextBlock;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="IsOn"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
            nameof(IsOn), typeof(bool), typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOnChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the control is in the "on" state.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the switch is currently on.")]
        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OnText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OnTextProperty = DependencyProperty.Register(
            nameof(OnText), typeof(string), typeof(ToggleSwitch), new PropertyMetadata("ON"));

        /// <summary>
        /// Gets or sets the text displayed when the control is in the "on" state.
        /// </summary>
        [Category("Appearance")]
        [Description("Text shown on the left side when the switch is on.")]
        public string OnText
        {
            get => (string)GetValue(OnTextProperty);
            set => SetValue(OnTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OffText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffTextProperty = DependencyProperty.Register(
            nameof(OffText), typeof(string), typeof(ToggleSwitch), new PropertyMetadata("OFF"));

        /// <summary>
        /// Gets or sets the text displayed when the control is in the "off" state.
        /// </summary>
        [Category("Appearance")]
        [Description("Text shown on the right side when the switch is off.")]
        public string OffText
        {
            get => (string)GetValue(OffTextProperty);
            set => SetValue(OffTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OnBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OnBackgroundBrushProperty = DependencyProperty.Register(
            nameof(OnBackgroundBrush), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.Green, OnStateVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the background brush used when the switch is on.
        /// </summary>
        [Category("Brushes")]
        [Description("Background brush used when IsOn is true.")]
        public Brush OnBackgroundBrush
        {
            get => (Brush)GetValue(OnBackgroundBrushProperty);
            set => SetValue(OnBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OffBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffBackgroundBrushProperty = DependencyProperty.Register(
            nameof(OffBackgroundBrush), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.DarkGray, OnStateVisualPropertyChanged));

        /// <summary>
        /// Gets or sets the background brush used when the switch is off.
        /// </summary>
        [Category("Brushes")]
        [Description("Background brush used when IsOn is false.")]
        public Brush OffBackgroundBrush
        {
            get => (Brush)GetValue(OffBackgroundBrushProperty);
            set => SetValue(OffBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(ToggleSwitch), new PropertyMetadata(default(CornerRadius)));

        /// <summary>
        /// Gets or sets the corner radius used by the switch track and thumb.
        /// </summary>
        [Category("Appearance")]
        [Description("Corner radius for switch border and thumb.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(ToggleSwitch), new PropertyMetadata(default(ICommand)));

        /// <summary>
        /// Gets or sets the command that is invoked for user initiated toggles.
        /// </summary>
        [Category("Action")]
        [Description("Command invoked when the user toggles the switch.")]
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(ToggleSwitch), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets an optional parameter for <see cref="Command"/>.
        /// </summary>
        [Category("Action")]
        [Description("Optional command parameter; defaults to the new IsOn state when null.")]
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="Toggled"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ToggledEvent = EventManager.RegisterRoutedEvent(
            nameof(Toggled), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<bool>), typeof(ToggleSwitch));

        /// <summary>
        /// Occurs when <see cref="IsOn"/> changes.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when IsOn changes.")]
        public event RoutedPropertyChangedEventHandler<bool> Toggled
        {
            add => AddHandler(ToggledEvent, value);
            remove => RemoveHandler(ToggledEvent, value);
        }

        #endregion

        #region CLR Events

        /// <summary>
        /// Raised when <see cref="IsOn"/> changes.
        /// </summary>
        /// <remarks>
        /// This event is preserved for backward compatibility. Prefer <see cref="Toggled"/> for routed event scenarios.
        /// </remarks>
        public event EventHandler<ToggleSwitchChangedEventArgs>? Changed;

        /// <summary>
        /// Event args for the <see cref="Changed"/> event.
        /// </summary>
        public sealed class ToggleSwitchChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the current switch state.
            /// </summary>
            public bool IsOn { get; }

            public ToggleSwitchChangedEventArgs(bool isOn)
            {
                IsOn = isOn;
            }
        }

        #endregion

        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
            FocusableProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(true));
            WidthProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(60.0));
            HeightProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(28.0));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleSwitch"/> class.
        /// </summary>
        public ToggleSwitch()
        {
            Cursor = Cursors.Hand;
        }

        private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ToggleSwitch toggleSwitch)
            {
                return;
            }

            var oldValue = (bool)e.OldValue;
            var newValue = (bool)e.NewValue;

            toggleSwitch.UpdateThumbPosition(animate: true);
            toggleSwitch.RefreshUI();
            toggleSwitch.RaiseToggledEvents(oldValue, newValue);
        }

        private static void OnStateVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleSwitch toggleSwitch)
            {
                toggleSwitch.RefreshUI();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToggleSwitchAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_thumb != null)
            {
                _thumb.Loaded -= OnThumbLoaded;
            }

            if (_canvasParent != null)
            {
                _canvasParent.SizeChanged -= OnCanvasSizeChanged;
            }

            _thumb = GetTemplateChild(PartThumb) as Border;
            _canvasParent = _thumb?.Parent as Canvas;
            _onTextBlock = GetTemplateChild(PartOnTextBlock) as TextBlock;
            _offTextBlock = GetTemplateChild(PartOffTextBlock) as TextBlock;

            if (_thumb != null)
            {
                _thumb.Loaded += OnThumbLoaded;
            }

            if (_canvasParent != null)
            {
                _canvasParent.SizeChanged += OnCanvasSizeChanged;
            }

            UpdateThumbPosition(animate: false);
            RefreshUI();
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (!IsEnabled)
            {
                return;
            }

            ToggleFromUserInteraction();
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!IsEnabled)
            {
                return;
            }

            if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                ToggleFromUserInteraction();
                e.Handled = true;
            }
        }

        internal void ToggleFromAutomation()
        {
            if (!IsEnabled)
            {
                return;
            }

            ToggleFromUserInteraction();
        }

        private void ToggleFromUserInteraction()
        {
            var newValue = !IsOn;
            SetCurrentValue(IsOnProperty, newValue);
            ExecuteToggleCommand(newValue);
        }

        private void ExecuteToggleCommand(bool newValue)
        {
            var command = Command;
            if (command == null)
            {
                return;
            }

            var parameter = CommandParameter ?? newValue;
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        private void RaiseToggledEvents(bool oldValue, bool newValue)
        {
            RaiseEvent(new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue, ToggledEvent));
            OnChanged(new ToggleSwitchChangedEventArgs(newValue));

            if (UIElementAutomationPeer.FromElement(this) is ToggleSwitchAutomationPeer peer)
            {
                peer.RaiseToggleStateChanged(oldValue, newValue);
            }
        }

        /// <summary>
        /// Protected virtual method to allow derived classes to handle the <see cref="Changed"/> event.
        /// </summary>
        protected virtual void OnChanged(ToggleSwitchChangedEventArgs e)
        {
            Changed?.Invoke(this, e);
        }

        private void RefreshUI()
        {
            Background = IsOn ? OnBackgroundBrush : OffBackgroundBrush;

            if (_onTextBlock != null)
            {
                _onTextBlock.Visibility = IsOn ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_offTextBlock != null)
            {
                _offTextBlock.Visibility = IsOn ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void OnThumbLoaded(object? sender, RoutedEventArgs e)
        {
            UpdateThumbPosition(animate: false);
        }

        private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateThumbPosition(animate: false);
        }

        private void UpdateThumbPosition(bool animate)
        {
            if (_thumb == null || _canvasParent == null)
            {
                return;
            }

            if (_canvasParent.ActualWidth <= 0 || _thumb.ActualWidth <= 0)
            {
                return;
            }

            var marginLeft = _thumb.Margin.Left;
            var offLeft = 0.0;
            var desiredOnLeftEdge = _canvasParent.ActualWidth - _thumb.ActualWidth - 4;
            var onLeft = Math.Max(0, desiredOnLeftEdge - marginLeft);

            var target = IsOn ? onLeft : offLeft;

            if (animate)
            {
                var animation = new DoubleAnimation
                {
                    To = target,
                    Duration = TimeSpan.FromMilliseconds(200),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };

                _thumb.BeginAnimation(Canvas.LeftProperty, animation);
            }
            else
            {
                _thumb.BeginAnimation(Canvas.LeftProperty, null);
                Canvas.SetLeft(_thumb, target);
            }
        }
    }
}
