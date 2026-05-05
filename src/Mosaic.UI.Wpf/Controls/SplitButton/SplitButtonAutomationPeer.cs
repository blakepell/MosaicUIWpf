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

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Mosaic.UI.Wpf.Controls
{
    internal sealed class SplitButtonAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider
    {
        public SplitButtonAutomationPeer(SplitButton owner) : base(owner)
        {
        }

        private SplitButton OwnerButton => (SplitButton)Owner;

        protected override string GetClassNameCore()
        {
            return nameof(SplitButton);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return OwnerButton.Content?.ToString() ?? string.Empty;
        }

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke || patternInterface == PatternInterface.ExpandCollapse)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        public void Invoke()
        {
            if (!OwnerButton.CanInvokePrimaryAction)
            {
                throw new ElementNotEnabledException();
            }

            OwnerButton.InvokePrimaryActionFromAutomation();
        }

        public ExpandCollapseState ExpandCollapseState => OwnerButton.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public void Expand()
        {
            if (!OwnerButton.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerButton.ExpandFromAutomation();
        }

        public void Collapse()
        {
            if (!OwnerButton.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerButton.CollapseFromAutomation();
        }

        internal void RaiseExpandCollapseStateChanged(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }
    }
}
