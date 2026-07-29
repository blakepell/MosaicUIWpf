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
    /// <summary>
    /// Exposes a <see cref="CopyTextBox"/> to UI Automation as an editable value that can also be
    /// invoked to place its text onto the clipboard.
    /// </summary>
    internal sealed class CopyTextBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider, IInvokeProvider
    {
        public CopyTextBoxAutomationPeer(CopyTextBox owner) : base(owner)
        {
        }

        private CopyTextBox OwnerCopyTextBox => (CopyTextBox)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(CopyTextBox);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface is PatternInterface.Value or PatternInterface.Invoke)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public bool IsReadOnly => OwnerCopyTextBox.IsReadOnly;

        /// <inheritdoc />
        public string Value => OwnerCopyTextBox.Text ?? string.Empty;

        /// <inheritdoc />
        public void SetValue(string value)
        {
            if (!OwnerCopyTextBox.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            if (OwnerCopyTextBox.IsReadOnly)
            {
                throw new InvalidOperationException("The CopyTextBox is read only.");
            }

            OwnerCopyTextBox.Text = value ?? string.Empty;
        }

        /// <summary>
        /// Copies the control's text to the clipboard, the same action the copy button performs.
        /// </summary>
        public void Invoke()
        {
            if (!OwnerCopyTextBox.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerCopyTextBox.Copy();
        }
    }
}
