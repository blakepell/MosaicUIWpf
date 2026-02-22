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

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using ToggleStateValue = System.Windows.Automation.ToggleState;

namespace Mosaic.UI.Wpf.Controls
{
    internal sealed class ToggleSwitchAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider, IInvokeProvider
    {
        public ToggleSwitchAutomationPeer(ToggleSwitch owner) : base(owner)
        {
        }

        private ToggleSwitch OwnerSwitch => (ToggleSwitch)Owner;

        protected override string GetClassNameCore()
        {
            return nameof(ToggleSwitch);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Toggle || patternInterface == PatternInterface.Invoke)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        public ToggleStateValue ToggleState => OwnerSwitch.IsOn ? ToggleStateValue.On : ToggleStateValue.Off;

        public void Toggle()
        {
            if (!OwnerSwitch.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerSwitch.ToggleFromAutomation();
        }

        public void Invoke()
        {
            Toggle();
        }

        internal void RaiseToggleStateChanged(bool oldValue, bool newValue)
        {
            RaisePropertyChangedEvent(
                TogglePatternIdentifiers.ToggleStateProperty,
                oldValue ? ToggleStateValue.On : ToggleStateValue.Off,
                newValue ? ToggleStateValue.On : ToggleStateValue.Off);
        }
    }
}
