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
    /// Represents a button control that can switch between two states: checked and unchecked.  This
    /// implementation looks like a theme styled switch.
    /// </summary>
    /// <remarks>
    /// This is based on code from https://github.com/WPFDevelopersOrg/WPFDevelopers available via the MIT License.
    /// </remarks>
    public class ToggleButton : System.Windows.Controls.Primitives.ToggleButton
    {
        /// <summary>
        /// Identifies the <see cref="CheckedBackground"/> dependency property.
        /// </summary>
        /// <remarks>This property represents the background brush used when the <see
        /// cref="ToggleButton"/> is in the checked state.</remarks>
        public static readonly DependencyProperty CheckedBackgroundProperty =
            DependencyProperty.Register(nameof(CheckedBackground), typeof(Brush), typeof(ToggleButton), new PropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// Gets of sets the background color when the ToggleButton is in the "Checked" state.
        /// </summary>
        public Brush CheckedBackground
        {
            get => (Brush)GetValue(CheckedBackgroundProperty);
            set => SetValue(CheckedBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="UncheckedBackground"/> dependency property.
        /// </summary>
        /// <remarks>This property represents the background brush used for the <see cref="ToggleButton"/>
        /// when it is in the unchecked state. The default value is <see cref="Brushes.Transparent"/>.</remarks>
        public static readonly DependencyProperty UncheckedBackgroundProperty =
            DependencyProperty.Register(nameof(UncheckedBackground), typeof(Brush), typeof(ToggleButton), new PropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// Gets or sets the background color when the ToggleButton is in the "Unchecked" state.
        /// </summary>
        public Brush UncheckedBackground
        {
            get => (Brush)GetValue(UncheckedBackgroundProperty);
            set => SetValue(UncheckedBackgroundProperty, value);
        }
    }
}
