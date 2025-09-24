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
        /// Stores the last recorded location as a double value.
        /// </summary>
        private static double _lastLocation;

        /// <summary>
        /// Identifies the <see cref="IsScrollAnimation"/> dependency property.
        /// </summary>
        /// <remarks>This property determines whether scroll animations are enabled for the <see
        /// cref="InertiaScrollViewer"/>.</remarks>
        public static readonly DependencyProperty IsScrollAnimationProperty = DependencyProperty.Register(
            nameof(IsScrollAnimation), typeof(bool), typeof(InertiaScrollViewer), new PropertyMetadata(false, OnIsScrollAnimationChanged));

        /// <summary>
        /// Gets or sets a value indicating whether scroll animations are enabled.
        /// </summary>
        public bool IsScrollAnimation
        {
            get => (bool)GetValue(IsScrollAnimationProperty);
            set => SetValue(IsScrollAnimationProperty, value);
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
        /// Handles changes to the <see cref="IsScrollAnimation"/> dependency property.
        /// </summary>
        /// <param name="d">The object on which the property value has changed. Expected to be an instance of <see
        /// cref="InertiaScrollViewer"/>.</param>
        /// <param name="e">Provides data about the property change, including the old and new values.</param>
        private static void OnIsScrollAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not InertiaScrollViewer ctrl)
            {
                return;
            }

            if (ctrl.IsScrollAnimation)
            {
                ctrl.ScrollChanged -= OnScrollChanged;
                ctrl.ScrollChanged += OnScrollChanged;
            }
            else
            {
                ctrl.ScrollChanged -= OnScrollChanged;
            }
        }

        /// <summary>
        /// Handles the <see cref="ScrollViewer.ScrollChanged"/> event to track vertical scroll changes.
        /// </summary>
        /// <param name="sender">The source of the event, expected to be an <see cref="InertiaScrollViewer"/>.</param>
        /// <param name="e">The event data containing information about the scroll change.</param>
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is not InertiaScrollViewer ctrl)
            {
                return;
            }

            if (e.VerticalChange != 0)
            {
                _lastLocation = ctrl.VerticalOffset;
            }
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
            var wheelChange = e.Delta;
            var newOffset = _lastLocation - wheelChange * 2;

            ScrollToVerticalOffset(_lastLocation);

            if (newOffset < 0)
            {
                newOffset = 0;
            }

            if (newOffset > ScrollableHeight)
            {
                newOffset = ScrollableHeight;
            }

            AnimateScroll(newOffset);

            _lastLocation = newOffset;
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
        public void AnimateScroll(double toValue, Action onCompleted = null)
        {
            BeginAnimation(InertiaScrollViewerBehavior.VerticalOffsetProperty, null);

            var animation = new DoubleAnimation
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                From = VerticalOffset,
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(this.AnimationDurationMilliseconds)
            };

            Timeline.SetDesiredFrameRate(animation, this.DesiredFrameRate);
            animation.Completed += (s, e) => onCompleted?.Invoke();
            BeginAnimation(InertiaScrollViewerBehavior.VerticalOffsetProperty, animation);
        }
    }
}