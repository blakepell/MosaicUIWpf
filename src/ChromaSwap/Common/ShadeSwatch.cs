/*
 * ChromaSwap
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Media;

namespace ChromaSwap.Common
{
    /// <summary>
    /// A single entry in the shades &amp; tints panel.
    /// </summary>
    public class ShadeSwatch
    {
        /// <summary>
        /// The scale label, e.g. "shade-550 (Base)".
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// The #RRGGBB hex value copied to the clipboard.
        /// </summary>
        public required string Hex { get; init; }

        /// <summary>
        /// The CSS variable name used when exporting, e.g. "shade-550".
        /// </summary>
        public required string CssName { get; init; }

        /// <summary>
        /// The swatch fill brush.
        /// </summary>
        public required Brush Brush { get; init; }

        /// <summary>
        /// Whether this is the base (selected) color entry.
        /// </summary>
        public bool IsBase { get; init; }

        /// <summary>
        /// The hex label weight; bold for the base entry.
        /// </summary>
        public FontWeight FontWeight => this.IsBase ? FontWeights.Bold : FontWeights.Normal;

        /// <summary>
        /// The hex label brush; accent colored for the base entry.
        /// </summary>
        public required Brush HexForeground { get; init; }
    }
}
