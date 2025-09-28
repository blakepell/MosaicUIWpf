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
    /// A value converter that formats a <see cref="DateTime"/> value as a localized time string if the date is the
    /// current day; otherwise, returns the original <see cref="DateTime"/> value.
    /// </summary>
    public sealed class DateTimeDisplayConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Provides a singleton instance of the <see cref="DateTimeDisplayConverter"/> class.
        /// </summary>
        public static readonly DateTimeDisplayConverter Instance = new();

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <summary>
        /// Converts a <see cref="DateTime"/> value to a localized string representation of the time if the date is the
        /// current day;  otherwise, returns the original value.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                var now = DateTime.Now;

                if (dt.Day == now.Day && dt.Month == now.Month && dt.Year == now.Year)
                {
                    // It's the current day, so just show the time.
                    return dt.ToLocalTime().ToString("h:mm tt", culture);
                }

                return dt;
            }

            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
