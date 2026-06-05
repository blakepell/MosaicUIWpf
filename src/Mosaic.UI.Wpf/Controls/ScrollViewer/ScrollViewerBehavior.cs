namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides attached properties that synchronize a scroll viewer's vertical offset.
    /// </summary>
    public static class ScrollViewerBehavior
    {
        /// <summary>
        /// Identifies the <see cref="VerticalOffset"/> attached property.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
                new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

        /// <summary>
        /// Sets the vertical offset for the specified target.
        /// </summary>
        /// <param name="target">The element that owns the attached property.</param>
        /// <param name="value">The vertical offset to apply.</param>
        public static void SetVerticalOffset(FrameworkElement target, double value)
        {
            target.SetValue(VerticalOffsetProperty, value);
        }

        /// <summary>
        /// Gets the vertical offset for the specified target.
        /// </summary>
        /// <param name="target">The element that owns the attached property.</param>
        /// <returns>The current vertical offset value.</returns>
        public static double GetVerticalOffset(FrameworkElement target)
        {
            return (double)target.GetValue(VerticalOffsetProperty);
        }

        /// <summary>
        /// Responds to vertical offset changes by scrolling the target viewer.
        /// </summary>
        /// <param name="target">The dependency object that owns the property.</param>
        /// <param name="e">The event data for the property change.</param>
        private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as WDScrollViewer)?.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}