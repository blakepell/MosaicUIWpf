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
    /// Provides a value converter that inverts a boolean value.
    /// </summary>
    /// <remarks>This converter is typically used in data binding scenarios where a boolean value needs to be
    /// inverted. For example, it can be used to bind a <see langword="true"/> value to a property that expects <see
    /// langword="false"/>.</remarks>
    public sealed class InvertedBoolConverter : MarkupExtension, IValueConverter
    {
        public static readonly InvertedBoolConverter Instance = new();

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <summary>
        /// Converts a boolean value to its negated value.
        /// </summary>
        /// <param name="value">The value to convert. Must be of type <see cref="bool"/> or <see langword="null"/>.</param>
        /// <param name="targetType">The type to convert to. This parameter is not used in the conversion.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the conversion.</param>
        /// <param name="culture">The culture to use in the conversion. This parameter is not used in the conversion.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is <see langword="false"/>;  <see langword="false"/> if
        /// <paramref name="value"/> is <see langword="true"/> or not a <see cref="bool"/>.</returns>
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        /// <summary>
        /// Converts a value back to its original type.
        /// </summary>
        /// <param name="value">The value to be converted back. Can be <see langword="null"/>.</param>
        /// <param name="targetType">The type to which the value is being converted. This parameter is not used in the current implementation.</param>
        /// <param name="parameter">An optional parameter for the conversion. This parameter is not used in the current implementation.</param>
        /// <param name="culture">The culture to use during the conversion. This parameter is not used in the current implementation.</param>
        /// <returns>The original value passed as <paramref name="value"/>.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
