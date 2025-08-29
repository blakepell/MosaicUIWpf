/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                element.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
