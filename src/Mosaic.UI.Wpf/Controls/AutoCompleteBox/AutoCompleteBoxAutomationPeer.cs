/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    internal sealed class AutoCompleteBoxAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IValueProvider
    {
        public AutoCompleteBoxAutomationPeer(AutoCompleteBox owner) : base(owner)
        {
        }

        private AutoCompleteBox OwnerBox => (AutoCompleteBox)Owner;

        protected override string GetClassNameCore()
        {
            return nameof(AutoCompleteBox);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ComboBox;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? OwnerBox.Text : name;
        }

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse || patternInterface == PatternInterface.Value)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        public ExpandCollapseState ExpandCollapseState => OwnerBox.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public void Expand()
        {
            if (!OwnerBox.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerBox.OpenDropDown();
        }

        public void Collapse()
        {
            if (!OwnerBox.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerBox.CloseDropDown();
        }

        public bool IsReadOnly => false;

        public string Value => OwnerBox.Text;

        public void SetValue(string value)
        {
            if (!OwnerBox.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerBox.Text = value;
        }

        internal void RaiseExpandCollapseStateChanged(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }

        internal void RaiseValueChanged(string oldValue, string newValue)
        {
            RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
        }
    }
}
