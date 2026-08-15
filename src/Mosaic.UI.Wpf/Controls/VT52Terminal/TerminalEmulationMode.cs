/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// Identifies how incoming terminal control characters and escape sequences are interpreted.
    /// </summary>
    public enum TerminalEmulationMode
    {
        /// <summary>ANSI/VT-compatible parsing suitable for PC bulletin board systems.</summary>
        Ansi,

        /// <summary>VT52 parsing, including its two-byte direct cursor addressing sequences.</summary>
        Vt52,

        /// <summary>Basic teletype parsing without interpreting escape sequences.</summary>
        Tty
    }
}
