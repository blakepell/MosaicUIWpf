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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides a mechanism for converting a value to a formatted string representation based on a specified format.
    /// </summary>
    public class StringFormatConverter : IValueConverter
    {
        /// <summary>
        /// Converts the specified value to a formatted string based on the provided format parameter and culture.
        /// </summary>
        /// <param name="value">The value to format. Can be <see langword="null"/>.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in this implementation.</param>
        /// <param name="parameter">A format string that specifies how the value should be formatted. Must be a valid format string.</param>
        /// <param name="culture">The culture to use for formatting. If the format string does not contain placeholders, this culture is used
        /// for formatting the value.</param>
        /// <returns>A string representation of the value formatted according to the specified format string and culture.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? formatParameter = parameter as string;

            if (formatParameter == null)
            {
                return value;
            }

            if (formatParameter.Contains('{'))
            {
                return string.Format(formatParameter, value);
            }

            return string.Format(culture, "{0:" + formatParameter + "}", value);
        }

        /// <summary>
        /// Converts a value back to its source type.
        /// </summary>
        /// <param name="value">The value produced by the binding target. This value is to be converted back to the source type.</param>
        /// <param name="targetType">The type to which the value is being converted.</param>
        /// <param name="parameter">An optional parameter to use during the conversion. This can be <see langword="null"/>.</param>
        /// <param name="culture">The culture to use in the conversion process.</param>
        /// <returns>The converted value, which is of the source type expected by the binding source.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented and will always throw this exception.</exception>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
