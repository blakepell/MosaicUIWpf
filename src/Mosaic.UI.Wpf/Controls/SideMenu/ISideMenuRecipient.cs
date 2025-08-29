/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a recipient of side menu interactions, providing properties and methods  for handling menu-related
    /// data and actions.
    /// </summary>
    public interface ISideMenuRecipient
    {
        /// <summary>
        /// Gets or sets the source of the image to be displayed.
        /// </summary>
        ImageSource? ImageSource { get; set; }

        /// <summary>
        /// Dictionary of parameters passed from the SideMenuItem to the content control.
        /// This provides flexible data transfer without requiring specific property names.
        /// </summary>
        IReadOnlyDictionary<string, object?> Parameters { get; set; }

        /// <summary>
        /// Refreshes the current state of the object, updating its data or configuration to reflect any changes since
        /// the last refresh.
        /// </summary>
        void Refresh();
    }
}