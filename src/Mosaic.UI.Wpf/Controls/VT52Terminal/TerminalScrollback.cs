/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// Fixed-capacity circular buffer of scrolled-off terminal lines. Adding past the
    /// capacity overwrites (and recycles) the oldest line so memory stays bounded.
    /// </summary>
    internal sealed class TerminalScrollback
    {
        private TerminalCell[]?[] _lines;
        private int _head;
        private int _count;
        private int _capacity;

        public TerminalScrollback(int capacity)
        {
            _capacity = Math.Max(0, capacity);
            _lines = new TerminalCell[Math.Max(1, _capacity)][];
        }

        /// <summary>Number of lines currently stored.</summary>
        public int Count => _count;

        /// <summary>Maximum number of lines that can be stored.</summary>
        public int Capacity => _capacity;

        /// <summary>Gets the line at the supplied logical index (0 = oldest).</summary>
        public TerminalCell[] this[int index] => _lines[(_head + index) % _lines.Length]!;

        /// <summary>
        /// Appends a line. When the buffer is at capacity the oldest line is evicted and
        /// returned so its backing array can be reused, avoiding per-line allocations.
        /// </summary>
        public TerminalCell[]? AddAndRecycle(TerminalCell[] line)
        {
            if (_capacity == 0)
            {
                return line;
            }

            if (_count < _capacity)
            {
                _lines[(_head + _count) % _lines.Length] = line;
                _count++;
                return null;
            }

            var evicted = _lines[_head];
            _lines[_head] = line;
            _head = (_head + 1) % _lines.Length;
            return evicted;
        }

        /// <summary>Removes all stored lines.</summary>
        public void Clear()
        {
            Array.Clear(_lines, 0, _lines.Length);
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Resizes the buffer, preserving the most recent lines (oldest are dropped when shrinking).
        /// </summary>
        public void SetCapacity(int capacity)
        {
            capacity = Math.Max(0, capacity);

            if (capacity == _capacity)
            {
                return;
            }

            int keep = Math.Min(_count, capacity);
            var newLines = new TerminalCell[Math.Max(1, capacity)][];

            for (int i = 0; i < keep; i++)
            {
                newLines[i] = this[_count - keep + i];
            }

            _lines = newLines;
            _head = 0;
            _count = keep;
            _capacity = capacity;
        }
    }
}
