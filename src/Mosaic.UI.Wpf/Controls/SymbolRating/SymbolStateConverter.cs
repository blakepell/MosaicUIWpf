/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Data;
using System.Windows.Markup;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converter to determine if a symbol should be highlighted.
    /// </summary>
    public class SymbolStateConverter : MarkupExtension, IMultiValueConverter
    {
        /// <summary>
        /// Returns the current instance of the markup extension.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 4 || values[0] is not int symbolIndex || values[1] is not int selectedCount || values[2] is not int hoveredIndex || values[3] is not bool isHoverPreviewEnabled)
            {
                return false;
            }

            if (parameter?.ToString() == "State")
            {
                // Return state string for styling
                if (isHoverPreviewEnabled && hoveredIndex >= symbolIndex)
                {
                    return "Hover";
                }

                if (selectedCount >= symbolIndex)
                {
                    return "Selected";
                }

                return "Normal";
            }

            // Return boolean - true if this symbol should be highlighted
            if (isHoverPreviewEnabled && hoveredIndex > 0)
            {
                return hoveredIndex >= symbolIndex;
            }

            return selectedCount >= symbolIndex;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
