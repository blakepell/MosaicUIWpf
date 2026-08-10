/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media.Animation;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// An info card with a highlight color on the left hand side.
    /// </summary>
    [TemplatePart(Name = PartContent, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartCollapseArea, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartChevronButton, Type = typeof(System.Windows.Controls.Primitives.ToggleButton))]
    [DefaultProperty(nameof(Content))]
    public class ContentPanel : ContentControl
    {
        private const string PartContent = "PART_Content";
        private const string PartCollapseArea = "PART_CollapseArea";
        private const string PartChevronButton = "PART_ChevronButton";

        /// <summary>
        /// The collapsible region of the template (separator, content and footer).
        /// </summary>
        private FrameworkElement? _collapseArea;

        /// <summary>
        /// Incremented every time an expand/collapse animation starts so a superseded
        /// animation's completion handler does not apply a stale final state.
        /// </summary>
        private int _animationToken;

        public new static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
            nameof(Background), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        public new Brush Background
        {
            get => (Brush)this.GetValue(BackgroundProperty);
            set => this.SetValue(BackgroundProperty, value);
        }

        public new static readonly DependencyProperty OpacityProperty = DependencyProperty.Register(
            nameof(Opacity), typeof(double), typeof(ContentPanel), new PropertyMetadata(1.0));

        public new double Opacity
        {
            get => (double)this.GetValue(OpacityProperty);
            set => this.SetValue(OpacityProperty, value);
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(ContentPanel), new PropertyMetadata("Info"));

        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(ContentPanel));

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        public static readonly DependencyProperty FooterContentProperty =
            DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(ContentPanel));

        public object FooterContent
        {
            get => GetValue(FooterContentProperty);
            set => SetValue(FooterContentProperty, value);
        }

        public static readonly DependencyProperty SeparatorVisibilityProperty = DependencyProperty.Register(
            nameof(SeparatorVisibility), typeof(Visibility), typeof(ContentPanel), new PropertyMetadata(Visibility.Visible));

        public Visibility SeparatorVisibility
        {
            get => (Visibility)GetValue(SeparatorVisibilityProperty);
            set => SetValue(SeparatorVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderVisibilityProperty = DependencyProperty.Register(
            nameof(HeaderVisibility), typeof(Visibility), typeof(ContentPanel), new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Gets or sets the visibility of the header area.
        /// </summary>
        public Visibility HeaderVisibility
        {
            get => (Visibility)GetValue(HeaderVisibilityProperty);
            set => SetValue(HeaderVisibilityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(ContentPanel), new PropertyMetadata(new CornerRadius(5)));

        /// <summary>
        /// Gets or sets the radius used to round the panel corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderBackgroundProperty = DependencyProperty.Register(
            nameof(HeaderBackground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint the header background.
        /// </summary>
        public Brush HeaderBackground
        {
            get => (Brush)GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderForegroundProperty = DependencyProperty.Register(
            nameof(HeaderForeground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint header text.
        /// </summary>
        public Brush HeaderForeground
        {
            get => (Brush)GetValue(HeaderForegroundProperty);
            set => SetValue(HeaderForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FooterBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterBackgroundProperty = DependencyProperty.Register(
            nameof(FooterBackground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint the footer background.
        /// </summary>
        public Brush FooterBackground
        {
            get => (Brush)GetValue(FooterBackgroundProperty);
            set => SetValue(FooterBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FooterForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterForegroundProperty = DependencyProperty.Register(
            nameof(FooterForeground), typeof(Brush), typeof(ContentPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Gets or sets the brush used to paint footer text.
        /// </summary>
        public Brush FooterForeground
        {
            get => (Brush)GetValue(FooterForegroundProperty);
            set => SetValue(FooterForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowChevron"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowChevronProperty = DependencyProperty.Register(
            nameof(ShowChevron), typeof(bool), typeof(ContentPanel), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether a chevron is shown on the left hand side of the
        /// header. The chevron expands and collapses the content and footer areas of the panel.
        /// </summary>
        [Category("Common")]
        [Description("Shows a chevron in the header that expands and collapses the panel.")]
        public bool ShowChevron
        {
            get => (bool)GetValue(ShowChevronProperty);
            set => SetValue(ShowChevronProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
            nameof(IsOpen), typeof(bool), typeof(ContentPanel),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the panel is open. When closed only the header
        /// remains visible; the content and footer areas are animated shut.
        /// </summary>
        [Category("Common")]
        [Description("Whether the content and footer areas are expanded. When false only the header is shown.")]
        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AnimationDuration"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
            nameof(AnimationDuration), typeof(Duration), typeof(ContentPanel),
            new FrameworkPropertyMetadata(new Duration(TimeSpan.FromMilliseconds(180))));

        /// <summary>
        /// Gets or sets how long the expand and collapse animation takes.
        /// </summary>
        [Category("Common")]
        [Description("The duration of the expand and collapse animation.")]
        public Duration AnimationDuration
        {
            get => (Duration)GetValue(AnimationDurationProperty);
            set => SetValue(AnimationDurationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Opened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(Opened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ContentPanel));

        /// <summary>
        /// Occurs when the panel has been expanded.
        /// </summary>
        public event RoutedEventHandler Opened
        {
            add => AddHandler(OpenedEvent, value);
            remove => RemoveHandler(OpenedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="Closed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
            nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ContentPanel));

        /// <summary>
        /// Occurs when the panel has been collapsed.
        /// </summary>
        public event RoutedEventHandler Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        static ContentPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentPanel), new FrameworkPropertyMetadata(typeof(ContentPanel)));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _ = this.GetTemplateChild(PartContent) as ContentPresenter;
            _collapseArea = this.GetTemplateChild(PartCollapseArea) as FrameworkElement;

            // Apply the current state without animating; the panel may be templated while closed.
            this.SetCollapseAreaState(this.IsOpen, false);
        }

        /// <summary>
        /// Handles a change to <see cref="IsOpen"/> by animating the collapsible area open or shut.
        /// </summary>
        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (ContentPanel)d;
            bool isOpen = (bool)e.NewValue;

            panel.SetCollapseAreaState(isOpen, true);
            panel.RaiseEvent(new RoutedEventArgs(isOpen ? OpenedEvent : ClosedEvent, panel));
        }

        /// <summary>
        /// Expands or collapses the content/footer area, optionally with an animation.
        /// </summary>
        /// <param name="isOpen">Whether the area should end up open.</param>
        /// <param name="animate">Whether the transition should be animated.</param>
        private void SetCollapseAreaState(bool isOpen, bool animate)
        {
            var area = _collapseArea;

            if (area == null)
            {
                return;
            }

            // Cancel anything currently running and invalidate its completion handler.
            int token = unchecked(++_animationToken);
            area.BeginAnimation(HeightProperty, null);

            if (!animate)
            {
                area.Height = isOpen ? double.NaN : 0;
                area.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            if (isOpen)
            {
                area.Visibility = Visibility.Visible;
                double target = this.MeasureCollapseAreaHeight(area);

                if (target <= 0)
                {
                    // Nothing measurable to animate to (zero width, no content, etc).
                    area.Height = double.NaN;
                    return;
                }

                var animation = new DoubleAnimation(0, target, this.AnimationDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                animation.Completed += (_, _) =>
                {
                    if (token != _animationToken)
                    {
                        return;
                    }

                    // Release the animated height so the area sizes to its content again.
                    area.BeginAnimation(HeightProperty, null);
                    area.Height = double.NaN;
                };

                area.Height = 0;
                area.BeginAnimation(HeightProperty, animation);
            }
            else
            {
                double from = area.ActualHeight;

                var animation = new DoubleAnimation(from, 0, this.AnimationDuration)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };

                animation.Completed += (_, _) =>
                {
                    if (token != _animationToken)
                    {
                        return;
                    }

                    area.BeginAnimation(HeightProperty, null);
                    area.Height = 0;
                    area.Visibility = Visibility.Collapsed;
                };

                area.BeginAnimation(HeightProperty, animation);
            }
        }

        /// <summary>
        /// Measures the natural height of the collapsible area so it can be animated to an explicit value.
        /// </summary>
        /// <param name="area">The collapsible area.</param>
        private double MeasureCollapseAreaHeight(FrameworkElement area)
        {
            double width = area.ActualWidth > 0 ? area.ActualWidth : this.ActualWidth;

            if (width <= 0 || double.IsNaN(width))
            {
                width = double.PositiveInfinity;
            }

            double previousHeight = area.Height;
            area.Height = double.NaN;
            area.Measure(new Size(width, double.PositiveInfinity));

            double desired = area.DesiredSize.Height;
            area.Height = previousHeight;
            area.InvalidateMeasure();

            return desired;
        }
    }
}
