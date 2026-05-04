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
    /// A single cell in the terminal buffer.
    /// </summary>
    public struct TerminalCell
    {
        public char Char;
        public TerminalAttributes Attrs;

        public TerminalCell(char c, TerminalAttributes attrs)
        {
            Char = c;
            Attrs = attrs;
        }
    }
}
