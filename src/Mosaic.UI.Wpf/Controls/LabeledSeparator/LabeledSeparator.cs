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
    /// A labeled separator
    /// </summary>
    public class LabeledSeparator : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(LabeledSeparator), new PropertyMetadata(default(string)));

        /// <summary>
        /// Gets or sets the text content associated with this element.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LabelPosition"/> dependency property, which determines the position of the label
        /// relative to the separator.`
        /// </summary>
        public static readonly DependencyProperty LabelPositionProperty = DependencyProperty.Register(
            nameof(LabelPosition), typeof(LabelPosition), typeof(LabeledSeparator), new PropertyMetadata(LabelPosition.Left));

        /// <summary>
        /// Gets or sets the position of the label relative to its associated control.
        /// </summary>
        public LabelPosition LabelPosition
        {
            get => (LabelPosition)GetValue(LabelPositionProperty);
            set => SetValue(LabelPositionProperty, value);
        }
        
        static LabeledSeparator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LabeledSeparator), new FrameworkPropertyMetadata(typeof(LabeledSeparator)));
            BackgroundProperty.OverrideMetadata(typeof(LabeledSeparator), new FrameworkPropertyMetadata(SystemColors.GrayTextBrush));
        }
    }
}
