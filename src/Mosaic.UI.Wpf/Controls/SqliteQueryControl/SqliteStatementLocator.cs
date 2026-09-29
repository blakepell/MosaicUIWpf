/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Splits a SQLite script into its individual statements and finds the one under the caret.
    /// </summary>
    /// <remarks>
    /// A statement ends at a semicolon or at a blank line, so a script whose statements are only
    /// separated by blank lines still splits the way it reads. Semicolons and blank lines inside
    /// string literals, quoted identifiers and comments are ignored, as are those inside the
    /// <c>BEGIN</c>…<c>END</c> body of a <c>CREATE TRIGGER</c>.
    /// </remarks>
    internal static class SqliteStatementLocator
    {
        /// <summary>
        /// Finds the statement that sits on the caret's line.
        /// </summary>
        /// <param name="text">The full script.</param>
        /// <param name="caretOffset">The caret's offset into <paramref name="text"/>.</param>
        /// <returns>The statement's range, or <c>null</c> when the caret's line holds no statement.</returns>
        public static (int Start, int Length)? FindStatementAt(string text, int caretOffset)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            caretOffset = Math.Clamp(caretOffset, 0, text.Length);

            int lineStart = caretOffset == 0 ? 0 : text.LastIndexOf('\n', caretOffset - 1) + 1;
            int lineEnd = text.IndexOf('\n', caretOffset);

            if (lineEnd < 0)
            {
                lineEnd = text.Length;
            }

            (int Start, int End)? match = null;

            foreach (var statement in Split(text))
            {
                if (statement.End <= lineStart || statement.Start > lineEnd)
                {
                    continue;
                }

                // More than one statement can share a line, so prefer the one the caret is inside
                // of, then the last one that starts before it.
                if (match == null || statement.Start <= caretOffset)
                {
                    match = statement;
                }

                if (statement.Start <= caretOffset && caretOffset <= statement.End)
                {
                    break;
                }
            }

            return match is { } m ? (m.Start, m.End - m.Start) : null;
        }

        /// <summary>
        /// Splits a script into statement ranges, trimmed of surrounding whitespace. Ranges that
        /// hold nothing but comments are dropped.
        /// </summary>
        /// <param name="text">The full script.</param>
        /// <returns>The start (inclusive) and end (exclusive) offset of each statement.</returns>
        public static List<(int Start, int End)> Split(string text)
        {
            var statements = new List<(int Start, int End)>();

            int segmentStart = 0;
            bool segmentHasCode = false;
            bool lineHasContent = false;
            int lineStart = 0;

            // CREATE TRIGGER bodies contain semicolons that do not end the statement.
            string? firstWord = null;
            bool isTrigger = false;
            int blockDepth = 0;

            void EndSegment(int end)
            {
                if (segmentHasCode)
                {
                    int start = segmentStart;

                    while (start < end && char.IsWhiteSpace(text[start]))
                    {
                        start++;
                    }

                    while (end > start && char.IsWhiteSpace(text[end - 1]))
                    {
                        end--;
                    }

                    if (end > start)
                    {
                        statements.Add((start, end));
                    }
                }

                segmentHasCode = false;
                firstWord = null;
                isTrigger = false;
                blockDepth = 0;
            }

            int i = 0;

            while (i < text.Length)
            {
                char c = text[i];

                if (c == '\n')
                {
                    if (!lineHasContent && blockDepth == 0)
                    {
                        EndSegment(lineStart);
                        segmentStart = i + 1;
                    }

                    lineHasContent = false;
                    lineStart = i + 1;
                    i++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                lineHasContent = true;

                if (c == '-' && i + 1 < text.Length && text[i + 1] == '-')
                {
                    int newline = text.IndexOf('\n', i);
                    i = newline < 0 ? text.Length : newline;
                    continue;
                }

                if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = SkipTo(text, i, close < 0 ? text.Length : close + 2, ref lineStart);
                    continue;
                }

                segmentHasCode = true;

                if (c is '\'' or '"' or '`' or '[')
                {
                    char closeChar = c == '[' ? ']' : c;
                    int close = text.IndexOf(closeChar, i + 1);
                    i = SkipTo(text, i, close < 0 ? text.Length : close + 1, ref lineStart);
                    continue;
                }

                if (c == ';')
                {
                    if (blockDepth == 0)
                    {
                        EndSegment(i + 1);
                        segmentStart = i + 1;
                    }

                    i++;
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int wordStart = i;

                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        i++;
                    }

                    var word = text.AsSpan(wordStart, i - wordStart);

                    if (firstWord == null)
                    {
                        firstWord = word.ToString();
                    }
                    else if (!isTrigger && word.Equals("TRIGGER", StringComparison.OrdinalIgnoreCase)
                                        && firstWord.Equals("CREATE", StringComparison.OrdinalIgnoreCase))
                    {
                        isTrigger = true;
                    }
                    else if (isTrigger)
                    {
                        if (word.Equals("BEGIN", StringComparison.OrdinalIgnoreCase) || word.Equals("CASE", StringComparison.OrdinalIgnoreCase))
                        {
                            blockDepth++;
                        }
                        else if (word.Equals("END", StringComparison.OrdinalIgnoreCase) && blockDepth > 0)
                        {
                            blockDepth--;
                        }
                    }

                    continue;
                }

                i++;
            }

            EndSegment(text.Length);

            return statements;
        }

        /// <summary>
        /// Advances past a literal or comment, keeping the line tracking in step with any newlines
        /// it spans.
        /// </summary>
        /// <param name="text">The full script.</param>
        /// <param name="from">The offset the literal starts at.</param>
        /// <param name="to">The offset just past the literal.</param>
        /// <param name="lineStart">The start of the current line, updated when the literal spans lines.</param>
        /// <returns><paramref name="to"/>.</returns>
        private static int SkipTo(string text, int from, int to, ref int lineStart)
        {
            int lastNewline = text.LastIndexOf('\n', to - 1, to - from);

            if (lastNewline >= 0)
            {
                lineStart = lastNewline + 1;
            }

            return to;
        }
    }
}
