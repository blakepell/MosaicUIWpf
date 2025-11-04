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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A badge component.
    /// </summary>
    public class Badge : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(Badge), new PropertyMetadata(default(string)));

        /// <summary>
        /// Gets or sets the text content associated with this element.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Initializes static metadata for the <see cref="Badge"/> class.
        /// </summary>
        static Badge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(typeof(Badge)));
            ForegroundProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(Brushes.White));
            BackgroundProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(Brushes.CornflowerBlue));
            HeightProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(20.0));
            HorizontalContentAlignmentProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(HorizontalAlignment.Center));
            BorderThicknessProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(new Thickness(0.0)));
        }
    }
}
