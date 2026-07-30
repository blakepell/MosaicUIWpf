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
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Exposes a <see cref="DateSpinner"/> to UI Automation as a single expandable element whose value is the
    /// selected date. The individual wheels are reachable as children and name themselves through their own peers.
    /// </summary>
    internal sealed class DateSpinnerAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IValueProvider
    {
        public DateSpinnerAutomationPeer(DateSpinner owner) : base(owner)
        {
        }

        private DateSpinner OwnerSpinner => (DateSpinner)this.Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(DateSpinner);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ComboBox;
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            // A string header is the label the user sees, so it doubles as the accessible name.
            if (this.OwnerSpinner.Header is string header && !string.IsNullOrWhiteSpace(header))
            {
                return header;
            }

            return "Date";
        }

        /// <inheritdoc />
        protected override bool IsContentElementCore()
        {
            return true;
        }

        /// <inheritdoc />
        protected override string GetLocalizedControlTypeCore()
        {
            return "date picker";
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse || patternInterface == PatternInterface.Value)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        #region ExpandCollapse

        /// <inheritdoc />
        public ExpandCollapseState ExpandCollapseState =>
            this.OwnerSpinner.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        /// <inheritdoc />
        public void Expand()
        {
            var spinner = this.OwnerSpinner;

            if (!spinner.IsEnabled || spinner.IsReadOnly)
            {
                throw new ElementNotEnabledException();
            }

            spinner.Open();
        }

        /// <inheritdoc />
        public void Collapse()
        {
            if (!this.OwnerSpinner.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            this.OwnerSpinner.Close();
        }

        #endregion

        #region Value

        /// <inheritdoc />
        public bool IsReadOnly => this.OwnerSpinner.IsReadOnly || !this.OwnerSpinner.IsEnabled;

        /// <inheritdoc />
        public string Value => Describe(this.OwnerSpinner, this.OwnerSpinner.SelectedDate);

        /// <summary>
        /// Sets the selected date from a string. An empty string clears the selection; anything else is parsed
        /// with the spinner's effective culture and then with the invariant culture.
        /// </summary>
        /// <param name="value">The date to select.</param>
        public void SetValue(string value)
        {
            var spinner = this.OwnerSpinner;

            if (this.IsReadOnly)
            {
                throw new ElementNotEnabledException();
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                spinner.Clear();
                return;
            }

            if (DateTime.TryParse(value, spinner.EffectiveCulture, DateTimeStyles.None, out var parsed) ||
                DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                spinner.SelectedDate = parsed;
                return;
            }

            throw new ArgumentException($"'{value}' could not be parsed as a date.", nameof(value));
        }

        #endregion

        /// <summary>
        /// Announces a selection change to assistive technology. Called by the owner rather than derived from a
        /// property so the announcement carries the same text the value pattern reports.
        /// </summary>
        /// <param name="oldDate">The previously selected date.</param>
        /// <param name="newDate">The newly selected date.</param>
        internal void RaiseValueChanged(DateTime? oldDate, DateTime? newDate)
        {
            var spinner = this.OwnerSpinner;

            this.RaisePropertyChangedEvent(
                ValuePatternIdentifiers.ValueProperty,
                Describe(spinner, oldDate),
                Describe(spinner, newDate));
        }

        /// <summary>
        /// Builds the spoken form of a date, honoring which fields the spinner actually shows so a month and year
        /// picker does not announce a day the user never chose.
        /// </summary>
        private static string Describe(DateSpinner spinner, DateTime? date)
        {
            if (!date.HasValue)
            {
                return "No date selected";
            }

            var culture = spinner.EffectiveCulture;
            var value = date.Value;
            var parts = new List<string>(3);

            if (spinner.IsMonthVisible)
            {
                parts.Add(culture.DateTimeFormat.GetMonthName(value.Month));
            }

            if (spinner.IsDayVisible)
            {
                parts.Add(value.Day.ToString(culture));
            }

            if (spinner.IsYearVisible)
            {
                parts.Add(value.Year.ToString("0000", culture));
            }

            return string.Join(" ", parts);
        }
    }
}
