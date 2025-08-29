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
    /// A text block that displays asterisks for each character in its text property.
    /// </summary>
    public class AsteriskTextBlock : Control
    {
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(AsteriskTextBlock), new PropertyMetadata(string.Empty, OnTextChanged));

        /// <summary>
        /// The text property that should be bound to.  This value will then trigger the update of DisplayText which
        /// is what is shown on this control.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DisplayText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
            nameof(DisplayText), typeof(string), typeof(AsteriskTextBlock), new PropertyMetadata(default(string)));

        /// <summary>
        /// The text that is displayed on this TextBlock.  Do not update this directly, instead update the <see cref="Text"/> property.
        /// </summary>
        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        /// <summary>
        /// The asterisk character to use for the mask.
        /// </summary>
        public char Character { get; set; } = '\u25cf';

        /// <summary>
        /// A text block that displays asterisks for each character in its text property.
        /// </summary>
        static AsteriskTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AsteriskTextBlock), new FrameworkPropertyMetadata(typeof(AsteriskTextBlock)));
        }

        /// <summary>
        /// Handles updating the DisplayText property whenever the Text has been updated.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AsteriskTextBlock)d;
            control.UpdateText();
        }

        /// <summary>
        /// Handles updating the DisplayText property.
        /// </summary>
        private void UpdateText()
        {
            DisplayText = new string(this.Character, Text.Length);
        }
    }
}
