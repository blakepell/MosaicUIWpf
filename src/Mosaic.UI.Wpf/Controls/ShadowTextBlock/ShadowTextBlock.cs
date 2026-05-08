/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using System.Windows.Media.Effects;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A <see cref="TextBlock"/> that automatically applies a configurable <see cref="DropShadowEffect"/>
    /// to its text via <see cref="BlurRadius"/>, <see cref="ShadowDepth"/>, and <see cref="ShadowColor"/>.
    /// </summary>
    public class ShadowTextBlock : TextBlock
    {
        private readonly DropShadowEffect _dropShadowEffect;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShadowTextBlock"/> class.
        /// </summary>
        public ShadowTextBlock()
        {
            _dropShadowEffect = new DropShadowEffect
            {
                BlurRadius = BlurRadius,
                ShadowDepth = ShadowDepth,
                Color = ShadowColor
            };

            Effect = _dropShadowEffect;
        }

        /// <summary>
        /// Identifies the <see cref="BlurRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(
            nameof(BlurRadius), typeof(double), typeof(ShadowTextBlock),
            new PropertyMetadata(20.0, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the blur radius of the drop shadow. Higher values create a more diffused shadow.
        /// </summary>
        [Category("Shadow")]
        [Description("The blur radius of the drop shadow effect.")]
        [DefaultValue(20.0)]
        public double BlurRadius
        {
            get => (double)GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShadowDepth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShadowDepthProperty = DependencyProperty.Register(
            nameof(ShadowDepth), typeof(double), typeof(ShadowTextBlock),
            new PropertyMetadata(5.0, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the shadow depth, which controls how far the shadow is offset from the text.
        /// </summary>
        [Category("Shadow")]
        [Description("The depth (offset distance) of the drop shadow effect.")]
        [DefaultValue(5.0)]
        public double ShadowDepth
        {
            get => (double)GetValue(ShadowDepthProperty);
            set => SetValue(ShadowDepthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShadowColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShadowColorProperty = DependencyProperty.Register(
            nameof(ShadowColor), typeof(Color), typeof(ShadowTextBlock),
            new PropertyMetadata(Colors.Black, OnShadowPropertyChanged));

        /// <summary>
        /// Gets or sets the color of the drop shadow effect.
        /// </summary>
        [Category("Shadow")]
        [Description("The color of the drop shadow effect.")]
        public Color ShadowColor
        {
            get => (Color)GetValue(ShadowColorProperty);
            set => SetValue(ShadowColorProperty, value);
        }

        /// <summary>
        /// Handles changes to any shadow-related property and synchronizes the underlying <see cref="DropShadowEffect"/>.
        /// </summary>
        private static void OnShadowPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ShadowTextBlock stb)
            {
                stb._dropShadowEffect.BlurRadius = stb.BlurRadius;
                stb._dropShadowEffect.ShadowDepth = stb.ShadowDepth;
                stb._dropShadowEffect.Color = stb.ShadowColor;
            }
        }
    }
}
