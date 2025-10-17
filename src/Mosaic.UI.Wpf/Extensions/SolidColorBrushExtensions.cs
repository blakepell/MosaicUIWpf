/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="System.Windows.Media.SolidColorBrush"/> conversions.
    /// </summary>
    public static class SolidColorBrushExtensions
    {
        /// <summary>
        /// Converts the brush's color to a hexadecimal string.
        /// </summary>
        /// <param name="brush">The <see cref="System.Windows.Media.SolidColorBrush"/> to convert.</param>
        /// <param name="includeAlpha">If true, includes the alpha channel as the first two hex digits.</param>
        /// <returns>
        /// A hex string representing the color. Example: "#RRGGBB" or "#AARRGGBB" when <paramref name="includeAlpha"/> is true.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="brush"/> is null.</exception>
        public static string ToHexString(this SolidColorBrush brush, bool includeAlpha = false)
        {
            if (brush == null)
            {
                throw new ArgumentNullException(nameof(brush));
            }

            var color = brush.Color;
            return includeAlpha
                ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Converts the brush's color to an "rgb(r, g, b)" string.
        /// </summary>
        /// <param name="brush">The <see cref="System.Windows.Media.SolidColorBrush"/> to convert.</param>
        /// <returns>A string in the format "rgb(R, G, B)".</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="brush"/> is null.</exception>
        public static string ToRgbString(this SolidColorBrush brush)
        {
            if (brush == null)
            {
                throw new ArgumentNullException(nameof(brush));
            }

            var color = brush.Color;
            return $"rgb({color.R}, {color.G}, {color.B})";
        }
    }
}
