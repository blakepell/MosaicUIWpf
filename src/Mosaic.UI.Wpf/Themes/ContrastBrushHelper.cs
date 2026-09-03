/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Themes
{
    /// <summary>
    /// Selects a light or dark foreground brush from the relative luminance of a background color.
    /// </summary>
    public static class ContrastBrushHelper
    {
        private static readonly IReadOnlyDictionary<string, int> PaletteLightForegroundStartShades =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Blue"] = 500,
                ["Purple"] = 500,
                ["Orange"] = 500,
                ["Yellow"] = 500,
                ["Green"] = 500,
                ["Teal"] = 600,
                ["Cyan"] = 600
            };

        /// <summary>
        /// The default relative-luminance cutoff used by Mosaic controls.
        /// </summary>
        /// <remarks>
        /// Raising this value makes the light foreground apply to lighter backgrounds. The value must be
        /// between 0 and 1. A value near 0.179 gives the foreground with the higher WCAG contrast ratio;
        /// Mosaic uses 0.21 to allow a modest visual preference toward white text.
        /// </remarks>
        public const double DefaultLuminanceThreshold = 0.21;

        /// <summary>
        /// Selects black or white for the specified background color.
        /// </summary>
        /// <param name="background">The color behind the text.</param>
        /// <param name="luminanceThreshold">
        /// The luminance at or below which white is selected. Higher values favor white.
        /// </param>
        /// <returns>A shared black or white brush.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="luminanceThreshold"/> is outside the range 0 through 1.
        /// </exception>
        public static Brush GetForegroundBrush(
            Color background,
            double luminanceThreshold = DefaultLuminanceThreshold)
        {
            ValidateLuminanceThreshold(luminanceThreshold);
            return GetRelativeLuminance(background) > luminanceThreshold ? Brushes.Black : Brushes.White;
        }

        /// <summary>
        /// Selects black or white using Mosaic's family-specific palette switch point when one is defined,
        /// or relative luminance for other palette families.
        /// </summary>
        /// <param name="paletteFamily">The palette family name, such as Blue or Yellow.</param>
        /// <param name="shade">The numeric palette shade.</param>
        /// <param name="background">The palette color behind the text.</param>
        /// <param name="fallbackLuminanceThreshold">
        /// The luminance cutoff used when <paramref name="paletteFamily"/> has no configured switch point.
        /// </param>
        /// <returns>A shared black or white brush.</returns>
        public static Brush GetPaletteForegroundBrush(
            string paletteFamily,
            int shade,
            Color background,
            double fallbackLuminanceThreshold = DefaultLuminanceThreshold)
        {
            return ShouldUseLightForeground(paletteFamily, shade, background, fallbackLuminanceThreshold)
                ? Brushes.White
                : Brushes.Black;
        }

        /// <summary>
        /// Determines whether a palette color should use its light foreground.
        /// </summary>
        /// <param name="paletteFamily">The palette family name.</param>
        /// <param name="shade">The numeric palette shade.</param>
        /// <param name="background">The palette color behind the text.</param>
        /// <param name="fallbackLuminanceThreshold">The cutoff used for an unconfigured family.</param>
        /// <returns><see langword="true"/> when the light foreground should be used.</returns>
        public static bool ShouldUseLightForeground(
            string? paletteFamily,
            int shade,
            Color background,
            double fallbackLuminanceThreshold = DefaultLuminanceThreshold)
        {
            ValidateLuminanceThreshold(fallbackLuminanceThreshold);

            if (!string.IsNullOrWhiteSpace(paletteFamily) &&
                shade > 0 &&
                PaletteLightForegroundStartShades.TryGetValue(paletteFamily, out int startShade))
            {
                return shade >= startShade;
            }

            return GetRelativeLuminance(background) <= fallbackLuminanceThreshold;
        }

        /// <summary>
        /// Calculates the WCAG relative luminance of an sRGB color.
        /// </summary>
        /// <param name="color">The color to evaluate.</param>
        /// <returns>A value between 0 for black and 1 for white.</returns>
        public static double GetRelativeLuminance(Color color)
        {
            return (0.2126 * ToLinearColorChannel(color.R)) +
                   (0.7152 * ToLinearColorChannel(color.G)) +
                   (0.0722 * ToLinearColorChannel(color.B));
        }

        /// <summary>
        /// Ensures a luminance threshold is in the supported range.
        /// </summary>
        /// <param name="luminanceThreshold">The threshold to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">The value is outside the range 0 through 1.</exception>
        internal static void ValidateLuminanceThreshold(double luminanceThreshold)
        {
            if (double.IsNaN(luminanceThreshold) || luminanceThreshold < 0 || luminanceThreshold > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(luminanceThreshold),
                    luminanceThreshold,
                    "The luminance threshold must be between 0 and 1.");
            }
        }

        /// <summary>
        /// Converts an eight-bit sRGB channel to its linear-light value.
        /// </summary>
        private static double ToLinearColorChannel(byte channel)
        {
            double normalized = channel / 255d;
            return normalized <= 0.04045
                ? normalized / 12.92
                : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }
    }
}
