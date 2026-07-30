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

using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a scroll viewer that supports inertia-based scrolling animations.
    /// </summary>
    /// <remarks>
    /// The <see cref="InertiaScrollViewer"/> extends the <see cref="ScrollViewer"/> class to provide
    /// smooth, animated scrolling behavior when the <see cref="IsScrollAnimation"/> property is enabled. This is
    /// particularly useful for scenarios where a more fluid scrolling experience is desired, such as in touch-based or
    /// mouse-wheel interactions.
    ///
    /// This is based on code from https://github.com/WPFDevelopersOrg/WPFDevelopers available via the MIT License.
    /// </remarks>
    public class InertiaScrollViewer : ScrollViewer
    {
        /// <summary>
        /// The destination of the current animation. Repeated wheel input accumulates from this value instead of
        /// from the partially animated offset, which preserves momentum.
        /// </summary>
        private double _targetVerticalOffset;

        private bool _isAnimating;

        /// <summary>
        /// Identifies the <see cref="IsScrollAnimation"/> dependency property.
        /// </summary>
        /// <remarks>This property determines whether scroll animations are enabled for the <see
        /// cref="InertiaScrollViewer"/>.</remarks>
        public static readonly DependencyProperty IsScrollAnimationProperty = DependencyProperty.Register(
            nameof(IsScrollAnimation), typeof(bool), typeof(InertiaScrollViewer), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether scroll animations are enabled. The default is
        /// <see langword="true"/>.
        /// </summary>
        public bool IsScrollAnimation
        {
            get => (bool)GetValue(IsScrollAnimationProperty);
            set => SetValue(IsScrollAnimationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="WheelScrollDistance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WheelScrollDistanceProperty = DependencyProperty.Register(
            nameof(WheelScrollDistance), typeof(double), typeof(InertiaScrollViewer),
            new FrameworkPropertyMetadata(320.0, null, CoerceWheelScrollDistance));

        /// <summary>
        /// Gets or sets the number of device-independent pixels added to the inertial destination for one standard
        /// mouse-wheel detent. The default is 320.
        /// </summary>
        [Category("Mosaic")]
        [Description("The inertial scroll distance, in pixels, for one standard mouse-wheel detent.")]
        public double WheelScrollDistance
        {
            get => (double)this.GetValue(WheelScrollDistanceProperty);
            set => this.SetValue(WheelScrollDistanceProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InertiaScrollViewer"/> class.
        /// </summary>
        public InertiaScrollViewer()
        {
            this.ScrollChanged += this.OnScrollChanged;
        }

        /// <summary>
        /// Identifies the <see cref="AnimationDurationMilliseconds"/> dependency property.
        /// </summary>
        /// <remarks>
        /// This property specifies the duration of the animation, in milliseconds, for scrolling
        /// inertia. The default value is 800 milliseconds.
        /// </remarks>
        public static readonly DependencyProperty AnimationDurationMillisecondsProperty = DependencyProperty.Register(
            nameof(AnimationDurationMilliseconds), typeof(int), typeof(InertiaScrollViewer), new PropertyMetadata(800));

        /// <summary>
        /// Gets or sets the duration of the animation in milliseconds.  The default value is 800ms.
        /// </summary>
        public int AnimationDurationMilliseconds
        {
            get => (int)GetValue(AnimationDurationMillisecondsProperty);
            set => SetValue(AnimationDurationMillisecondsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DesiredFrameRate"/> dependency property.
        /// </summary>
        /// <remarks>
        /// This property is used to specify the desired frame rate for the <see
        /// cref="InertiaScrollViewer"/>. The default value is 40.
        /// </remarks>
        public static readonly DependencyProperty DesiredFrameRateProperty = DependencyProperty.Register(
            nameof(DesiredFrameRate), typeof(int), typeof(InertiaScrollViewer), new PropertyMetadata(40));

        /// <summary>
        /// Gets or sets the desired frame rate for rendering or processing operations.  The default value is 40.
        /// </summary>
        public int DesiredFrameRate
        {
            get => (int)GetValue(DesiredFrameRateProperty);
            set => SetValue(DesiredFrameRateProperty, value);
        }

        /// <summary>
        /// Handles the <see cref="ScrollViewer.ScrollChanged"/> event to track vertical scroll changes.
        /// </summary>
        /// <param name="sender">The source of the event, expected to be an <see cref="InertiaScrollViewer"/>.</param>
        /// <param name="e">The event data containing information about the scroll change.</param>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0 && !_isAnimating)
            {
                _targetVerticalOffset = this.VerticalOffset;
            }
        }

        private static object CoerceWheelScrollDistance(DependencyObject d, object baseValue)
        {
            double value = (double)baseValue;

            return double.IsNaN(value) || double.IsInfinity(value) || value <= 0 ? 1.0 : value;
        }

        /// <summary>
        /// Handles the mouse wheel event to provide custom scrolling behavior.
        /// </summary>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!IsScrollAnimation)
            {
                base.OnMouseWheel(e);
                return;
            }
            double startingOffset = _isAnimating ? _targetVerticalOffset : this.VerticalOffset;
            double wheelDetents = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
            double newOffset = Math.Clamp(
                startingOffset - wheelDetents * this.WheelScrollDistance,
                0,
                this.ScrollableHeight);

            if (Math.Abs(newOffset - startingOffset) < 0.5)
            {
                // Leave the event unhandled so an enclosing viewer can continue scrolling at this boundary.
                return;
            }

            this.AnimateScroll(newOffset);
            e.Handled = true;
        }

        /// <summary>
        /// Animates the vertical scroll position to a specified value over a fixed duration.
        /// </summary>
        /// <remarks>The animation uses a cubic easing function with an "EaseOut" mode to create a smooth
        /// deceleration effect. The animation duration is fixed at 800 milliseconds, and the frame rate is limited to
        /// 40 frames per second.</remarks>
        /// <param name="toValue">The target vertical offset to scroll to.</param>
        /// <param name="onCompleted">An optional callback that is invoked when the animation completes. If not provided, no action is taken upon
        /// completion.</param>
        public void AnimateScroll(double toValue, Action? onCompleted = null)
        {
            double currentOffset = this.VerticalOffset;
            double targetOffset = Math.Clamp(toValue, 0, this.ScrollableHeight);

            // Store the current effective offset as the animation property's base before replacing its clock.
            // Otherwise removing the previous animation can briefly restore the attached property's default of zero.
            InertiaScrollViewerBehavior.SetVerticalOffset(this, currentOffset);
            this.BeginAnimation(InertiaScrollViewerBehavior.VerticalOffsetProperty, null);

            var animation = new DoubleAnimation
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                From = currentOffset,
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(this.AnimationDurationMilliseconds)
            };

            Timeline.SetDesiredFrameRate(animation, this.DesiredFrameRate);
            animation.Completed += (_, _) =>
            {
                // Make the destination the base value before removing the completed animation so the viewer stays
                // exactly where the easing finished.
                InertiaScrollViewerBehavior.SetVerticalOffset(this, targetOffset);
                this.BeginAnimation(InertiaScrollViewerBehavior.VerticalOffsetProperty, null);
                _targetVerticalOffset = targetOffset;
                _isAnimating = false;
                onCompleted?.Invoke();
            };

            _targetVerticalOffset = targetOffset;
            _isAnimating = true;
            this.BeginAnimation(InertiaScrollViewerBehavior.VerticalOffsetProperty, animation);
        }
    }
}
