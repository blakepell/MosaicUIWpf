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
    /// Specifies the protocol a BBS session runs over.
    /// </summary>
    public enum BbsTransport
    {
        /// <summary>
        /// Telnet on the profile's <see cref="BbsProfile.Port"/>. This is the default for a BBS.
        /// </summary>
        Telnet,

        /// <summary>
        /// SSH on the profile's <see cref="BbsProfile.SshPort"/>, using stored or prompted credentials.
        /// </summary>
        Ssh
    }
}
