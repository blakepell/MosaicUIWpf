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
    /// Describes the current state of a BBS session.
    /// </summary>
    public enum BbsConnectionState
    {
        /// <summary>
        /// Indicates that no connection is active.
        /// </summary>
        Disconnected,

        /// <summary>
        /// Indicates that a connection attempt is in progress.
        /// </summary>
        Connecting,

        /// <summary>
        /// Indicates that the BBS session is connected.
        /// </summary>
        Connected,

        /// <summary>
        /// Indicates that an automatic reconnection attempt is in progress.
        /// </summary>
        Reconnecting,

        /// <summary>
        /// Indicates that the connection ended because of an error.
        /// </summary>
        Faulted
    }
}
