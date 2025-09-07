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

using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a toggle switch control that allows users to switch between two states, such as "On" and "Off".
    /// </summary>
    /// <remarks>The <see cref="ToggleSwitch"/> control provides a customizable toggle mechanism with visual
    /// feedback. It supports binding to a boolean value via the <see cref="IsOn"/> property, and allows customization
    /// of the displayed text and background colors for the "On" and "Off" states using the <see cref="OnText"/>, <see
    /// cref="OffText"/>, <see cref="OnBackgroundBrush"/>, and <see cref="OffBackgroundBrush"/> properties.  The control
    /// is designed to be templated, and it relies on specific template parts for its functionality: - A <see
    /// cref="Border"/> named "PART_Thumb" for the toggle thumb. - A <see cref="TextBlock"/> named "PART_OnTextBlock"
    /// for the "On" state text. - A <see cref="TextBlock"/> named "PART_OffTextBlock" for the "Off" state text.  The
    /// control handles user interaction, such as mouse clicks, to toggle its state.</remarks>
    [TemplatePart(Name = PartThumb, Type = typeof(Border))]
    [TemplatePart(Name = PartOnTextBlock, Type = typeof(TextBlock))]
    [TemplatePart(Name = PartOffTextBlock, Type = typeof(TextBlock))]
    public class ToggleSwitch : Control
    {
        #region Private Fields

        private const string PartThumb = "PART_Thumb";
        private const string PartOnTextBlock = "PART_OnTextBlock";
        private const string PartOffTextBlock = "PART_OffTextBlock";

        private Border? _thumb;
        private Canvas? _canvasParent;
        private TextBlock? _onTextBlock;
        private TextBlock? _offTextBlock;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="IsOn"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
            nameof(IsOn), typeof(bool), typeof(ToggleSwitch), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOnChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the control is in the "on" state.
        /// </summary>
        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        /// <summary>
        /// Called when the value of the <see cref="IsOn"/> dependency property changes.
        /// </summary>
        /// <param name="d">The <see cref="DependencyObject"/> on which the property value has changed.</param>
        /// <param name="e">The event data containing information about the property change.</param>
        private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ts = (ToggleSwitch)d;
            ts?.UpdateThumbPosition(true);
        }

        /// <summary>
        /// Identifies the <see cref="OnText"/> dependency property, which represents the text displayed when the toggle
        /// switch is in the "on" state.
        /// </summary>
        public static readonly DependencyProperty OnTextProperty = DependencyProperty.Register(
            nameof(OnText), typeof(string), typeof(ToggleSwitch), new PropertyMetadata("ON"));

        /// <summary>
        /// Gets or sets the text displayed when the control is in the "on" state.
        /// </summary>
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
        public string OffText
        {
            get => (string)GetValue(OffTextProperty);
            set => SetValue(OffTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OnBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OnBackgroundBrushProperty = DependencyProperty.Register(nameof(OnBackgroundBrush), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.Green));

        /// <summary>
        /// Gets or sets the brush used to render the background when the control is in the "on" state.
        /// </summary>
        public Brush OnBackgroundBrush
        {
            get => (Brush)GetValue(OnBackgroundBrushProperty);
            set => SetValue(OnBackgroundBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OffBackgroundBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffBackgroundBrushProperty = DependencyProperty.Register(nameof(OffBackgroundBrush), typeof(Brush), typeof(ToggleSwitch), new PropertyMetadata(Brushes.DarkGray));

        /// <summary>
        /// Gets or sets the background brush used when the control is in the "off" state.
        /// </summary>
        public Brush OffBackgroundBrush
        {
            get => (Brush)GetValue(OffBackgroundBrushProperty);
            set => SetValue(OffBackgroundBrushProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes static members of the <see cref="ToggleSwitch"/> class.
        /// </summary>
        /// <remarks>This static constructor overrides metadata for several dependency properties of the
        /// <see cref="ToggleSwitch"/> control: <list type="bullet"> <item> <description>Sets the default style key to
        /// associate the control with its default style.</description> </item> <item> <description>Marks the control as
        /// focusable by default.</description> </item> <item> <description>Sets the default width to 60.0 and the
        /// default height to 28.0.</description> </item> </list></remarks>
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
        /// <remarks>The default cursor for the toggle switch is set to <see cref="Cursors.Hand"/>, 
        /// indicating that the control is interactive.</remarks>
        public ToggleSwitch()
        {
            Cursor = Cursors.Hand;
        }

        /// <summary>
        /// Updates the user interface to reflect the current state of the control.
        /// </summary>
        /// <remarks>
        /// This method adjusts the background and the visibility of text blocks based on the
        /// value of the <see cref="IsOn"/> property. If <see cref="_onTextBlock"/> or <see cref="_offTextBlock"/> are
        /// null, their visibility is not modified.
        /// </remarks>
        private void RefreshUI()
        {
            this.Background = IsOn ? OnBackgroundBrush : OffBackgroundBrush;

            if (_onTextBlock != null)
            {
                _onTextBlock.Visibility = IsOn ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_offTextBlock != null)
            {
                _offTextBlock.Visibility = IsOn ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Invoked whenever application code or internal processes call <see cref="ApplyTemplate"/>.  This method is
        /// used to reapply the control's template and initialize or update references  to template parts and event
        /// handlers.
        /// </summary>
        /// <remarks>
        /// This method ensures that the control's visual elements, such as the thumb and text
        /// blocks, are correctly initialized and event handlers are properly subscribed. It also updates the thumb's
        /// position and refreshes the control's appearance to reflect the current state.  If the control template is
        /// changed, this method will be called again to reinitialize the template parts and associated logic.
        /// </remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Unsubscribe previous handlers if any
            if (_thumb != null)
            {
                _thumb.Loaded -= OnThumbLoaded;
            }

            _thumb = GetTemplateChild(PartThumb) as Border;
            _canvasParent = _thumb?.Parent as Canvas;

            if (_thumb != null)
            {
                _thumb.Loaded += OnThumbLoaded;
            }

            if (_canvasParent != null)
            {
                _canvasParent.SizeChanged -= OnCanvasSizeChanged;
                _canvasParent.SizeChanged += OnCanvasSizeChanged;
            }

            _onTextBlock = GetTemplateChild(PartOnTextBlock) as TextBlock;
            _offTextBlock = GetTemplateChild(PartOffTextBlock) as TextBlock;

            // Handle clicks anywhere on the control
            this.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;

            // Set initial position without animation
            this.UpdateThumbPosition(false);

            // Refresh colors, etc.
            this.RefreshUI();
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> event for the thumb control.
        /// </summary>
        /// <param name="sender">The source of the event, typically the thumb control.</param>
        /// <param name="e">The event data associated with the <see cref="RoutedEventArgs"/>.</param>
        private void OnThumbLoaded(object? sender, RoutedEventArgs e)
        {
            UpdateThumbPosition(false);
        }

        /// <summary>
        /// Handles the event triggered when the size of the canvas changes.
        /// </summary>
        /// <param name="sender">The source of the event. This can be <see langword="null"/>.</param>
        /// <param name="e">The event data containing information about the new and previous sizes of the canvas.</param>
        private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Recalculate target positions when container resizes
            UpdateThumbPosition(false);
        }

        /// <summary>
        /// Handles the left mouse button down event and toggles the state of the control.
        /// </summary>
        /// <param name="sender">The source of the event, typically the control that was clicked.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs"/> containing event data, including the mouse button state.</param>
        private void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // Toggle state on click anywhere within the control
            IsOn = !IsOn;
            e.Handled = true;

            this.RefreshUI();
        }

        /// <summary>
        /// Updates the position of the thumb control within its parent canvas, optionally animating the movement.
        /// </summary>
        /// <param name="animate">A value indicating whether the thumb's movement should be animated.  <see langword="true"/> to animate the
        /// movement; otherwise, <see langword="false"/>.</param>
        private void UpdateThumbPosition(bool animate)
        {
            if (_thumb == null || _canvasParent == null)
            {
                return;
            }

            // Ensure we have measured sizes
            if (_canvasParent.ActualWidth <= 0 || _thumb.ActualWidth <= 0)
            {
                return;
            }

            // The template sets Margin="4" on the thumb; when using Canvas.Left the actual left edge is Canvas.Left + Margin.Left.
            // We want the thumb's left edge to be 4 when off, and (parentWidth - thumbWidth - 4) when on.
            // Therefore Canvas.Left (value we set) should be desiredLeft - marginLeft.

            double marginLeft = _thumb.Margin.Left;

            double offLeft = 0; // desired left edge 4 => Canvas.Left = 0 because Margin.Left == 4
            double desiredOnLeftEdge = _canvasParent.ActualWidth - _thumb.ActualWidth - 4;
            double onLeft = Math.Max(0, desiredOnLeftEdge - marginLeft);

            double target = IsOn ? onLeft : offLeft;

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
                _thumb.BeginAnimation(Canvas.LeftProperty, null); // stop any animation
                Canvas.SetLeft(_thumb, target);
            }
        }
    }
}
