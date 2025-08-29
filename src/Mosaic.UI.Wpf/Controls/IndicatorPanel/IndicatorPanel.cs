/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media.Animation;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// An info card with a highlight color on the left hand side.
    /// </summary>
    public class IndicatorPanel : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="AccentBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
            nameof(AccentBrush),
            typeof(Brush),
            typeof(IndicatorPanel),
            new PropertyMetadata(Brushes.CornflowerBlue, OnAccentBrushChanged));

        /// <summary>
        /// Gets or sets the brush used to render the accent color in the UI.  To hide the accent change its
        /// brush color to Transparent.
        /// </summary>
        public Brush AccentBrush
        {
            get => (Brush)GetValue(AccentBrushProperty);
            set => SetValue(AccentBrushProperty, value);
        }

        /// <summary>
        /// Read-only AnimatedAccentBrush used by the template (this is what is actually shown and animated).
        /// </summary>
        private static readonly DependencyPropertyKey AnimatedAccentBrushPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AnimatedAccentBrush), typeof(Brush), typeof(IndicatorPanel), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Identifies the <see cref="AnimatedAccentBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimatedAccentBrushProperty =
            AnimatedAccentBrushPropertyKey.DependencyProperty;

        /// <summary>
        /// The brush currently being animated/displayed.
        /// </summary>
        public Brush AnimatedAccentBrush => (Brush)GetValue(AnimatedAccentBrushProperty);

        /// <summary>
        /// Represents the brush used for rendering animations.
        /// </summary>
        private readonly SolidColorBrush _animationBrush;

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(IndicatorPanel), new PropertyMetadata("Info"));

        /// <summary>
        /// Gets or sets the title associated with the object that is hosted on the upper left hand
        /// side of the panel.
        /// </summary>
        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(IndicatorPanel));

        /// <summary>
        /// Gets or sets the content displayed in the header.
        /// </summary>
        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FooterContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FooterContentProperty =
            DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(IndicatorPanel));

        /// <summary>
        /// Gets or sets the content displayed in the footer of the control.
        /// </summary>
        public object FooterContent
        {
            get => GetValue(FooterContentProperty);
            set => SetValue(FooterContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SeparatorVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeparatorVisibilityProperty = DependencyProperty.Register(
            nameof(SeparatorVisibility), typeof(Visibility), typeof(IndicatorPanel), new PropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Gets or sets the visibility of the separator element.
        /// </summary>
        public Visibility SeparatorVisibility
        {
            get => (Visibility)GetValue(SeparatorVisibilityProperty);
            set => SetValue(SeparatorVisibilityProperty, value);
        }

        /// <summary>
        /// Initializes the <see cref="IndicatorPanel"/> class and overrides the default style key metadata.
        /// </summary>
        static IndicatorPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IndicatorPanel), new FrameworkPropertyMetadata(typeof(IndicatorPanel)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndicatorPanel"/> class.
        /// </summary>
        public IndicatorPanel()
        {
            // Initialize the internal animation brush with the starting AccentBrush color.
            var startColor = GetColorFromBrush(AccentBrush);
            _animationBrush = new SolidColorBrush(startColor);
            SetValue(AnimatedAccentBrushPropertyKey, _animationBrush);
        }

        /// <summary>
        /// Handles changes to the <see cref="AccentBrush"/> dependency property by animating the transition between the
        /// previous and new brush colors.
        /// </summary>
        private static void OnAccentBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not IndicatorPanel panel)
            {
                return;
            }

            var fromColor = panel._animationBrush.Color; // current visible color
            var toColor = GetColorFromBrush(e.NewValue as Brush);

            // Build keyframe animation: old -> new (.5s)
            var animation = new ColorAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(1),
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.KeyFrames.Add(new DiscreteColorKeyFrame(fromColor, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new LinearColorKeyFrame(toColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(.5))));

            // Restart animation from current color each time (SnapshotAndReplace to avoid stacking)
            panel._animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        /// <summary>
        /// Retrieves the color associated with the specified <see cref="Brush"/>.
        /// </summary>
        private static Color GetColorFromBrush(Brush? brush)
        {
            switch (brush)
            {
                case SolidColorBrush scb:
                    return scb.Color;
                case GradientBrush { GradientStops.Count: > 0 } gb:
                    return gb.GradientStops[0].Color;
                default:
                    return Colors.Transparent;
            }
        }
    }
}
