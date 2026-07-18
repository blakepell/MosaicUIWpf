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
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;

namespace BbsNavigator.Converters
{
    /// <summary>
    /// Displays an enum value using its <see cref="DescriptionAttribute"/> text.
    /// </summary>
    public sealed class EnumDescriptionConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Gets the shared converter instance.
        /// </summary>
        public static EnumDescriptionConverter Instance { get; } = new();

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }

        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Enum enumValue)
            {
                return value;
            }

            FieldInfo? field = enumValue.GetType().GetField(enumValue.ToString());
            return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? enumValue.ToString();
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
