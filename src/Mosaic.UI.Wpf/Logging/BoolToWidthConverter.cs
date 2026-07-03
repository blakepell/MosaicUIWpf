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

namespace Mosaic.UI.Wpf.Logging
{
    /// <summary>
    /// Converts a boolean value to a column width.
    /// </summary>
    public sealed class BoolToWidthConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not true)
            {
                return 0d;
            }

            if (parameter is string text && double.TryParse(text, NumberStyles.Float, culture, out var width))
            {
                return width;
            }

            return parameter is double doubleWidth ? doubleWidth : 100d;
        }

        /// <inheritdoc/>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
