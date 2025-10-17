/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Provides attached properties and utility methods for modifying the appearance of brushes used in WPF controls,
    /// such as lightening the background color of elements.
    /// </summary>
    /// <remarks>
    /// local:BrushModifier.AdjustBackgroundPercentage="-0.2"
    /// </remarks>
    public static class BrushModifier
    {
        /// <summary>
        /// Identifies the <see cref="IsModified"/> attached property.
        /// </summary>
        /// <remarks>This attached property is used to indicate whether a specific object has been
        /// modified.  The default value is <see langword="false"/>.</remarks>
        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.RegisterAttached("IsModified", typeof(bool), typeof(BrushModifier), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the AdjustBackgroundPercentage attached property, which specifies the percentage by which to
        /// lighten a background.
        /// </summary>
        /// <remarks>This attached property is used to adjust the brightness of a background by a
        /// specified percentage.  The value must be a double between 0.0 and 1.0, where 0.0 represents no change and
        /// 1.0 represents fully lightened.</remarks>
        public static readonly DependencyProperty AdjustBackgroundPercentageProperty =
            DependencyProperty.RegisterAttached("AdjustBackgroundPercentage", typeof(double), typeof(BrushModifier), new FrameworkPropertyMetadata(0.0, OnAdjustBackgroundPercentageChanged));

        /// <summary>
        /// Sets the value of the <see cref="IsModifiedProperty"/> attached property for the specified <see
        /// cref="DependencyObject"/>.
        /// </summary>
        /// <param name="element">The <see cref="DependencyObject"/> for which to set the property value. Cannot be <see langword="null"/>.</param>
        /// <param name="value">The new value to set for the <see cref="IsModifiedProperty"/>.</param>
        public static void SetIsModified(DependencyObject element, bool value) 
            => element.SetValue(IsModifiedProperty, value);

        /// <summary>
        /// Determines whether the specified <see cref="DependencyObject"/> has been marked as modified.
        /// </summary>
        /// <param name="element">The <see cref="DependencyObject"/> to check for the modified state.</param>
        /// <returns><see langword="true"/> if the <paramref name="element"/> is marked as modified; otherwise, <see
        /// langword="false"/>.</returns>
        public static bool GetIsModified(DependencyObject element)
            => (bool)element.GetValue(IsModifiedProperty);


        /// <summary>
        /// Sets the percentage used to lighten the background on the specified element.
        /// </summary>
        /// <param name="element">The element to set the percentage on.</param>
        /// <param name="value">The lighten percentage (-1.0 to 1.0).</param>
        public static void SetAdjustBackgroundPercentage(DependencyObject element, double value)
            => element.SetValue(AdjustBackgroundPercentageProperty, value);

        /// <summary>
        /// Gets the percentage used to lighten the background on the specified element.
        /// </summary>
        /// <param name="element">The element to read the percentage from.</param>
        /// <returns>The lighten percentage (-1.0 to 1.0).</returns>
        public static double GetAdjustBackgroundPercentage(DependencyObject element)
            => (double)element.GetValue(AdjustBackgroundPercentageProperty);


        /// <summary>
        /// Handles changes to <see cref="AdjustBackgroundPercentageProperty"/> and attaches an updater for the element's background.
        /// </summary>
        /// <param name="d">The element whose property changed.</param>
        /// <param name="e">Event arguments describing the change.</param>
        private static void OnAdjustBackgroundPercentageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyChangeHandler? handler = null;
            if (d is Control control)
            {
                handler = new PropertyChangeHandler(control, Control.BackgroundProperty);
            }
            else if (d is Border border)
            {
                handler = new PropertyChangeHandler(border, Border.BackgroundProperty);
            }
            else if (d is Panel panel)
            {
                handler = new PropertyChangeHandler(panel, Panel.BackgroundProperty);
            }

            handler?.UpdateBrush();
        }

        private class PropertyChangeHandler
        {
            private readonly DependencyObject _element;
            private readonly DependencyProperty _property;

            /// <summary>
            /// Initializes a new instance for the specified element and dependency property.
            /// </summary>
            /// <param name="element">The element to monitor.</param>
            /// <param name="property">The dependency property to monitor.</param>
            internal PropertyChangeHandler(DependencyObject element, DependencyProperty property)
            {
                _element = element;
                _property = property;

                if (_element is FrameworkElement fe)
                {
                    var dpd = DependencyPropertyDescriptor.FromProperty(_property, _element.GetType());
                    dpd?.AddValueChanged(_element, OnPropertyChanged);
                    fe.Unloaded += (s, e) => dpd?.RemoveValueChanged(_element, OnPropertyChanged);
                }
            }

            /// <summary>
            /// Called when the monitored property changes; triggers a brush update.
            /// </summary>
            private void OnPropertyChanged(object? sender, EventArgs e) => UpdateBrush();

            /// <summary>
            /// Creates a modified brush based on the original background and applies it to the element.
            /// </summary>
            internal void UpdateBrush()
            {
                var originalBrush = _element.GetValue(_property) as SolidColorBrush;
                if (originalBrush == null)
                {
                    return;
                }

                // If the current brush already has our "IsModified" flag, stop to prevent an endless loop.
                if (GetIsModified(originalBrush))
                {
                    return;
                }

                double percentage = GetAdjustBackgroundPercentage(_element);
                if (Math.Abs(percentage) < 0.001)
                {
                    return;
                }

                percentage = Math.Clamp(percentage, -1.0, 1.0);
                Color originalColor = originalBrush.Color;

                ToHsl(originalColor, out var h, out var s, out var l);
                l *= (1.0 + percentage);
                l = Math.Clamp(l, 0.0, 1.0);
                Color newColor = FromHsl(h, s, l);
                newColor.A = originalColor.A;

                var newBrush = new SolidColorBrush(newColor);

                // Set our attached property on the new brush we've just created.
                SetIsModified(newBrush, true);
                newBrush.Freeze();

                _element.SetValue(_property, newBrush);
            }
        }

        #region Color Conversion
        /// <summary>
        /// Converts an RGB <see cref="Color"/> to HSL components.
        /// </summary>
        /// <param name="rgb">The input RGB color.</param>
        /// <param name="h">Output hue (0-360).</param>
        /// <param name="s">Output saturation (0-1).</param>
        /// <param name="l">Output lightness (0-1).</param>
        private static void ToHsl(Color rgb, out double h, out double s, out double l)
        {
            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            l = (max + min) / 2.0;
            h = 0;
            s = 0;

            if (delta != 0)
            {
                s = l < 0.5 ? delta / (max + min) : delta / (2.0 - max - min);

                if (r == max)
                {
                    h = (g - b) / delta;
                }
                else if (g == max)
                {
                    h = 2.0 + (b - r) / delta;
                }
                else if (b == max)
                {
                    h = 4.0 + (r - g) / delta;
                }

                h *= 60;

                if (h < 0)
                {
                    h += 360;
                }
            }
        }

        /// <summary>
        /// Creates an RGB <see cref="Color"/> from HSL components.
        /// </summary>
        /// <param name="h">Hue (0-360).</param>
        /// <param name="s">Saturation (0-1).</param>
        /// <param name="l">Lightness (0-1).</param>
        /// <returns>The converted RGB <see cref="Color"/>.</returns>
        private static Color FromHsl(double h, double s, double l)
        {
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l;
            }
            else
            {
                double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                double p = 2 * l - q;
                h /= 360;
                r = HueToRgb(p, q, h + 1.0 / 3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3.0);
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        /// <summary>
        /// Helper that converts a hue to an RGB component.
        /// </summary>
        /// <param name="p">First intermediate value.</param>
        /// <param name="q">Second intermediate value.</param>
        /// <param name="t">Adjusted hue position.</param>
        /// <returns>RGB component value (0-1).</returns>
        private static double HueToRgb(double p, double q, double t)
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
        #endregion
    }
}