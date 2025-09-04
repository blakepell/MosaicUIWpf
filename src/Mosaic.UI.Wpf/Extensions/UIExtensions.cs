/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Extensions
{
    /// <summary>
    /// Provides an attached property to control the visibility of any FrameworkElement using a boolean value.
    /// </summary>
    public static class UIExtensions
    {
        /// <summary>
        /// Gets the IsVisible attached property.
        /// </summary>
        public static bool GetIsVisible(FrameworkElement element)
        {
            return (bool)element.GetValue(IsVisibleProperty);
        }

        /// <summary>
        /// Sets the IsVisible attached property.
        /// </summary>
        public static void SetIsVisible(FrameworkElement element, bool value)
        {
            element.SetValue(IsVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the IsVisible attached property.
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(UIExtensions), new PropertyMetadata(true, OnIsVisibleChanged));

        /// <summary>
        /// Handles changes to the <see cref="IsVisible"/> dependency property.
        /// </summary>
        /// <remarks>
        /// Updates the <see cref="FrameworkElement.Visibility"/> property of the target object
        /// based on the new value of the <see cref="IsVisible"/> property. Sets the visibility to <see
        /// cref="Visibility.Visible"/> if the new value is <see langword="true"/>; otherwise, sets it to <see
        /// cref="Visibility.Collapsed"/>.
        /// 
        /// Usage Example:
        /// extensions:UIExtensions.IsVisible="{Binding IsVisible}"
        /// </remarks>
        /// <param name="d">The dependency object on which the property value has changed. Must be a <see cref="FrameworkElement"/>.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                element.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
