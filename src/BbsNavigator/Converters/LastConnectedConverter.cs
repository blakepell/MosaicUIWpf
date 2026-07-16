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
using System.Windows.Markup;

namespace BbsNavigator.Converters
{
    /// <summary>
    /// Formats the last connection date for display in the BBS directory.
    /// </summary>
    public sealed class LastConnectedConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Gets the shared converter instance.
        /// </summary>
        public static LastConnectedConverter Instance { get; } = new();

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is DateTime lastConnected
                ? $"Last Connected: {lastConnected.ToString("g", culture)}"
                : "Last Connected: Never";
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
