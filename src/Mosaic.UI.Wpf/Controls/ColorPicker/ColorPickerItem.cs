/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable InconsistentNaming

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a color item in the color picker dropdown.
    /// </summary>
    public class ColorPickerItem
    {
        /// <summary>
        /// Gets or sets the display name of the color.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the hexadecimal string representation of the color.
        /// </summary>
        public string HexValue { get; set; }

        /// <summary>
        /// Gets or sets the brush representation of the color.
        /// </summary>
        public Brush Brush { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPickerItem"/> class with the specified name, hex color
        /// value, and brush.
        /// </summary>
        /// <param name="name">The display name of the color.</param>
        /// <param name="hexValue">The hexadecimal representation of the color (e.g., "#FFFFFF" for white).</param>
        /// <param name="brush">The <see cref="Brush"/> object representing the color.</param>
        public ColorPickerItem(string name, string hexValue, Brush brush)
        {
            Name = name;
            HexValue = hexValue;
            Brush = brush;
        }

        /// <summary>
        /// Returns a string representation of the current object.
        /// </summary>
        public override string ToString() => $"{Name}: {HexValue}";
    }
}
