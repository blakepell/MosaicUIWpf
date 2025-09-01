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
    /// Converts an inverted boolean value into a configurable value of type <seealso cref="Visibility"/>.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class InvertedBoolToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public static readonly InvertedBoolToVisibilityConverter Instance = new();

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        public InvertedBoolToVisibilityConverter()
        {
            // set defaults
            TrueValue = Visibility.Collapsed;
            FalseValue = Visibility.Visible;
        }

        /// <summary>
        /// Gets/sets the <see cref="Visibility"/> value that is associated (converted into) with the boolean true value.
        /// </summary>
        private Visibility TrueValue { get; }

        /// <summary>
        /// Gets/sets the <see cref="Visibility"/> value that is associated (converted into) with the boolean false value.
        /// </summary>
        private Visibility FalseValue { get; }

        /// <summary>
        /// Converts a bool value into <see cref="Visibility"/> as configured in the <see cref="TrueValue"/> and <see cref="FalseValue"/> properties.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return FalseValue;
            }

            if (!(value is bool))
            {
                return FalseValue;
            }

            return (bool)value ? TrueValue : FalseValue;
        }

        /// <summary>
        /// Converts a <see cref="Visibility"/> value into bool as configured in the <see cref="TrueValue"/> and <see cref="FalseValue"/> properties.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return FalseValue;
            }

            if (Equals(value, TrueValue))
            {
                return true;
            }

            if (Equals(value, FalseValue))
            {
                return false;
            }

            return FalseValue;
        }
    }
}