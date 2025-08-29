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
    /// TextBox that only allows digits, minus sign and a decimal point.
    /// </summary>
    public class NumericTextBox : TextBox
    {
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
            if (Regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
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
        }
    }
}