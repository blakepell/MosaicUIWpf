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

using System.Globalization;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// TextBox that only allows digits, minus sign and a decimal point.
    /// </summary>
    public class NumericTextBox : TextBox
    {
        /// <summary>
        /// Identifies the <see cref="MinValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue),
            typeof(int?),
            typeof(NumericTextBox),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="MaxValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue),
            typeof(int?),
            typeof(NumericTextBox),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the number of decimal places to use for formatting numeric values.  A value of =1
        /// indicates unlimited decimal places.
        /// </summary>
        public int DecimalPlaces { get; set; } = -1;

        /// <summary>
        /// Gets or sets the minimum value that may be entered. A value of <see langword="null"/>
        /// indicates that no minimum is enforced.
        /// </summary>
        public int? MinValue
        {
            get => (int?)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum value that may be entered. A value of <see langword="null"/>
        /// indicates that no maximum is enforced.
        /// </summary>
        public int? MaxValue
        {
            get => (int?)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        /// <summary>
        /// Regex to allow digits, minus sign, and decimal point.
        /// </summary>
        private static readonly Regex Regex = new("[^0-9.-]+");

        /// <summary>
        /// Constructor
        /// </summary>
        public NumericTextBox()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataObject.AddPastingHandler(this, HandlePaste);
        }

        /// <summary>
        /// Loaded: Wire up our key handling events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PreviewTextInput += HandlePreviewTextInput;
            PreviewKeyDown += HandlePreviewKeyDown;
        }

        /// <summary>
        /// Unloaded: Release any handlers we wired up.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            PreviewTextInput -= HandlePreviewTextInput;
            PreviewKeyDown -= HandlePreviewKeyDown;
        }

        /// <summary>
        /// Process input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandlePreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (Regex.IsMatch(e.Text) || !IsInputAllowed(e.Text))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Rejects pasted content that is not a valid number or falls outside the configured range.
        /// </summary>
        private void HandlePaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText) ||
                e.SourceDataObject.GetData(DataFormats.UnicodeText) is not string pastedText ||
                Regex.IsMatch(pastedText) ||
                !IsInputAllowed(pastedText))
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// Determines whether inserting text at the current selection produces an allowed value.
        /// </summary>
        private bool IsInputAllowed(string input)
        {
            string candidate = Text.Remove(SelectionStart, SelectionLength).Insert(SelectionStart, input);

            // Permit temporary editing states that do not yet represent a number.
            if (string.IsNullOrEmpty(candidate) || candidate == "-")
            {
                return true;
            }

            if (!decimal.TryParse(candidate, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out decimal value))
            {
                return false;
            }

            if (DecimalPlaces >= 0)
            {
                int decimalPoint = candidate.IndexOf('.');
                if (decimalPoint >= 0 && candidate.Length - decimalPoint - 1 > DecimalPlaces)
                {
                    return false;
                }
            }

            return (!MinValue.HasValue || value >= MinValue.Value) &&
                   (!MaxValue.HasValue || value <= MaxValue.Value);
        }

        /// <summary>
        /// Process input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Disallow spaces
                return;
            }

            // Limit to one decimal point and one minus sign at the beginning.
            if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
            {
                // If DecimalPlaces is 0, no decimal points are allowed.
                if (this.DecimalPlaces == 0)
                {
                    e.Handled = true;
                    return;
                }

                if (Text.Contains('.'))
                {
                    e.Handled = true; // Disallow multiple decimal point.
                    return;
                }
            }

            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                if (CaretIndex != 0 || Text.Contains('-'))
                {
                    e.Handled = true; // Disallow minus sign not at the start or multiple minus signs
                    return;
                }
            }

            // A decimal can't be the first character.
            if ((e.Key == Key.OemPeriod || e.Key == Key.Decimal) && CaretIndex == 0)
            {
                e.Handled = true;
                return;
            }

            // Enforce decimal places if set.
            if (DecimalPlaces > 0 && (e.Key == Key.OemPeriod || e.Key == Key.Decimal || !char.IsControl((char)KeyInterop.VirtualKeyFromKey(e.Key))))
            {
                string? text = Text;
                if (SelectionLength > 0)
                {
                    text = text.Remove(SelectionStart, SelectionLength);
                }

                text = text.Insert(CaretIndex, e.Key == Key.OemPeriod || e.Key == Key.Decimal ? "." : e.Key.ToString().Replace("D", "").Replace("NumPad", ""));
                var parts = text.Split('.');

                if (parts.Length > 1 && parts[1].Length > DecimalPlaces)
                {
                    e.Handled = true; // Disallow input that would exceed the decimal places.
                }
            }
        }
    }
}
