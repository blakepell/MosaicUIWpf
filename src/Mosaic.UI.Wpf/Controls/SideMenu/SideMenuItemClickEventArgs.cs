/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Provides data for the <see cref="SideMenuItem.Click"/> and <see cref="SideMenu.ItemClicked"/> events.
    /// </summary>
    public class SideMenuItemClickEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="SideMenuItem"/> that was clicked.
        /// </summary>
        public SideMenuItem Item { get; }

        /// <summary>
        /// Gets a read-only snapshot of the parameters associated with the clicked menu item at the time
        /// the event was raised. Changes made to <see cref="SideMenuItem.Parameters"/> after this event
        /// fires are not reflected here.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Parameters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SideMenuItemClickEventArgs"/> class.
        /// </summary>
        /// <param name="item">The menu item that was clicked.</param>
        public SideMenuItemClickEventArgs(SideMenuItem item)
        {
            Item = item;
            Parameters = new Dictionary<string, object?>(item.Parameters);
        }
    }
}
