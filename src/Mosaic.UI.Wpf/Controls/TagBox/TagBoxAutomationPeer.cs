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

using System.Windows.Automation.Peers;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Exposes <see cref="TagBox"/> to UI Automation.
    /// </summary>
    internal sealed class TagBoxAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TagBoxAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The owning <see cref="TagBox"/>.</param>
        public TagBoxAutomationPeer(TagBox owner) : base(owner)
        {
        }

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
    }
}
