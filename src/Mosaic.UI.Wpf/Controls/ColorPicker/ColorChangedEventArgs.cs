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
    /// Event arguments for color changed events.
    /// </summary>
    public class ColorChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the color associated with the object.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Gets the brush associated with the object.
        /// </summary>
        public Brush Brush { get; }

        /// <summary>
        /// Gets the hex value associated with the object.
        /// </summary>
        public string HexValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorChangedEventArgs"/> class with the specified color, brush,
        /// and hexadecimal color value.
        /// </summary>
        /// <param name="color">The new <see cref="Color"/> associated with the event.</param>
        /// <param name="brush">The <see cref="Brush"/> representation of the color.</param>
        /// <param name="hexValue">The hexadecimal string representation of the color.</param>
        public ColorChangedEventArgs(Color color, Brush brush, string hexValue)
        {
            Color = color;
            Brush = brush;
            HexValue = hexValue;
        }
    }
}
