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
    /// Exposes the <see cref="Files"/> control to UI Automation as a list of files.
    /// </summary>
    internal sealed class FilesAutomationPeer : FrameworkElementAutomationPeer
    {
        public FilesAutomationPeer(Files owner) : base(owner)
        {
        }

        protected override string GetClassNameCore()
        {
            return nameof(Files);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.List;
        }
    }
}
