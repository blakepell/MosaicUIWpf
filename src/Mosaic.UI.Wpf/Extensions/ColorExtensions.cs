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
    /// Provides extension methods for converting <see cref="Color"/> objects to string representations.
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="Color"/> to a hexadecimal color string.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> to convert.</param>
        /// <param name="includeAlpha">
        /// If <c>true</c>, the returned string includes the alpha channel as the first two hex digits
        /// (format: <c>#AARRGGBB</c>). If <c>false</c>, the returned string contains only the RGB channels
        /// (format: <c>#RRGGBB</c>).
        /// </param>
        /// <returns>
        /// A string containing the hexadecimal representation of the color prefixed with '#'. 
        /// Examples: <c>#FF3366</c> (RGB only) or <c>#80FF3366</c> (including alpha).
        /// Hex values are uppercase and each channel is zero-padded to two characters.
        /// </returns>
        /// <remarks>
        /// This extension targets WPF <see cref="Color"/> (System.Windows.Media.Color).
        /// Uses uppercase hexadecimal digits and ensures two characters per channel.
        /// </remarks>
        public static string ToHexString(this Color color, bool includeAlpha = false)
        {
            return includeAlpha
                ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Converts the specified <see cref="Color"/> to a CSS-style RGB string.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> to convert. This is the WPF <c>System.Windows.Media.Color</c>.</param>
        /// <returns>
        /// A string in the form <c>rgb(R, G, B)</c> where <c>R</c>, <c>G</c>, and <c>B</c> are the
        /// red, green, and blue channel values from <paramref name="color"/> (0–255).
        /// The alpha channel is ignored; use an RGBA or hex representation if alpha is required.
        /// </returns>
        /// <remarks>
        /// Produces a CSS-compatible RGB representation suitable for use in web/CSS contexts
        /// or any textual representation that expects integers for each color channel.
        /// Each channel is formatted as a decimal integer with no leading zeros or padding.
        /// </remarks>
        public static string ToRgbString(this Color color)
        {
            return $"rgb({color.R}, {color.G}, {color.B})";
        }

        /// <summary>
        /// Converts the specified <see cref="System.Drawing.Color"/> to a hexadecimal color string.
        /// </summary>
        /// <param name="color">The <see cref="System.Drawing.Color"/> to convert.</param>
        /// <param name="includeAlpha">
        /// If <c>true</c>, the returned string includes the alpha channel as the first two hex digits
        /// (format: <c>#AARRGGBB</c>). If <c>false</c>, the returned string contains only the RGB channels
        /// (format: <c>#RRGGBB</c>).
        /// </param>
        /// <returns>
        /// A string containing the hexadecimal representation of the color prefixed with '#'. 
        /// Examples: <c>#FF3366</c> (RGB only) or <c>#80FF3366</c> (including alpha).
        /// Hex values are uppercase and each channel is zero-padded to two characters.
        /// </returns>
        public static string ToHexString(this System.Drawing.Color color, bool includeAlpha = false)
        {
            return includeAlpha
                ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Converts the specified <see cref="System.Drawing.Color"/> to a CSS-style RGB string.
        /// </summary>
        /// <param name="color">The <see cref="System.Drawing.Color"/> to convert.</param>
        /// <returns>
        /// A string in the form <c>rgb(R, G, B)</c> where <c>R</c>, <c>G</c>, and <c>B</c> are the
        /// red, green, and blue channel values from <paramref name="color"/> (0–255).
        /// The alpha channel is ignored; use an RGBA or hex representation if alpha is required.
        /// </returns>
        public static string ToRgbString(this System.Drawing.Color color)
        {
            return $"rgb({color.R}, {color.G}, {color.B})";
        }

    }
}
