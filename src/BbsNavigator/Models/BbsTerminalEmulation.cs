/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls.VT52Terminal;

namespace BbsNavigator.Models
{
    /// <summary>Identifies the terminal personality used for a BBS profile.</summary>
    public enum BbsTerminalEmulation
    {
        /// <summary>Classic PC ANSI-BBS behavior and an <c>ANSI</c> Telnet terminal type.</summary>
        [Description("ANSI-BBS (classic PC)")]
        AnsiBbs,

        /// <summary>VT100-compatible behavior.</summary>
        [Description("VT100")]
        Vt100,

        /// <summary>Modern xterm-compatible behavior with 256 colors.</summary>
        [Description("xterm-256color")]
        Xterm256Color,

        /// <summary>Legacy DEC VT52 escape sequences.</summary>
        [Description("VT52")]
        Vt52,

        /// <summary>Basic teletype behavior without ANSI escape-sequence parsing.</summary>
        [Description("TTY / plain text")]
        Tty
    }

    /// <summary>Converts BBS profile emulation choices to reusable terminal settings.</summary>
    public static class BbsTerminalEmulationExtensions
    {
        /// <summary>Returns the parser mode used by the Mosaic terminal.</summary>
        public static TerminalEmulationMode ToTerminalMode(this BbsTerminalEmulation emulation) => emulation switch
        {
            BbsTerminalEmulation.Vt52 => TerminalEmulationMode.Vt52,
            BbsTerminalEmulation.Tty => TerminalEmulationMode.Tty,
            _ => TerminalEmulationMode.Ansi
        };

        /// <summary>Returns the default Telnet TTYPE value for the selected emulation.</summary>
        public static string ToTerminalType(this BbsTerminalEmulation emulation) => emulation switch
        {
            BbsTerminalEmulation.AnsiBbs => "ANSI",
            BbsTerminalEmulation.Vt100 => "VT100",
            BbsTerminalEmulation.Vt52 => "VT52",
            BbsTerminalEmulation.Tty => "DUMB",
            _ => "xterm-256color"
        };
    }
}
