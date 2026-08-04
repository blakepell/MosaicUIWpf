/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Networking
{
    /// <summary>
    /// Routes the SSH shell payload stream to a file transfer protocol while terminal text
    /// delivery is suspended. The SSH channel is already 8-bit clean, so bytes are written
    /// through unchanged.
    /// </summary>
    public sealed class SshBinaryChannel : QueuedBinaryChannel
    {
        private readonly BbsSshConnection _connection;

        /// <summary>
        /// Initializes the channel for the specified connection. Use
        /// <see cref="BbsSshConnection.EnterBinaryMode"/> rather than constructing this directly.
        /// </summary>
        /// <param name="connection">The owning connection.</param>
        internal SshBinaryChannel(BbsSshConnection connection)
        {
            _connection = connection;
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return _connection.SendBinaryAsync(buffer, cancellationToken);
        }

        /// <inheritdoc />
        protected override void ReleaseConnection()
        {
            _connection.ExitBinaryMode(this);
        }
    }
}
