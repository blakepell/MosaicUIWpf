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
    /// Converts a <see cref="Color"/> to a <see cref="SolidColorBrush"/>.
    /// </summary>
    /// <remarks>
    /// This avoids placing bindings on Freezable properties inside templates which can
    /// produce "Cannot find governing FrameworkElement" binding errors.
    /// </remarks>
    public class ColorToBrushConverter : MarkupExtension, IValueConverter
    {
        public static readonly ColorToBrushConverter Instance = new();

        /// <inheritdoc />
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Brushes.Transparent;
            }

            if (value is Color c)
            {
                var brush = new SolidColorBrush(c);
                brush.Freeze();
                return brush;
            }

            // Try to handle Color? string or other representations
            try
            {
                var conv = ColorConverter.ConvertFromString(value.ToString());
                if (conv is Color c2)
                {
                    var brush = new SolidColorBrush(c2);
                    brush.Freeze();
                    return brush;
                }
            }
            catch
            {
                // Ignore
            }

            return Brushes.Transparent;
        }

        /// <inheritdoc/>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
