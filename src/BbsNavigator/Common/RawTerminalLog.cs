/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace BbsNavigator.Common
{
    /// <summary>
    /// A bounded, thread-safe ring buffer that keeps the most recent raw terminal bytes so a
    /// session can be re-decoded and redrawn when the text encoding changes mid-connection.
    /// </summary>
    /// <remarks>
    /// Terminal text reaches the view already decoded, and the escape sequences that produced the
    /// current colors and cursor moves are consumed by the emulator, so the only way to honor a
    /// new encoding without reconnecting is to replay the original bytes.
    /// </remarks>
    public sealed class RawTerminalLog
    {
        private readonly object _gate = new();
        private readonly byte[] _buffer;
        private int _start;
        private int _length;

        /// <summary>
        /// Initializes a log that retains at most <paramref name="capacity"/> bytes.
        /// </summary>
        /// <param name="capacity">The maximum number of bytes to retain.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
        public RawTerminalLog(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
            _buffer = new byte[capacity];
        }

        /// <summary>
        /// Appends bytes, discarding the oldest data once the capacity is reached.
        /// </summary>
        /// <param name="data">The bytes received from the remote system.</param>
        public void Append(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            lock (_gate)
            {
                // Anything beyond the capacity would be immediately overwritten; keep only the tail.
                if (data.Length >= _buffer.Length)
                {
                    data[^_buffer.Length..].CopyTo(_buffer);
                    _start = 0;
                    _length = _buffer.Length;
                    return;
                }

                int writeAt = (_start + _length) % _buffer.Length;
                int firstRun = Math.Min(data.Length, _buffer.Length - writeAt);
                data[..firstRun].CopyTo(_buffer.AsSpan(writeAt));

                if (firstRun < data.Length)
                {
                    data[firstRun..].CopyTo(_buffer);
                }

                int overflow = _length + data.Length - _buffer.Length;
                if (overflow > 0)
                {
                    _start = (_start + overflow) % _buffer.Length;
                    _length = _buffer.Length;
                }
                else
                {
                    _length += data.Length;
                }
            }
        }

        /// <summary>
        /// Returns a copy of the retained bytes in the order they were received.
        /// </summary>
        /// <returns>The retained bytes, oldest first.</returns>
        public byte[] Snapshot()
        {
            lock (_gate)
            {
                if (_length == 0)
                {
                    return [];
                }

                var copy = new byte[_length];
                int firstRun = Math.Min(_length, _buffer.Length - _start);
                _buffer.AsSpan(_start, firstRun).CopyTo(copy);

                if (firstRun < _length)
                {
                    _buffer.AsSpan(0, _length - firstRun).CopyTo(copy.AsSpan(firstRun));
                }

                return copy;
            }
        }

        /// <summary>
        /// Discards everything retained so far.
        /// </summary>
        public void Clear()
        {
            lock (_gate)
            {
                _start = 0;
                _length = 0;
            }
        }
    }
}
