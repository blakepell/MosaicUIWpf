/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Threading.Channels;

namespace BbsNavigator.Networking
{
    /// <summary>
    /// Buffers the payload bytes a connection's read loop produces and serves them to a file
    /// transfer protocol. Transports supply their own write path and teardown.
    /// </summary>
    public abstract class QueuedBinaryChannel : IBbsBinaryChannel
    {
        private readonly Channel<byte[]> _incoming = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        private byte[]? _current;
        private int _offset;
        private bool _disposed;

        /// <inheritdoc />
        public bool DataAvailable => (_current != null && _offset < _current.Length) || _incoming.Reader.Count > 0;

        /// <summary>
        /// Queues received payload bytes for the transfer protocol. Called by the
        /// connection's read loop.
        /// </summary>
        /// <param name="payload">The payload bytes, with any transport escaping already removed.</param>
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
        public abstract ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

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
        /// Releases the byte stream back to the connection so terminal text delivery resumes.
        /// </summary>
        protected abstract void ReleaseConnection();

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
            ReleaseConnection();
            GC.SuppressFinalize(this);
        }
    }
}
