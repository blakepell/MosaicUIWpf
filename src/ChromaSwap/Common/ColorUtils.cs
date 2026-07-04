/*
 * ChromaSwap
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media;

namespace ChromaSwap.Common
{
    /// <summary>
    /// RGB/HSV conversion helpers used by the color picking and swap features.
    /// HSV uses H in [0, 360], S and V in [0, 100].
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Formats a color as an upper case #RRGGBB hex string.
        /// </summary>
        /// <param name="color">The color to format.</param>
        public static string ToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// Parses a #RRGGBB (or RRGGBB) hex string.  Returns null when the value isn't valid.
        /// </summary>
        /// <param name="hex">The hex string to parse.</param>
        public static Color? FromHex(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                return null;
            }

            hex = hex.Trim().TrimStart('#');

            if (hex.Length != 6 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out uint value))
            {
                return null;
            }

            return Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
        }

        /// <summary>
        /// Converts RGB bytes to HSV (H 0-360, S 0-100, V 0-100).
        /// </summary>
        /// <param name="r">Red component.</param>
        /// <param name="g">Green component.</param>
        /// <param name="b">Blue component.</param>
        public static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;

            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double d = max - min;

            double h;

            if (d == 0)
            {
                h = 0;
            }
            else if (max == rd)
            {
                h = ((gd - bd) / d + (gd < bd ? 6 : 0)) / 6.0;
            }
            else if (max == gd)
            {
                h = ((bd - rd) / d + 2) / 6.0;
            }
            else
            {
                h = ((rd - gd) / d + 4) / 6.0;
            }

            double s = max == 0 ? 0 : d / max;

            return (h * 360.0, s * 100.0, max * 100.0);
        }

        /// <summary>
        /// Converts HSV (H 0-360, S 0-100, V 0-100) to RGB bytes.
        /// </summary>
        /// <param name="h">Hue in degrees.</param>
        /// <param name="s">Saturation percent.</param>
        /// <param name="v">Value percent.</param>
        public static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
        {
            h = ((h % 360.0) + 360.0) % 360.0;
            s = Math.Clamp(s, 0, 100) / 100.0;
            v = Math.Clamp(v, 0, 100) / 100.0;

            double c = v * s;
            double x = c * (1 - Math.Abs(((h / 60.0) % 2) - 1));
            double m = v - c;

            (double r, double g, double b) = h switch
            {
                < 60 => (c, x, 0.0),
                < 120 => (x, c, 0.0),
                < 180 => (0.0, c, x),
                < 240 => (0.0, x, c),
                < 300 => (x, 0.0, c),
                _ => (c, 0.0, x)
            };

            return (
                (byte)Math.Clamp(Math.Round((r + m) * 255.0), 0, 255),
                (byte)Math.Clamp(Math.Round((g + m) * 255.0), 0, 255),
                (byte)Math.Clamp(Math.Round((b + m) * 255.0), 0, 255));
        }

        /// <summary>
        /// Mixes a channel toward a target (0 for black, 255 for white) by a weight in [0, 1].
        /// Used to build the shades/tints scale.
        /// </summary>
        /// <param name="channel">The starting channel value.</param>
        /// <param name="target">The channel value being mixed toward.</param>
        /// <param name="weight">How far to move toward the target.</param>
        public static byte Mix(byte channel, byte target, double weight)
        {
            return (byte)Math.Clamp(Math.Round(channel + (target - channel) * weight), 0, 255);
        }
    }
}
