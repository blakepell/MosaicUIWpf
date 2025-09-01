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
    /// Specifies the direction of a message in a communication system.
    /// </summary>
    /// <remarks>This enumeration is used to indicate whether a message was sent or received.  It can be
    /// useful in scenarios such as logging, message processing, or communication protocols  where the direction of a
    /// message needs to be tracked or differentiated.</remarks>
    public enum MessageDirection
    {
        /// <summary>
        /// Represents the state of a message that has been sent.
        /// </summary>
        Sent,
        /// <summary>
        /// Represents the state of a message that has been received.
        /// </summary>
        Received
    }
}
