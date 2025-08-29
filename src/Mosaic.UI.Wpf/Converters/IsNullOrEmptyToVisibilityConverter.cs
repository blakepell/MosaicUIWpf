/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Converters
{
    /// <summary>
    /// Converts a string value to a <see cref="Visibility"/> value based on whether the string is null or consists only
    /// of white-space characters.
    /// </summary>
    public class IsNullOrWhiteSpaceToVisibilityConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Returns the current instance of the markup extension.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <summary>
        /// Converts the specified value to a <see cref="Visibility"/> value based on its content.
        /// </summary>
        /// <param name="value">The value to convert. If <paramref name="value"/> is <see langword="null"/> or an empty or whitespace
        /// string, the method returns <see cref="Visibility.Collapsed"/>.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in the conversion process.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion process.</param>
        /// <param name="culture">The culture information to use during the conversion. This parameter is not used in the conversion process.</param>
        /// <returns>A <see cref="Visibility"/> value. Returns <see cref="Visibility.Collapsed"/> if <paramref name="value"/> is
        /// <see langword="null"/> or an empty or whitespace string; otherwise, returns <see
        /// cref="Visibility.Visible"/>.</returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
