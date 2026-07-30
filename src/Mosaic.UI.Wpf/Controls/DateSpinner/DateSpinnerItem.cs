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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single entry inside a <see cref="DateSpinnerSelector"/> wheel. Instances are created and owned by the
    /// <see cref="DateSpinner"/> that builds the wheel and are only mutated by it, templates bind to the state.
    /// </summary>
    /// <remarks>
    /// A wheel is padded at each end with <see cref="IsSpacer"/> entries so that the first and last real value can
    /// still be scrolled into the centred selection band. Spacers carry no value and can never be selected.
    /// </remarks>
    public sealed class DateSpinnerItem : ObservableObject
    {
        private string _displayText;
        private bool _isSelectable;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateSpinnerItem"/> class representing a real value.
        /// </summary>
        /// <param name="value">The month number, day of month, or year this entry represents.</param>
        /// <param name="displayText">The culture-formatted text to render.</param>
        /// <param name="isSelectable">Whether the value falls inside the spinner's permitted date range.</param>
        public DateSpinnerItem(int value, string displayText, bool isSelectable)
        {
            this.Value = value;
            _displayText = displayText;
            _isSelectable = isSelectable;
        }

        private DateSpinnerItem()
        {
            _displayText = string.Empty;
            this.IsSpacer = true;
        }

        /// <summary>
        /// Creates a blank padding entry used to centre the first and last real values in the selection band.
        /// </summary>
        internal static DateSpinnerItem CreateSpacer()
        {
            return new DateSpinnerItem();
        }

        /// <summary>
        /// The month number (1 to 12), day of month, or four digit year this entry represents. Zero for a spacer.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// The culture-formatted text rendered for this entry. Empty for a spacer.
        /// </summary>
        public string DisplayText
        {
            get => _displayText;
            internal set => this.SetProperty(ref _displayText, value);
        }

        /// <summary>
        /// Whether the entry can be chosen. Values that fall outside the spinner's minimum or maximum date are
        /// present but not selectable, which keeps the wheel's geometry stable as the user moves across a
        /// boundary month or year.
        /// </summary>
        public bool IsSelectable
        {
            get => _isSelectable;
            internal set => this.SetProperty(ref _isSelectable, value);
        }

        /// <summary>
        /// Whether this is a blank padding entry rather than a real value.
        /// </summary>
        public bool IsSpacer { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.IsSpacer ? string.Empty : this.DisplayText;
        }
    }
}
