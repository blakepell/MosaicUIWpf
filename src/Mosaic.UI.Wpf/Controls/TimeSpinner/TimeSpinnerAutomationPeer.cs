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
    /// Exposes a <see cref="TimeSpinner"/> to UI Automation as a single expandable element whose value is the
    /// selected time. The individual wheels are reachable as children and name themselves through their own peers.
    /// </summary>
    internal sealed class TimeSpinnerAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IValueProvider
    {
        public TimeSpinnerAutomationPeer(TimeSpinner owner) : base(owner)
        {
        }

        private TimeSpinner OwnerSpinner => (TimeSpinner)this.Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(TimeSpinner);
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

            return "Time";
        }

        /// <inheritdoc />
        protected override bool IsContentElementCore()
        {
            return true;
        }

        /// <inheritdoc />
        protected override string GetLocalizedControlTypeCore()
        {
            return "time picker";
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
        public string Value => Describe(this.OwnerSpinner, this.OwnerSpinner.SelectedTime);

        /// <summary>
        /// Sets the selected time from a string. An empty string clears the selection; anything else is parsed with
        /// the spinner's effective culture and then with the invariant culture.
        /// </summary>
        /// <param name="value">The time to select.</param>
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

            if (TimeOnly.TryParse(value, spinner.EffectiveCulture, DateTimeStyles.None, out var parsed) ||
                TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                spinner.SelectedTime = parsed;
                return;
            }

            throw new ArgumentException($"'{value}' could not be parsed as a time.", nameof(value));
        }

        #endregion

        /// <summary>
        /// Announces a selection change to assistive technology. Called by the owner rather than derived from a
        /// property so the announcement carries the same text the value pattern reports.
        /// </summary>
        /// <param name="oldTime">The previously selected time.</param>
        /// <param name="newTime">The newly selected time.</param>
        internal void RaiseValueChanged(TimeOnly? oldTime, TimeOnly? newTime)
        {
            var spinner = this.OwnerSpinner;

            this.RaisePropertyChangedEvent(
                ValuePatternIdentifiers.ValueProperty,
                Describe(spinner, oldTime),
                Describe(spinner, newTime));
        }

        /// <summary>
        /// Builds the spoken form of a time, honoring whether the spinner actually shows minutes so an hour picker
        /// does not announce a minute the user never chose.
        /// </summary>
        private static string Describe(TimeSpinner spinner, TimeOnly? time)
        {
            if (!time.HasValue)
            {
                return "No time selected";
            }

            var culture = spinner.EffectiveCulture;
            var value = time.Value;
            var meridiem = TimeSpinnerClock.ToMeridiem(value);
            var parts = new List<string>(3)
            {
                TimeSpinnerClock.ToHour12(value).ToString(culture)
            };

            if (spinner.IsMinuteVisible)
            {
                parts.Add(value.Minute.ToString("00", culture));
            }

            parts.Add(TimeSpinnerClock.FormatMeridiem(meridiem, culture, spinner.MeridiemDisplayMode));

            return string.Join(" ", parts);
        }
    }
}
