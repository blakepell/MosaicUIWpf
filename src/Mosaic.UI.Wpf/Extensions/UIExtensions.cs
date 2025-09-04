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
        #region IsVisible Attached Property

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

        #endregion

        #region IsVisibleIfNotNullOrEmpty Attached Property

        /// <summary>
        /// Gets the IsVisibleIfNotNullOrEmpty attached property (string).
        /// </summary>
        public static string GetIsVisibleIfNotNullOrEmpty(FrameworkElement element)
        {
            return (string)element.GetValue(IsVisibleIfNotNullOrEmptyProperty);
        }

        /// <summary>
        /// Sets the IsVisibleIfNotNullOrEmpty attached property (string).
        /// </summary>
        public static void SetIsVisibleIfNotNullOrEmpty(FrameworkElement element, string value)
        {
            element.SetValue(IsVisibleIfNotNullOrEmptyProperty, value);
        }

        /// <summary>
        /// Identifies the IsVisibleIfNotNullOrEmpty attached property.
        /// </summary>
        public static readonly DependencyProperty IsVisibleIfNotNullOrEmptyProperty =
            DependencyProperty.RegisterAttached("IsVisibleIfNotNullOrEmpty", typeof(string), typeof(UIExtensions), new PropertyMetadata(null, OnIsVisibleIfNotNullOrEmptyChanged));

        /// <summary>
        /// Handles changes to the <see cref="IsVisibleIfNotNullOrEmpty"/> dependency property.
        /// </summary>
        /// <remarks>
        /// Updates the <see cref="FrameworkElement.Visibility"/> property based on whether the bound string is non-null and non-empty.
        /// Sets Visibility.Visible when the string is not null or empty; otherwise Visibility.Collapsed.
        /// 
        /// Usage Example:
        /// extensions:UIExtensions.IsVisibleIfNotNullOrEmpty="{Binding SomeString}"
        /// </remarks>
        /// <param name="d">The dependency object on which the property value has changed. Must be a <see cref="FrameworkElement"/>.</param>
        /// <param name="e">The event data containing information about the property change, including the old and new values.</param>
        private static void OnIsVisibleIfNotNullOrEmptyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                var str = e.NewValue as string;
                element.Visibility = !string.IsNullOrEmpty(str) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion
    }
}
