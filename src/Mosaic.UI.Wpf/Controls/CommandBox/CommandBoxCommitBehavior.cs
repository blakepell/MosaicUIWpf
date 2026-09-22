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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Describes what a <see cref="CommandBox"/> does with the text it just dispatched.
    /// </summary>
    public enum CommandBoxCommitBehavior
    {
        /// <summary>
        /// The text is removed from the box, leaving it empty and ready for the next command.
        /// </summary>
        Clear,

        /// <summary>
        /// The text and the caret position are left exactly as they were. Useful when a command is
        /// likely to be edited slightly and run again.
        /// </summary>
        Keep,

        /// <summary>
        /// The text is left in place but fully selected, so the next keystroke replaces it while
        /// the previous command stays visible and can still be edited with an arrow key. This is
        /// the default.
        /// </summary>
        SelectAll
    }
}
