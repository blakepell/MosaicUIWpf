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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides attached properties and behaviors for managing the vertical scroll offset of an <see
    /// cref="InertiaScrollViewer"/>.
    /// </summary>
    /// <remarks>
    /// This is based on code from https://github.com/WPFDevelopersOrg/WPFDevelopers available via the MIT License.
    /// </remarks>
    public static class InertiaScrollViewerBehavior
    {
        /// <summary>
        /// Identifies the VerticalOffset attached property, which specifies the vertical scroll offset for a scroll
        /// viewer.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached(
            "VerticalOffset", typeof(double), typeof(InertiaScrollViewerBehavior), new UIPropertyMetadata(0.0, OnVerticalOffsetChanged));

        /// <summary>
        /// Sets the vertical offset value for the specified <see cref="FrameworkElement"/>.
        /// </summary>
        /// <remarks>The vertical offset is applied to the specified <see cref="FrameworkElement"/> using
        /// the attached property <c>VerticalOffsetProperty</c>. Ensure that the <paramref name="target"/> is not
        /// <c>null</c> before calling this method.</remarks>
        /// <param name="target">The <see cref="FrameworkElement"/> on which to set the vertical offset.</param>
        /// <param name="value">The vertical offset value to set. This value determines the vertical positioning of the element.</param>
        public static void SetVerticalOffset(FrameworkElement target, double value)
        {
            target.SetValue(VerticalOffsetProperty, value);
        }

        /// <summary>
        /// Gets the vertical offset value of the specified <see cref="FrameworkElement"/>.
        /// </summary>
        /// <param name="target">The <see cref="FrameworkElement"/> from which to retrieve the vertical offset.</param>
        /// <returns>The vertical offset value as a <see cref="double"/>.</returns>
        public static double GetVerticalOffset(FrameworkElement target)
        {
            return (double)target.GetValue(VerticalOffsetProperty);
        }

        /// <summary>
        /// Handles changes to the VerticalOffset dependency property.
        /// </summary>
        /// <remarks>This method updates the vertical scroll position of the <see
        /// cref="InertiaScrollViewer"/> to the new offset value.</remarks>
        /// <param name="target">The object on which the property change occurred. This must be an instance of <see
        /// cref="InertiaScrollViewer"/>.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnVerticalOffsetChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as InertiaScrollViewer)?.ScrollToVerticalOffset((double)e.NewValue);
        }
    }
}
