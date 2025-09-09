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
using Mosaic.UI.Wpf.Cache;

namespace Mosaic.UI.Wpf.Converters
{
    /// <summary>
    /// Converts a boolean value to a brush specified by hex values.  The ConverterParameter can be used to specify the
    /// hex value brushes, delimited by comma, semicolon, or pipe.
    /// </summary>
    /// <remarks>
    /// <![CDATA[
    /// Example usage in XAML:
    ///
    /// <Window.Resources>
    ///     <local:BoolToBrushConverter x:Key="BoolToBrushConverter" />
    /// </Window.Resources>
    ///
    /// <TextBlock Text="{Binding IsEnabled, Converter={StaticResource BoolToBrushConverter}, ConverterParameter='#267F00,#FF0000'}" />
    /// ]]>
    /// </remarks>
    public sealed class BoolToBrushConverter : MarkupExtension, IValueConverter
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

        /// <summary>
        /// Class constructor
        /// </summary>
        public BoolToBrushConverter()
        {

        }

        /// <summary>
        /// Converts a value to a string representation based on its boolean equivalent and optional labels.
        /// </summary>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "n/a";
            }

            // Determine boolean value
            bool flag;

            if (value is bool b)
            {
                flag = b;
            }
            else if (value is string s && bool.TryParse(s, out var parsed))
            {
                flag = parsed;
            }
            else
            {
                try
                {
                    flag = System.Convert.ToBoolean(value, culture);
                }
                catch
                {
                    flag = false;
                }
            }

            // Defaults
            Brush trueLabel = Brushes.White;
            Brush falseLabel = Brushes.DarkGray;

            // Only handle null, blank, or delimited string parameter
            if (parameter is string paramStr && !string.IsNullOrWhiteSpace(paramStr))
            {
                var parts = SplitLabels(paramStr);
                if (parts.Length >= 2)
                {
                    trueLabel = ColorPaletteCache.GetBrush(parts[0]);
                    falseLabel = ColorPaletteCache.GetBrush(parts[1]);
                }
                else if (parts.Length == 1)
                {
                    trueLabel = ColorPaletteCache.GetBrush(parts[0]);
                    falseLabel = trueLabel;
                }
            }

            return flag ? trueLabel : falseLabel;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Splits a string into labels based on common delimiters (comma, semicolon, pipe).
        /// </summary>
        /// <param name="input"></param>
        private static string[] SplitLabels(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return [];
            }

            char[] delims = [',', ';', '|'];
            return input
                .Split(delims, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
        }
    }
}