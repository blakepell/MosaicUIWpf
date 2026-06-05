using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides a scroll viewer with optional animated wheel scrolling.
    /// </summary>
    public class WDScrollViewer : ScrollViewer
    {
        private static double _lastLocation;

        /// <summary>
        /// Gets or sets a value that indicates whether scroll wheel animation is enabled.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if scroll wheel movement is animated; otherwise, <see langword="false" />.
        /// The default is <see langword="false" />.
        /// </value>
        public bool IsScrollAnimation
        {
            get { return (bool)GetValue(IsScrollAnimationProperty); }
            set { SetValue(IsScrollAnimationProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsScrollAnimation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsScrollAnimationProperty =
            DependencyProperty.Register("IsScrollAnimation", typeof(bool), typeof(WDScrollViewer), new PropertyMetadata(false, OnIsScrollAnimationChanged));

        /// <summary>
        /// Responds to changes in the scroll animation setting.
        /// </summary>
        /// <param name="d">The dependency object that owns the property.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnIsScrollAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as WDScrollViewer;
            if (ctrl == null)
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
        /// Captures the latest vertical offset when the viewer scrolls.
        /// </summary>
        /// <param name="sender">The source of the scroll event.</param>
        /// <param name="e">The event data for the scroll change.</param>
        private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var ctrl = sender as WDScrollViewer;
            if (ctrl == null)
            {
                return;
            }

            if (e.VerticalChange != 0)
            {
                _lastLocation = ctrl.VerticalOffset;
            }
        }

        /// <summary>
        /// Processes mouse-wheel input and optionally animates the resulting scroll.
        /// </summary>
        /// <param name="e">The event data for the mouse-wheel input.</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!IsScrollAnimation)
            {
                base.OnMouseWheel(e);
                return;
            }
            var WheelChange = e.Delta;
            var newOffset = _lastLocation - WheelChange * 2;
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
        /// Animates the vertical scroll offset to the specified value.
        /// </summary>
        /// <param name="toValue">The target vertical offset.</param>
        /// <param name="onCompleted">The callback to invoke when the animation completes.</param>
        public void AnimateScroll(double toValue, Action onCompleted = null)
        {
            BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, null);
            var animation = new DoubleAnimation
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                From = VerticalOffset,
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(800)
            };
            Timeline.SetDesiredFrameRate(animation, 40);
            animation.Completed += (s, e) => onCompleted?.Invoke();
            BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
        }
    }
}