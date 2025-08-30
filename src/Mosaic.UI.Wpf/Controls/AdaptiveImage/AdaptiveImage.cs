/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Windows.Media.Imaging;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Image that adapts its colors to match app/OS theme.  Intended purpose is to be used for
    /// icons to be able to adapt.
    /// </summary>
    /// <remarks>
    /// This control is dependent on being notified of theme changes.  It does not track the system
    /// because an app maybe in a mode that the system isn't.
    /// </remarks>
    public class AdaptiveImage : Image
    {
        // Caching transformed images`
        private ImageSource? _cachedLight;
        private ImageSource? _cachedDark;
        private ImageSource? _cachedOriginal;
        private ImageSource? _lastApplied;
        private bool _isUpdatingSource; // Flag to prevent recursion

        /// <summary>
        /// Identifies the <see cref="ThemeMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThemeModeProperty = DependencyProperty.Register(
                nameof(ThemeMode), typeof(ThemeMode), typeof(AdaptiveImage), new FrameworkPropertyMetadata(ThemeMode.Light, FrameworkPropertyMetadataOptions.AffectsRender, OnThemeRelatedChanged));

        /// <summary>
        /// Gets or sets the theme mode for the application.
        /// </summary>
        public ThemeMode ThemeMode
        {
            get => (ThemeMode)GetValue(ThemeModeProperty);
            set => SetValue(ThemeModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EnableLightnessMirror"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EnableLightnessMirrorProperty = DependencyProperty.Register(
            nameof(EnableLightnessMirror), typeof(bool), typeof(AdaptiveImage), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnThemeRelatedChanged));

        /// <summary>
        /// If true (default), mirror lightness for the active theme (e.g., dark colors become light on dark theme).
        /// </summary>
        public bool EnableLightnessMirror
        {
            get => (bool)GetValue(EnableLightnessMirrorProperty);
            set => SetValue(EnableLightnessMirrorProperty, value);
        }

        /// <summary>
        /// Identifies the SaturationFloor dependency property, which determines the minimum saturation level for the
        /// image.
        /// </summary>
        public static readonly DependencyProperty SaturationFloorProperty = DependencyProperty.Register(
            nameof(SaturationFloor), typeof(double), typeof(AdaptiveImage), 
            new FrameworkPropertyMetadata(0.15, FrameworkPropertyMetadataOptions.AffectsRender, OnThemeRelatedChanged), v => v is double and >= 0 and <= 1);

        /// <summary>
        /// Minimum saturation to keep icons from turning gray when mirrored. [0..1]
        /// </summary>
        public double SaturationFloor
        {
            get => (double)GetValue(SaturationFloorProperty);
            set => SetValue(SaturationFloorProperty, value);
        }

        public static readonly DependencyProperty LightnessBiasProperty = DependencyProperty.Register(
                nameof(LightnessBias), typeof(double), typeof(AdaptiveImage),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnThemeRelatedChanged), v => v is double and >= -0.5 and <= 0.5);

        /// <summary>
        /// Adds/subtracts lightness after mirroring to fine-tune contrast. [-0.5..0.5]
        /// </summary>
        public double LightnessBias
        {
            get => (double)GetValue(LightnessBiasProperty);
            set => SetValue(LightnessBiasProperty, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="AdaptiveImage"/> class.
        /// </summary>
        static AdaptiveImage()
        {
            // Re-render when Source changes.
            SourceProperty.OverrideMetadata(typeof(AdaptiveImage),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSourceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdaptiveImage"/> class.
        /// </summary>
        public AdaptiveImage()
        {
            // If the app has its own theme change event, hook it up here.  The caller
            // may or may not use our ThemeChanged event, but this is what we provide.
            MosaicApp.ThemeChanged += MosaicApp_ThemeChanged;
                                
            this.ThemeMode = MosaicApp.Theme.Equals("Light") ? ThemeMode.Light : ThemeMode.Dark;
        }

        /// <summary>
        /// Handles the theme change event for the Mosaic application.
        /// </summary>
        /// <param name="sender">The source of the event. This parameter can be <see langword="null"/>.</param>
        /// <param name="e">A string representing the new theme mode. Expected values are "Light" or "Dark".</param>
        private void MosaicApp_ThemeChanged(object? sender, string e)
        {
            this.ThemeMode = e.Equals("Light") ? ThemeMode.Light : ThemeMode.Dark;
            InvalidateAdaptive();
        }

        /// <summary>
        /// Handles changes to the <see cref="Source"/> property of an <see cref="AdaptiveImage"/> instance.
        /// </summary>
        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AdaptiveImage { _isUpdatingSource: false } ai)
            {
                ai.InvalidateAdaptive(resetCacheIfDifferentSource: true);
            }
        }

        /// <summary>
        /// Handles changes to theme-related properties and updates the associated <see cref="AdaptiveImage"/> instance.
        /// </summary>
        private static void OnThemeRelatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AdaptiveImage ai)
            {
                ai.InvalidateAdaptive();
            }
        }

        /// <summary>
        /// Updates the adaptive source and optionally resets cached values if the source has changed.
        /// </summary>
        /// <param name="resetCacheIfDifferentSource">A value indicating whether to reset cached values if the current source is different from the previously
        /// cached source. If <see langword="true"/>, the cache is cleared and updated if the source has changed;
        /// otherwise, the cache remains unchanged.</param>
        private void InvalidateAdaptive(bool resetCacheIfDifferentSource = false)
        {
            if (resetCacheIfDifferentSource)
            {
                if (!ReferenceEquals(_cachedOriginal, Source))
                {
                    _cachedOriginal = Source;
                    _cachedLight = null;
                    _cachedDark = null;
                    _lastApplied = null;
                }
            }

            UpdateAdaptiveSource();
        }

        /// <summary>
        /// Invoked when the control is rendered. This method updates the adaptive source and performs the base
        /// rendering logic.
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            UpdateAdaptiveSource();
            base.OnRender(dc);
        }

        /// <summary>
        /// Updates the image source based on the current theme and lightness mirroring settings.
        /// </summary>
        private void UpdateAdaptiveSource()
        {
            if (Source == null)
            {
                return;
            }

            var effectiveTheme = GetEffectiveTheme();
            ImageSource? target = null;

            if (!EnableLightnessMirror)
            {
                // Just use the source untouched.
                if (!ReferenceEquals(_lastApplied, Source))
                {
                    _isUpdatingSource = true;
                    base.Source = Source;
                    _isUpdatingSource = false;
                    _lastApplied = Source;
                }
                return;
            }

            if (effectiveTheme == ThemeMode.Light)
            {
                if (_cachedLight == null)
                {
                    _cachedLight = TryTransform(Source, forDarkTheme: false) ?? Source;
                }

                target = _cachedLight;
            }
            else // Dark
            {
                if (_cachedDark == null)
                {
                    _cachedDark = TryTransform(Source, forDarkTheme: true) ?? Source;
                }

                target = _cachedDark;
            }

            if (target != null && !ReferenceEquals(_lastApplied, target))
            {
                _isUpdatingSource = true;
                base.Source = target;
                _isUpdatingSource = false;
                _lastApplied = target;
            }
        }

        /// <summary>
        /// Determines the effective theme mode based on the application's current theme setting.
        /// </summary>
        /// <returns>A <see cref="ThemeMode"/> value indicating the effective theme.  Returns <see cref="ThemeMode.Dark"/> if the
        /// application's theme is set to "Dark"; otherwise, returns <see cref="ThemeMode.Light"/>.</returns>
        private ThemeMode GetEffectiveTheme()
        {
            return MosaicApp.Theme == "Dark" ? ThemeMode.Dark : ThemeMode.Light;
        }

        /// <summary>
        /// Attempts to transform the provided <see cref="ImageSource"/> based on the specified theme.
        /// </summary>
        /// <param name="src">The source image to transform. Must be an instance of <see cref="BitmapSource"/> to apply transformations;
        /// otherwise, the original image is returned.</param>
        /// <param name="forDarkTheme">A value indicating whether the transformation should be tailored for a dark theme.  If <see
        /// langword="true"/>, the transformation is applied for dark theme compatibility.</param>
        /// <returns>The transformed <see cref="ImageSource"/> if the source is a <see cref="BitmapSource"/>;  otherwise, the
        /// original <see cref="ImageSource"/>.</returns>
        private ImageSource? TryTransform(ImageSource src, bool forDarkTheme)
        {
            if (src is BitmapSource bmp)
            {
                return TransformBitmap(bmp, forDarkTheme);
            }

            // If it's a DrawingImage / vector, we leave it alone (or you could add a brush-tint path here).
            return src;
        }

        /// <summary>
        /// Transforms the input <see cref="BitmapSource"/> by adjusting its lightness and saturation  to create a
        /// theme-appropriate appearance.
        /// </summary>
        private ImageSource TransformBitmap(BitmapSource input, bool forDarkTheme)
        {
            // Ensure a known pixel format
            var fmt = PixelFormats.Bgra32;
            var converted = input.Format == fmt ? input : new FormatConvertedBitmap(input, fmt, null, 0);

            int width = converted.PixelWidth;
            int height = converted.PixelHeight;
            int stride = (width * fmt.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[height * stride];
            converted.CopyPixels(pixels, stride, 0);

            // Mirror lightness in HSL while preserving hue; bump saturation to a minimum.
            for (int y = 0; y < height; y++)
            {
                int row = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int idx = row + x * 4;
                    byte b = pixels[idx + 0];
                    byte g = pixels[idx + 1];
                    byte r = pixels[idx + 2];
                    byte a = pixels[idx + 3];

                    if (a == 0)
                    {
                        continue;
                    }

                    // Convert to normalized
                    double rn = r / 255.0, gn = g / 255.0, bn = b / 255.0;

                    RgbToHsl(rn, gn, bn, out double h, out double s, out double l);

                    // Special handling for grayscale colors (black, white, gray)
                    // These should mirror lightness without artificial saturation
                    bool isGrayscale = s < 0.05; // Very low saturation indicates grayscale

                    if (!isGrayscale)
                    {
                        // Raise desaturated icons to a floor so they don't go muddy
                        if (s < SaturationFloor)
                        {
                            s = SaturationFloor;
                        }
                    }

                    // Mirror lightness based on theme and original lightness:
                    double lPrime;
                    if (forDarkTheme)
                    {
                        // Dark theme: make dark colors light, keep light colors light
                        lPrime = l < 0.5 ? 1.0 - l : l;
                    }
                    else
                    {
                        // Light theme: make light colors dark, keep dark colors dark  
                        lPrime = l > 0.5 ? 1.0 - l : l;
                    }

                    lPrime = Clamp01(lPrime + LightnessBias);

                    // Optional slight contrast stretch to keep edges crisp
                    // (You can comment this out if you don't want it.)
                    lPrime = lPrime < 0.5 ? lPrime * 0.98 : 1 - (1 - lPrime) * 0.98;

                    HslToRgb(h, s, lPrime, out rn, out gn, out bn);

                    pixels[idx + 0] = (byte)Math.Round(Clamp01(bn) * 255);
                    pixels[idx + 1] = (byte)Math.Round(Clamp01(gn) * 255);
                    pixels[idx + 2] = (byte)Math.Round(Clamp01(rn) * 255);
                    pixels[idx + 3] = a; // preserve alpha
                }
            }

            var wb = new WriteableBitmap(width, height, converted.DpiX, converted.DpiY, fmt, null);
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            wb.Freeze();
            return wb;
        }

        /// <summary>
        /// Clamps a double-precision floating-point value to the range [0, 1].
        /// </summary>
        /// <param name="v">The value to clamp.</param>
        /// <returns>The clamped value, which will be 0 if <paramref name="v"/> is less than 0,  1 if <paramref name="v"/> is
        /// greater than 1, or <paramref name="v"/> itself if it is within the range [0, 1].</returns>
        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        /// <summary>
        /// Converts RGB color values to their equivalent HSL (Hue, Saturation, Lightness) representation.
        /// </summary>
        /// <remarks>The RGB values must be normalized to the range [0, 1] before calling this method. 
        /// The resulting HSL values are also normalized to the range [0, 1].</remarks>
        /// <param name="r">The red component of the color, in the range [0, 1].</param>
        /// <param name="g">The green component of the color, in the range [0, 1].</param>
        /// <param name="b">The blue component of the color, in the range [0, 1].</param>
        /// <param name="h">When this method returns, contains the hue of the color, in the range [0, 1].</param>
        /// <param name="s">When this method returns, contains the saturation of the color, in the range [0, 1].</param>
        /// <param name="l">When this method returns, contains the lightness of the color, in the range [0, 1].</param>
        private static void RgbToHsl(double r, double g, double b, out double h, out double s, out double l)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2.0;

            if (Math.Abs(max - min) < 1e-6)
            {
                h = 0; s = 0;
                return;
            }

            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (max == r)
            {
                h = (g - b) / d + (g < b ? 6 : 0);
            }
            else if (max == g)
            {
                h = (b - r) / d + 2;
            }
            else
            {
                h = (r - g) / d + 4;
            }

            h /= 6.0;
        }

        /// <summary>
        /// Converts a color from HSL (Hue, Saturation, Lightness) color space to RGB (Red, Green, Blue) color space.
        /// </summary>
        /// <remarks>This method calculates the RGB representation of a color based on its HSL values. The
        /// resulting RGB values are normalized to the range [0.0, 1.0]. The hue value wraps around cyclically, so
        /// values outside the range [0.0, 1.0] are interpreted modulo 1.0.</remarks>
        /// <param name="h">The hue of the color, specified as a value between 0.0 and 1.0, where 0.0 represents 0 degrees and 1.0
        /// represents 360 degrees.</param>
        /// <param name="s">The saturation of the color, specified as a value between 0.0 (completely unsaturated) and 1.0 (fully
        /// saturated).</param>
        /// <param name="l">The lightness of the color, specified as a value between 0.0 (completely dark) and 1.0 (completely light).</param>
        /// <param name="r">When the method returns, contains the red component of the color, as a value between 0.0 and 1.0.</param>
        /// <param name="g">When the method returns, contains the green component of the color, as a value between 0.0 and 1.0.</param>
        /// <param name="b">When the method returns, contains the blue component of the color, as a value between 0.0 and 1.0.</param>
        private static void HslToRgb(double h, double s, double l, out double r, out double g, out double b)
        {
            if (s == 0)
            {
                r = g = b = l;
                return;
            }

            double q = l < 0.5 ? l * (1 + s) : (l + s - l * s);
            double p = 2 * l - q;
            r = Hue2Rgb(p, q, h + 1.0 / 3.0);
            g = Hue2Rgb(p, q, h);
            b = Hue2Rgb(p, q, h - 1.0 / 3.0);
        }

        /// <summary>
        /// Converts a hue value to its corresponding RGB component value.
        /// </summary>
        /// <remarks>This method is typically used as part of the HSL-to-RGB color conversion process. The
        /// input parameter <paramref name="t"/> represents the adjusted hue for a specific RGB channel, and the method
        /// calculates the corresponding intensity for that channel.</remarks>
        /// <param name="p">The first intermediate value used in the RGB conversion process. Typically derived from the lightness and
        /// saturation of the color.</param>
        /// <param name="q">The second intermediate value used in the RGB conversion process. Typically derived from the lightness and
        /// saturation of the color.</param>
        /// <param name="t">The hue value to be converted, represented as a fractional value in the range [0, 1].</param>
        /// <returns>The calculated RGB component value as a double in the range [0, 1].</returns>
        private static double Hue2Rgb(double p, double q, double t)
        {
            if (t < 0)
            {
                t += 1;
            }

            if (t > 1)
            {
                t -= 1;
            }

            if (t < 1.0 / 6.0)
            {
                return p + (q - p) * 6 * t;
            }

            if (t < 1.0 / 2.0)
            {
                return q;
            }

            if (t < 2.0 / 3.0)
            {
                return p + (q - p) * (2.0 / 3.0 - t) * 6;
            }

            return p;
        }
    }
}
