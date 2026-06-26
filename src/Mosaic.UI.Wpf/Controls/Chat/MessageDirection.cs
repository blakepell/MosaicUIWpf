/*
 * Mosaic UI for WPF
 *
 * ChatThread Based on: https://github.com/SamKr/ChatBubbles
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
