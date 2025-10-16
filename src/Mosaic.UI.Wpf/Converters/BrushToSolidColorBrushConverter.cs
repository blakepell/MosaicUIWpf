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
    /// Converts a <see cref="Brush"/> to a <see cref="SolidColorBrush"/> and vice versa, primarily for use in data
    /// binding scenarios.
    /// </summary>
    /// <remarks>
    /// This converter is typically used in XAML to ensure that a <see cref="Brush"/> provided to the
    /// UI is treated as a <see cref="SolidColorBrush"/> when converting back to the source. The <see cref="Convert"/>
    /// method passes the input value through unchanged, while the <see cref="ConvertBack"/> method attempts to cast
    /// the input value to a <see cref="SolidColorBrush"/>.
    /// </remarks>
    public class BrushToSolidColorBrushConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Provides a singleton instance of the <see cref="BoolToBrushConverter"/> class.
        /// </summary>
        public static readonly BoolToBrushConverter Instance = new();

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // We expect a Brush, so just pass it through.
            return value;
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // The UI is providing a Brush, try to cast it as a SolidColorBrush for the source.
            return value as SolidColorBrush;
        }
    }
}