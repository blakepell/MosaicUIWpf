/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Models
{
    /// <summary>Identifies how a session chooses its text grid dimensions.</summary>
    public enum BbsTerminalDisplayMode
    {
        /// <summary>A fixed 80-column by 25-row IBM VGA-style screen.</summary>
        [Description("Classic ANSI 80 × 25")]
        Classic80X25,

        /// <summary>A grid that follows the available window size.</summary>
        [Description("Responsive to window")]
        Responsive
    }
}
