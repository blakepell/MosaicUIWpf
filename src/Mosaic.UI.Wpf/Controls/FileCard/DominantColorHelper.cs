/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Collections.Concurrent;
using System.Windows.Media.Imaging;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Extracts a representative ("dominant") color from a bitmap and blends it into a theme color so a
    /// control can carry a subtle hint of an image's palette without leaving the active theme's range.
    /// </summary>
    /// <remarks>
    /// Results are cached by a caller supplied key (typically a file extension) because the expensive part
    /// is the pixel walk, and the same shell icon is reused across every card of that file type.
    /// </remarks>
    internal static class DominantColorHelper
    {
        /// <summary>
        /// Cache of previously computed dominant colors keyed by the caller's identity string.
        /// </summary>
        private static readonly ConcurrentDictionary<string, Color?> Cache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The longest edge the source is scaled down to before the pixels are inspected. Icons are already
        /// small, but this keeps the cost bounded should a large image ever be supplied.
        /// </summary>
        private const int SampleSize = 32;

        /// <summary>
        /// Pixels below this alpha are treated as transparent and excluded from the sample.
        /// </summary>
        private const byte MinimumAlpha = 128;

        /// <summary>
        /// Gets the dominant color of <paramref name="source"/>, caching the result under <paramref name="cacheKey"/>.
        /// </summary>
        /// <param name="cacheKey">A stable identity for the image (for example a file extension or asset name).</param>
        /// <param name="source">The image to inspect.</param>
        /// <returns>The dominant color, or <c>null</c> when one could not be determined.</returns>
        public static Color? GetDominantColor(string cacheKey, ImageSource? source)
        {
            if (string.IsNullOrEmpty(cacheKey))
            {
                return GetDominantColor(source);
            }

            return Cache.GetOrAdd(cacheKey, _ => GetDominantColor(source));
        }

        /// <summary>
        /// Gets the dominant color of <paramref name="source"/> without consulting the cache.
        /// </summary>
        /// <param name="source">The image to inspect.</param>
        /// <returns>The dominant color, or <c>null</c> when one could not be determined.</returns>
        public static Color? GetDominantColor(ImageSource? source)
        {
            if (source is not BitmapSource bitmap || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
            {
                return null;
            }

            try
            {
                var sample = Downscale(bitmap);

                if (sample.Format != PixelFormats.Bgra32)
                {
                    sample = new FormatConvertedBitmap(sample, PixelFormats.Bgra32, null, 0);
                }

                int width = sample.PixelWidth;
                int height = sample.PixelHeight;
                int stride = width * 4;
                var pixels = new byte[stride * height];
                sample.CopyPixels(pixels, stride, 0);

                // Two accumulators are kept: a saturation weighted one that finds the color a person would
                // call the color of the icon, and a plain average used when the image is entirely gray.
                double vividR = 0, vividG = 0, vividB = 0, vividWeight = 0;
                double flatR = 0, flatG = 0, flatB = 0;
                int flatCount = 0;

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte a = pixels[i + 3];

                    if (a < MinimumAlpha)
                    {
                        continue;
                    }

                    double b = pixels[i];
                    double g = pixels[i + 1];
                    double r = pixels[i + 2];

                    flatR += r;
                    flatG += g;
                    flatB += b;
                    flatCount++;

                    double max = Math.Max(r, Math.Max(g, b));
                    double min = Math.Min(r, Math.Min(g, b));
                    double value = max / 255d;
                    double saturation = max <= 0 ? 0 : (max - min) / max;

                    // Near-white and near-black pixels carry no usable hue, and neither do grays. Weighting by
                    // saturation and value naturally suppresses them without a hard cutoff.
                    double weight = saturation * saturation * value;

                    if (weight <= 0.01)
                    {
                        continue;
                    }

                    vividR += r * weight;
                    vividG += g * weight;
                    vividB += b * weight;
                    vividWeight += weight;
                }

                if (vividWeight > 0.5)
                {
                    return Color.FromRgb(
                        (byte)Math.Clamp(vividR / vividWeight, 0, 255),
                        (byte)Math.Clamp(vividG / vividWeight, 0, 255),
                        (byte)Math.Clamp(vividB / vividWeight, 0, 255));
                }

                if (flatCount > 0)
                {
                    return Color.FromRgb(
                        (byte)Math.Clamp(flatR / flatCount, 0, 255),
                        (byte)Math.Clamp(flatG / flatCount, 0, 255),
                        (byte)Math.Clamp(flatB / flatCount, 0, 255));
                }

                return null;
            }
            catch
            {
                // A malformed or unreadable bitmap simply means no tint; it is never worth failing a render over.
                return null;
            }
        }

        /// <summary>
        /// Scales the bitmap down so its longest edge is at most <see cref="SampleSize"/> pixels.
        /// </summary>
        /// <param name="bitmap">The bitmap to scale.</param>
        /// <returns>The scaled bitmap, or the original when it is already small enough.</returns>
        private static BitmapSource Downscale(BitmapSource bitmap)
        {
            int longest = Math.Max(bitmap.PixelWidth, bitmap.PixelHeight);

            if (longest <= SampleSize)
            {
                return bitmap;
            }

            double scale = (double)SampleSize / longest;
            return new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
        }

        /// <summary>
        /// Produces a theme-safe tint by re-lighting <paramref name="tint"/> to suit <paramref name="baseColor"/>
        /// and then mixing a small amount of it into the base color.
        /// </summary>
        /// <param name="baseColor">The theme surface color the result must stay close to.</param>
        /// <param name="tint">The dominant color supplying the hue.</param>
        /// <param name="strength">How much of the tint to mix in, from 0 (none) to 1 (all tint).</param>
        /// <returns>A color that reads as the theme surface with a hint of the tint's hue.</returns>
        public static Color Tint(Color baseColor, Color tint, double strength)
        {
            var (hue, saturation, _) = ToHsv(tint);

            // Gray sources have no hue worth carrying; fall back to the base color unchanged.
            if (saturation < 0.05)
            {
                return baseColor;
            }

            // Re-light the hue to the base surface's own brightness so a dark theme never receives a bright
            // wash and a light theme never receives a muddy one. Only the hue really survives the trip.
            double baseLuminance = Luminance(baseColor);
            double targetValue = baseLuminance < 0.5 ? 0.55 : 1.0;
            var relit = FromHsv(hue, Math.Clamp(saturation, 0.45, 0.95), targetValue);

            double amount = Math.Clamp(strength, 0, 1);

            return Color.FromRgb(
                (byte)Math.Round(baseColor.R + ((relit.R - baseColor.R) * amount)),
                (byte)Math.Round(baseColor.G + ((relit.G - baseColor.G) * amount)),
                (byte)Math.Round(baseColor.B + ((relit.B - baseColor.B) * amount)));
        }

        /// <summary>
        /// Returns the perceived luminance of a color on a 0-1 scale.
        /// </summary>
        /// <param name="color">The color to measure.</param>
        public static double Luminance(Color color)
        {
            return ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255d;
        }

        /// <summary>
        /// Converts an RGB color to hue (0-360), saturation (0-1) and value (0-1).
        /// </summary>
        /// <param name="color">The color to convert.</param>
        private static (double Hue, double Saturation, double Value) ToHsv(Color color)
        {
            double r = color.R / 255d;
            double g = color.G / 255d;
            double b = color.B / 255d;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double hue = 0;

            if (delta > 0)
            {
                if (Math.Abs(max - r) < double.Epsilon)
                {
                    hue = 60 * (((g - b) / delta) % 6);
                }
                else if (Math.Abs(max - g) < double.Epsilon)
                {
                    hue = 60 * (((b - r) / delta) + 2);
                }
                else
                {
                    hue = 60 * (((r - g) / delta) + 4);
                }
            }

            if (hue < 0)
            {
                hue += 360;
            }

            return (hue, max <= 0 ? 0 : delta / max, max);
        }

        /// <summary>
        /// Converts hue (0-360), saturation (0-1) and value (0-1) back to an RGB color.
        /// </summary>
        /// <param name="hue">The hue in degrees.</param>
        /// <param name="saturation">The saturation from 0 to 1.</param>
        /// <param name="value">The value (brightness) from 0 to 1.</param>
        private static Color FromHsv(double hue, double saturation, double value)
        {
            double c = value * saturation;
            double x = c * (1 - Math.Abs(((hue / 60) % 2) - 1));
            double m = value - c;

            double r, g, b;

            switch ((int)(hue / 60) % 6)
            {
                case 0: r = c; g = x; b = 0; break;
                case 1: r = x; g = c; b = 0; break;
                case 2: r = 0; g = c; b = x; break;
                case 3: r = 0; g = x; b = c; break;
                case 4: r = x; g = 0; b = c; break;
                default: r = c; g = 0; b = x; break;
            }

            return Color.FromRgb(
                (byte)Math.Clamp(Math.Round((r + m) * 255), 0, 255),
                (byte)Math.Clamp(Math.Round((g + m) * 255), 0, 255),
                (byte)Math.Clamp(Math.Round((b + m) * 255), 0, 255));
        }
    }
}
