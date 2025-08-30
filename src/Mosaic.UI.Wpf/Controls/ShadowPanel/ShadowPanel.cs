/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media.Effects;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A panel control that applies a drop shadow effect to its child content.
    /// Provides properties to control shadow elevation and density/thickness.
    /// </summary>
    public class ShadowPanel : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="Elevation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ElevationProperty = DependencyProperty.Register(
            nameof(Elevation), typeof(double), typeof(ShadowPanel), new PropertyMetadata(8.0, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the elevation of the shadow, which controls the distance of the shadow from the element.
        /// Higher values create a more pronounced shadow effect.
        /// </summary>
        public double Elevation
        {
            get => (double)GetValue(ElevationProperty);
            set => SetValue(ElevationProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShadowThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShadowThicknessProperty = DependencyProperty.Register(
            nameof(ShadowThickness), typeof(double), typeof(ShadowPanel), new PropertyMetadata(10.0, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the thickness (blur radius) of the shadow effect.
        /// Higher values create a more diffused, softer shadow.
        /// </summary>
        public double ShadowThickness
        {
            get => (double)GetValue(ShadowThicknessProperty);
            set => SetValue(ShadowThicknessProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShadowOpacity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShadowOpacityProperty = DependencyProperty.Register(
            nameof(ShadowOpacity), typeof(double), typeof(ShadowPanel), new PropertyMetadata(0.3, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the opacity (density) of the shadow effect.
        /// Value should be between 0.0 (transparent) and 1.0 (opaque).
        /// </summary>
        public double ShadowOpacity
        {
            get => (double)GetValue(ShadowOpacityProperty);
            set => SetValue(ShadowOpacityProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShadowColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(
            nameof(ShadowColor), typeof(Color), typeof(ShadowPanel), new PropertyMetadata(Colors.Black, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the color of the drop shadow effect.
        /// </summary>
        public Color ShadowColor
        {
            get => (Color)GetValue(ShadowColorProperty);
            set => SetValue(ShadowColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShadowDirection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShadowDirectionProperty = DependencyProperty.Register(
            nameof(ShadowDirection), typeof(double), typeof(ShadowPanel), 
            new PropertyMetadata(315.0, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the direction of the shadow in degrees. 
        /// 0 degrees is to the right, 90 degrees is down, 180 degrees is to the left, 270 degrees is up.
        /// Default is 315 degrees (bottom-right).
        /// </summary>
        public double ShadowDirection
        {
            get => (double)GetValue(ShadowDirectionProperty);
            set => SetValue(ShadowDirectionProperty, value);
        }

        /// <summary>
        /// Stores the drop shadow effect instance.
        /// </summary>
        private DropShadowEffect? _dropShadowEffect;

        /// <summary>
        /// Initializes the <see cref="ShadowPanel"/> class and overrides the default style key metadata.
        /// </summary>
        static ShadowPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ShadowPanel), new FrameworkPropertyMetadata(typeof(ShadowPanel)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShadowPanel"/> class.
        /// </summary>
        public ShadowPanel()
        {
            UpdateShadowEffect();
        }

        /// <summary>
        /// Handles changes to shadow-related properties and updates the drop shadow effect.
        /// </summary>
        private static void OnShadowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ShadowPanel shadowPanel)
            {
                shadowPanel.UpdateShadowEffect();
            }
        }

        /// <summary>
        /// Updates the drop shadow effect with current property values.
        /// </summary>
        private void UpdateShadowEffect()
        {
            if (_dropShadowEffect == null)
            {
                _dropShadowEffect = new DropShadowEffect();
                Effect = _dropShadowEffect;
            }

            _dropShadowEffect.ShadowDepth = Elevation;
            _dropShadowEffect.BlurRadius = ShadowThickness;
            _dropShadowEffect.Opacity = ShadowOpacity;
            _dropShadowEffect.Color = ShadowColor;
            _dropShadowEffect.Direction = ShadowDirection;
        }
    }
}
