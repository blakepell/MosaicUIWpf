/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Transfers
{
    /// <summary>
    /// Defines the raw byte channel a file transfer protocol runs over. Implementations
    /// must deliver an 8-bit clean stream (any lower level escaping, such as Telnet IAC
    /// doubling, is handled by the link).
    /// </summary>
    public interface ITransferLink
    {
        /// <summary>
        /// Reads available bytes into <paramref name="buffer"/>, waiting until at least one
        /// byte arrives, the link closes, or the token is canceled.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="cancellationToken">A token that aborts the read.</param>
        /// <returns>The number of bytes read, or zero when the link has closed.</returns>
        ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        /// <summary>
        /// Gets whether received bytes are waiting to be read. Used by streaming protocols
        /// to poll for receiver interrupts without blocking.
        /// </summary>
        bool DataAvailable { get; }

        /// <summary>
        /// Writes the buffer to the remote system.
        /// </summary>
        /// <param name="buffer">The bytes to send.</param>
        /// <param name="cancellationToken">A token that aborts the write.</param>
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);

        /// <summary>
        /// Discards any bytes that have been received but not yet read.
        /// </summary>
        void Purge();
    }

    /// <summary>
    /// Provides buffered, timeout-aware single byte reads over an <see cref="ITransferLink"/>.
    /// </summary>
    public sealed class TransferLinkReader
    {
        private readonly ITransferLink _link;
        private readonly byte[] _buffer = new byte[4096];
        private int _offset;
        private int _length;

        /// <summary>
        /// Initializes a reader over the specified link.
        /// </summary>
        /// <param name="link">The link to read from.</param>
        public TransferLinkReader(ITransferLink link)
        {
            _link = link;
        }

        /// <summary>
        /// Gets whether the link reported end of stream.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Gets whether a byte can be read without waiting.
        /// </summary>
        public bool DataAvailable => _offset < _length || _link.DataAvailable;

        /// <summary>
        /// Reads the next byte, waiting up to <paramref name="timeout"/> for data.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for a byte.</param>
        /// <param name="cancellationToken">A token that aborts the transfer entirely.</param>
        /// <returns>The next byte, or -1 when the timeout elapsed or the link closed.</returns>
        /// <exception cref="OperationCanceledException">The transfer was canceled.</exception>
        public async ValueTask<int> ReadByteAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (_offset < _length)
            {
                return _buffer[_offset++];
            }

            if (IsClosed)
            {
                return -1;
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            try
            {
                int count = await _link.ReadAsync(_buffer, timeoutSource.Token).ConfigureAwait(false);

                if (count == 0)
                {
                    IsClosed = true;
                    return -1;
                }

                _offset = 1;
                _length = count;
                return _buffer[0];
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Only the per-read timeout fired; report it as no data rather than a cancel.
                return -1;
            }
        }

        /// <summary>
        /// Discards buffered bytes here and in the underlying link.
        /// </summary>
        public void Purge()
        {
            _offset = 0;
            _length = 0;
            _link.Purge();
        }
    }
}
