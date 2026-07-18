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
    /// Converts an encrypted credential value into a value that indicates whether it is present.
    /// </summary>
    public sealed class HasCredentialsConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Gets the shared converter instance.
        /// </summary>
        public static HasCredentialsConverter Instance { get; } = new();

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Instance;
        }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string encryptedCredentials && !string.IsNullOrWhiteSpace(encryptedCredentials);
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
