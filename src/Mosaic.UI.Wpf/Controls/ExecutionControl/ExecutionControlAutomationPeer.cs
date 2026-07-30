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
    /// Exposes an <see cref="ExecutionControl"/> to UI Automation as a tool bar whose children are
    /// the individual transport buttons.
    /// </summary>
    public class ExecutionControlAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionControlAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="ExecutionControl"/> this peer represents.</param>
        public ExecutionControlAutomationPeer(ExecutionControl owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ToolBar;
        }

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(ExecutionControl);
        }
    }
}
