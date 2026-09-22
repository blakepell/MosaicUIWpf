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
    /// <summary>
    /// Exposes a <see cref="CommandBox"/> to UI Automation as an editable text control.
    /// </summary>
    /// <remarks>
    /// AvalonEdit does not publish a usable automation peer for its <see cref="ICSharpCode.AvalonEdit.TextEditor"/>,
    /// so the command box supplies this one. It implements the Value
    /// pattern over the command text, which lets a screen reader read (and a test harness set) the
    /// current command, and it is the peer the control notifies when history navigation replaces
    /// the text.
    /// </remarks>
    public class CommandBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        /// <summary>
        /// The command box this peer represents.
        /// </summary>
        private readonly CommandBox _owner;

        /// <summary>
        /// Creates a new peer.
        /// </summary>
        /// <param name="owner">The command box this peer represents.</param>
        public CommandBoxAutomationPeer(CommandBox owner)
            : base(owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Reports the control as an edit control.
        /// </summary>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        /// <summary>
        /// The class name reported to automation clients.
        /// </summary>
        protected override string GetClassNameCore()
        {
            return nameof(CommandBox);
        }

        /// <summary>
        /// Reports the control as a content element so its text is surfaced to screen readers.
        /// </summary>
        protected override bool IsContentElementCore()
        {
            return true;
        }

        /// <summary>
        /// Falls back to the watermark when no <see cref="AutomationProperties.NameProperty"/> has been set,
        /// so the box is never announced as unnamed.
        /// </summary>
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            return string.IsNullOrEmpty(name) ? _owner.Watermark : name;
        }

        /// <summary>
        /// Returns this peer for the Value pattern and defers everything else to the base peer.
        /// </summary>
        /// <param name="patternInterface">The pattern being requested.</param>
        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        /// <summary>
        /// Whether the command box rejects edits.
        /// </summary>
        public bool IsReadOnly => _owner.IsReadOnly;

        /// <summary>
        /// The current command text.
        /// </summary>
        public string Value => _owner.Text;

        /// <summary>
        /// Replaces the command text.
        /// </summary>
        /// <param name="value">The text to place in the box.</param>
        /// <exception cref="ElementNotEnabledException">The command box is disabled or read-only.</exception>
        public void SetValue(string value)
        {
            if (!_owner.IsEnabled || _owner.IsReadOnly)
            {
                throw new ElementNotEnabledException();
            }

            _owner.Text = value ?? string.Empty;
        }

        /// <summary>
        /// Notifies automation clients that the command text changed, which is what makes a screen
        /// reader announce a command recalled from the history.
        /// </summary>
        /// <param name="oldValue">The previous text.</param>
        /// <param name="newValue">The new text.</param>
        internal void RaiseValueChanged(string oldValue, string newValue)
        {
            this.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, oldValue, newValue);
        }
    }
}
