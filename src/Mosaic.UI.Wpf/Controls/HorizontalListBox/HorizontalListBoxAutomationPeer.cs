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
    /// Exposes <see cref="HorizontalListBox"/> to UI Automation. Selection patterns are inherited
    /// from <see cref="ListBoxAutomationPeer"/> so each cell reports its toggled state as a
    /// selection item.
    /// </summary>
    internal sealed class HorizontalListBoxAutomationPeer : ListBoxAutomationPeer
    {
        public HorizontalListBoxAutomationPeer(HorizontalListBox owner) : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return nameof(HorizontalListBox);
        }
    }
}
