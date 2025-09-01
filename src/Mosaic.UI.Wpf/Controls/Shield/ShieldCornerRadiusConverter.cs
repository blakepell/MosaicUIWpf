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

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Converts a CornerRadius and position parameter into a selective CornerRadius for use with the shield control.
    /// </summary>
    public class ShieldCornerRadiusConverter : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// Returns the current instance of the markup extension.
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <summary>
        /// Converts a CornerRadius to a selective CornerRadius based on position.
        /// </summary>
        /// <param name="value">The source CornerRadius</param>
        /// <param name="targetType">The target type</param>
        /// <param name="parameter">Position parameter: "Left" or "Right"</param>
        /// <param name="culture">Culture info</param>
        /// <returns>A CornerRadius with selective corners applied</returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CornerRadius cornerRadius && parameter is string position)
            {
                return position.ToLower() switch
                {
                    "left" => new CornerRadius(cornerRadius.TopLeft, 0, 0, cornerRadius.BottomLeft),
                    "right" => new CornerRadius(0, cornerRadius.TopRight, cornerRadius.BottomRight, 0),
                    _ => new CornerRadius(0)
                };
            }
            
            return new CornerRadius(0);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}