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
    /// Exposes <see cref="TagBox"/> to UI Automation.
    /// </summary>
    internal sealed class TagBoxAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagBoxAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="TagBox"/>.</param>
        public TagBoxAutomationPeer(TagBox owner) : base(owner)
        {
        }

        /// <summary>
        /// The strongly typed owner of this peer.
        /// </summary>
        private TagBox OwnerTagBox => (TagBox)this.Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(TagBox);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public ExpandCollapseState ExpandCollapseState =>
            this.OwnerTagBox.IsSuggestionListOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        /// <inheritdoc />
        public void Expand()
        {
            this.OwnerTagBox.OpenSuggestionList();
        }

        /// <inheritdoc />
        public void Collapse()
        {
            this.OwnerTagBox.CloseSuggestionList();
        }

        /// <summary>
        /// Raises the automation property-changed event for the auto-complete drop-down state.
        /// </summary>
        /// <param name="oldValue">The previous open state.</param>
        /// <param name="newValue">The new open state.</param>
        internal void RaiseExpandCollapseStateChanged(bool oldValue, bool newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            this.RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        }
    }
}
