/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text;

namespace BbsNavigator.Common;

/// <summary>
/// Collects complete logical lines across reads while removing streamed ANSI control sequences.
/// </summary>
/// <remarks>
/// Treats CR, LF, CRLF, and LFCR as line endings. Oversized lines are discarded in their entirety,
/// preventing both unbounded memory use and matches against misleading truncated text.
/// This buffer is owned by the terminal's UI dispatcher.
/// </remarks>
internal sealed class TriggerLineBuffer
{
    private const int MaximumLineLength = 65536;
    private readonly StringBuilder _line = new();
    private int _escapeState;
    private char _lastNewline;
    private bool _overflow;

    /// <summary>
    /// Clears partial text and escape state when a session starts or trigger processing is disabled.
    /// </summary>
    internal void Reset()
    {
        _line.Clear();
        _escapeState = 0;
        _lastNewline = '\0';
        _overflow = false;
    }

    /// <summary>
    /// Consumes a chunk and synchronously reports each completed line without its terminator.
    /// </summary>
    /// <param name="data">The received terminal text.</param>
    /// <param name="onLine">The callback for complete lines.</param>
    internal void Append(string data, Action<string> onLine)
    {
        foreach (char ch in data)
        {
            switch (_escapeState)
            {
                case 1:
                    _escapeState = ch == '[' ? 2 : ch is ']' or 'P' or '^' or '_' ? 3 : 0;
                    continue;
                case 2:
                    if (ch is >= '@' and <= '~') _escapeState = 0;
                    continue;
                case 3:
                    if (ch == '\a') _escapeState = 0;
                    else if (ch == '\x1b') _escapeState = 4;
                    continue;
                case 4:
                    _escapeState = ch == '\\' ? 0 : ch == '\x1b' ? 4 : 3;
                    continue;
            }

            if (ch == '\x1b')
            {
                _escapeState = 1;
                continue;
            }

            if (ch is '\r' or '\n')
            {
                if (_lastNewline != '\0' && _lastNewline != ch)
                {
                    _lastNewline = '\0';
                    continue;
                }

                _lastNewline = ch;
                if (!_overflow) onLine(_line.ToString());
                _line.Clear();
                _overflow = false;
                continue;
            }

            if (ch == '\0') continue; // Telnet CR NUL represents a single CR.
            _lastNewline = '\0';
            if (_overflow) continue;
            if (ch == '\b')
            {
                if (_line.Length > 0) _line.Length--;
            }
            else if (!char.IsControl(ch) || ch == '\t')
            {
                if (_line.Length == MaximumLineLength)
                {
                    _line.Clear();
                    _overflow = true;
                }
                else _line.Append(ch);
            }
        }
    }
}
