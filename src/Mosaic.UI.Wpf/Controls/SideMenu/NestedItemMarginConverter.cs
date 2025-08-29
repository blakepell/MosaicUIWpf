/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts a <see cref="SideMenuItemControl"/> to a <see cref="Thickness"/> value representing its left margin,
    /// based on its nesting level within parent containers named "ChildrenContainer".
    /// </summary>
    /// <remarks>This converter calculates the left margin for a nested item in a hierarchical structure. The
    /// margin is determined by the item's nesting level within parent containers named "ChildrenContainer". The base
    /// margin is 3 pixels, with an additional 20 pixels added for each level of nesting.</remarks>
    public class NestedItemMarginConverter : IValueConverter
    {
        /// <summary>
        /// Provides a singleton instance of the <see cref="NestedItemMarginConverter"/> class.
        /// </summary>
        /// <remarks>This instance can be used to convert nested item margins without the need to create
        /// multiple instances of the <see cref="NestedItemMarginConverter"/> class.</remarks>
        public static readonly NestedItemMarginConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SideMenuItemControl control)
            {
                int nestingLevel = 0;
                var parent = VisualTreeHelper.GetParent(control);

                // Count how many "ChildrenContainer" ItemsControls we're nested within
                while (parent != null)
                {
                    if (parent is ItemsControl itemsControl && itemsControl.Name == "ChildrenContainer")
                    {
                        nestingLevel++;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }

                // Calculate left margin: base margin (3) + (20 pixels * nesting level)
                double leftMargin = 3 + (20 * nestingLevel);
                return new Thickness(leftMargin, 0, 0, 0);
            }
            return new Thickness(3, 0, 0, 0); // Default margin
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
