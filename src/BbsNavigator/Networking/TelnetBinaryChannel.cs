/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Transfers;
using System.Threading.Channels;

namespace BbsNavigator.Networking
{
    /// <summary>
    /// Routes the raw (de-escaped) Telnet payload stream to a file transfer protocol while
    /// terminal text delivery is suspended. Dispose the channel to return the connection to
    /// normal terminal operation.
    /// </summary>
    public sealed class TelnetBinaryChannel : ITransferLink, IDisposable
    {
        private readonly BbsTelnetConnection _connection;
        private readonly Channel<byte[]> _incoming = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        private byte[]? _current;
        private int _offset;
        private bool _disposed;

        /// <summary>
        /// Initializes the channel for the specified connection. Use
        /// <see cref="BbsTelnetConnection.EnterBinaryMode"/> rather than constructing this directly.
        /// </summary>
        /// <param name="connection">The owning connection.</param>
        internal TelnetBinaryChannel(BbsTelnetConnection connection)
        {
            _connection = connection;
        }

        /// <inheritdoc />
        public bool DataAvailable => (_current != null && _offset < _current.Length) || _incoming.Reader.Count > 0;

        /// <summary>
        /// Queues received payload bytes for the transfer protocol. Called by the
        /// connection's read loop.
        /// </summary>
        /// <param name="payload">The de-escaped payload bytes.</param>
        internal void Post(ReadOnlySpan<byte> payload)
        {
            if (!payload.IsEmpty)
            {
                _incoming.Writer.TryWrite(payload.ToArray());
            }
        }

        /// <summary>
        /// Marks the incoming stream as ended, unblocking pending reads. Called when the
        /// connection closes.
        /// </summary>
        internal void Complete()
        {
            _incoming.Writer.TryComplete();
        }

        /// <inheritdoc />
        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_current == null || _offset >= _current.Length)
            {
                try
                {
                    _current = await _incoming.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (ChannelClosedException)
                {
                    return 0;
                }

                _offset = 0;
            }

            int count = Math.Min(buffer.Length, _current.Length - _offset);
            _current.AsSpan(_offset, count).CopyTo(buffer.Span);
            _offset += count;
            return count;
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return _connection.SendBinaryAsync(buffer, cancellationToken);
        }

        /// <inheritdoc />
        public void Purge()
        {
            _current = null;
            _offset = 0;

            while (_incoming.Reader.TryRead(out _))
            {
            }
        }

        /// <summary>
        /// Ends binary mode and resumes normal terminal text delivery.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _incoming.Writer.TryComplete();
            _connection.ExitBinaryMode(this);
        }
    }
}
