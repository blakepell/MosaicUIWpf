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
        /// The text property that should be bound to.  This value will then trigger the update of <see cref="MaskedText"/> which
        /// is what is shown on this control.
        /// </summary>
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static readonly DependencyPropertyKey MaskedTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MaskedText), typeof(string), typeof(AsteriskTextBlock), 
            new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="MaskedText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaskedTextProperty = MaskedTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The text that is displayed on this TextBlock. This is automatically updated based on the <see cref="Text"/> property.
        /// </summary>
        public string MaskedText
        {
            get => (string)GetValue(MaskedTextProperty);
            private set => SetValue(MaskedTextPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaskCharacter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaskCharacterProperty = DependencyProperty.Register(
            nameof(MaskCharacter), typeof(char), typeof(AsteriskTextBlock), new PropertyMetadata('\u25cf', OnMaskCharacterChanged));

        /// <summary>
        /// The asterisk character to use for the mask.
        /// </summary>
        public char MaskCharacter
        {
            get => (char)GetValue(MaskCharacterProperty);
            set => SetValue(MaskCharacterProperty, value);
        }

        private static void OnMaskCharacterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AsteriskTextBlock)d;
            control.UpdateText();
        }

        /// <summary>
        /// A text block that displays asterisks for each character in its text property.
        /// </summary>
        static AsteriskTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AsteriskTextBlock), new FrameworkPropertyMetadata(typeof(AsteriskTextBlock)));
        }

        /// <summary>
        /// Handles updating the <see cref="MaskedText" /> property whenever the Text has been updated.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (AsteriskTextBlock)d;
            control.UpdateText();
        }

        /// <summary>
        /// Handles updating the <see cref="MaskedText" /> property.
        /// </summary>
        private void UpdateText()
        {
            MaskedText = string.IsNullOrEmpty(Text) ? string.Empty : new string(MaskCharacter, Text.Length);
        }
    }
}