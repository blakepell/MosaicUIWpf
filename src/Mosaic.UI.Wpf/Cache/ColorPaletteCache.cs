/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.Concurrent;

namespace Mosaic.UI.Wpf.Cache
{
    /// <summary>
    /// Common color palette with static copies of the brushes used in the app.
    /// </summary>
    public static class ColorPaletteCache
    {
        /// <summary>
        /// Internal cache of frozen <see cref="SolidColorBrush"/> objects.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SolidColorBrush> BrushCache = new();

        /// <summary>
        /// Internal cache of Color objects.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Color> ColorCache = new();

        /// <summary>
        /// Gets a <see cref="SolidColorBrush"/> for the specified hex, freezes and
        /// caches the value for subsequent calls.
        /// </summary>
        /// <param name="hex"></param>
        public static SolidColorBrush GetBrush(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return Brushes.Black;
            }

            if (hex[0] != '#')
            {
                hex = $"#{hex}";
            }

            if (BrushCache.TryGetValue(hex, out var brush))
            {
                return brush;
            }

            var newBrush = (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
            newBrush.Freeze();
            BrushCache[hex] = newBrush;

            return newBrush;
        }

        /// <summary>
        /// Gets a <see cref="SolidColorBrush"/> for the specified <see cref="Color"/> and caches the value
        /// </summary>
        /// <param name="color"></param>
        public static SolidColorBrush GetBrush(Color color)
        {
            string buf = color.ToString();

            if (BrushCache.TryGetValue(buf, out var brush))
            {
                return brush;
            }

            var newBrush = new SolidColorBrush(color);
            newBrush.Freeze();
            BrushCache[buf] = newBrush;

            return newBrush;
        }

        /// <summary>
        /// Gets a <see cref="Color"/> for the specified hex value and caches the value
        /// for subsequent calls.
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static Color GetColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return Colors.Black;
            }

            if (hex[0] != '#')
            {
                hex = $"#{hex}";
            }

            if (ColorCache.TryGetValue(hex, out var color))
            {
                return color;
            }

            // Remove the hash at the front if it's there
            hex = hex.Replace("#", "");

            byte a = 255; // Default Alpha value
            byte r, g, b;

            if (hex.Length == 6)
            {
                r = Convert.ToByte(hex.Substring(0, 2), 16);
                g = Convert.ToByte(hex.Substring(2, 2), 16);
                b = Convert.ToByte(hex.Substring(4, 2), 16);
            }
            else if (hex.Length == 8)
            {
                a = Convert.ToByte(hex.Substring(0, 2), 16);
                r = Convert.ToByte(hex.Substring(2, 2), 16);
                g = Convert.ToByte(hex.Substring(4, 2), 16);
                b = Convert.ToByte(hex.Substring(6, 2), 16);
            }
            else
            {
                throw new ArgumentException("Hex value is not valid.");
            }

            var newBrush = Color.FromArgb(a, r, g, b);
            ColorCache[hex] = newBrush;

            return newBrush;
        }
    }
}