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
    /// <summary>
    /// Specifies how typed characters are echoed in a BBS session.
    /// </summary>
    public enum BbsLocalEchoMode
    {
        /// <summary>
        /// Echo locally until Telnet WILL ECHO; resume on WONT ECHO. SSH uses remote echo.
        /// </summary>
        Automatic,

        /// <summary>
        /// Always echo typed characters locally.
        /// </summary>
        On,

        /// <summary>
        /// Never echo typed characters locally.
        /// </summary>
        Off
    }
}
