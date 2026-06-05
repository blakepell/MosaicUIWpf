/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mosaic.UI.Wpf.Converters
{
    /// <summary>
    /// Converts a <see cref="TreeViewItem"/> to the negative left margin needed to stretch
    /// its selection background to the left edge of the tree view.
    /// </summary>
    public sealed class TreeViewItemSelectionMarginConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the indent width used by each tree view level.
        /// </summary>
        public double IndentWidth { get; set; } = 19.0;

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not System.Windows.Controls.TreeViewItem item)
            {
                return DependencyProperty.UnsetValue;
            }

            var depth = 0;
            DependencyObject? current = item;
            while ((current = VisualTreeHelper.GetParent(current)) != null)
            {
                if (current is System.Windows.Controls.TreeViewItem)
                {
                    depth++;
                }
            }

            return new Thickness(-(depth + 1) * IndentWidth, 0, 0, 0);
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
