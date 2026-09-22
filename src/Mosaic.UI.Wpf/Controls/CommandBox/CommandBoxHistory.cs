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
    /// Controls whether a command that has been entered before is recorded again by a
    /// <see cref="CommandBoxHistory"/>.
    /// </summary>
    public enum HistoryDuplicatePolicy
    {
        /// <summary>
        /// Every command is recorded, even when it repeats one that is already in the list.
        /// </summary>
        Allow,

        /// <summary>
        /// A command is skipped when it is identical to the most recent entry. Running the same
        /// command ten times in a row produces a single history entry. This is the default.
        /// </summary>
        SkipConsecutive,

        /// <summary>
        /// A command is skipped when it already appears anywhere in the list, so the history never
        /// contains the same command twice and existing entries keep their original position.
        /// </summary>
        SkipAll
    }

    /// <summary>
    /// The command history behind a <see cref="CommandBox"/>: an ordered list of previously entered
    /// commands plus the cursor that Up and Down walk through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type has no UI dependencies so the arrow-key semantics can be exercised without a
    /// dispatcher or an STA thread.
    /// </para>
    /// <para>
    /// The cursor normally sits past the end of <see cref="Items"/>, meaning no navigation is in
    /// progress. <see cref="MoveBack"/> walks toward older entries and <see cref="MoveForward"/>
    /// walks back toward newer ones; stepping forward past the newest entry returns the draft, the
    /// in-progress text captured by the last <see cref="ResetCursor(string?)"/> call before
    /// navigation started.
    /// </para>
    /// </remarks>
    public sealed class CommandBoxHistory
    {
        /// <summary>
        /// The recorded commands, oldest first.
        /// </summary>
        private readonly List<string> _items = new();

        /// <summary>
        /// The index the next <see cref="MoveBack"/> / <see cref="MoveForward"/> works from. A value
        /// equal to the item count means the cursor is past the end and no navigation is in progress.
        /// </summary>
        private int _cursor;

        /// <summary>
        /// The in-progress text stashed before navigation started, restored when the user walks
        /// forward past the newest entry.
        /// </summary>
        private string? _draft;

        /// <summary>
        /// Backing field for <see cref="MaxItems"/>.
        /// </summary>
        private int _maxItems = 100;

        /// <summary>
        /// The recorded commands, oldest first. The newest command is the last element.
        /// </summary>
        public IReadOnlyList<string> Items => _items;

        /// <summary>
        /// The number of recorded commands.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// The maximum number of commands to retain. When the list grows past this value the oldest
        /// entries are evicted. A value of zero (or less) means unlimited. Defaults to 100.
        /// </summary>
        public int MaxItems
        {
            get => _maxItems;
            set
            {
                _maxItems = value;
                this.Trim();
            }
        }

        /// <summary>
        /// Whether a command that repeats an existing entry is recorded again. Defaults to
        /// <see cref="HistoryDuplicatePolicy.SkipConsecutive"/>.
        /// </summary>
        public HistoryDuplicatePolicy DuplicatePolicy { get; set; } = HistoryDuplicatePolicy.SkipConsecutive;

        /// <summary>
        /// Whether the cursor is currently inside the list, which is the case while the user is
        /// walking the history with the arrow keys.
        /// </summary>
        public bool IsNavigating => _cursor < _items.Count;

        /// <summary>
        /// Records a command and resets the cursor back to past-the-end.
        /// </summary>
        /// <param name="command">
        /// The command to record. It is trimmed, and is ignored entirely when it is
        /// <see langword="null"/>, empty or whitespace.
        /// </param>
        /// <remarks>
        /// The cursor is reset whether or not the command was actually recorded, so the next
        /// <see cref="MoveBack"/> always starts from the newest entry.
        /// </remarks>
        public void Add(string? command)
        {
            string trimmed = command?.Trim() ?? string.Empty;

            if (trimmed.Length == 0)
            {
                this.ResetCursor();
                return;
            }

            bool skip = this.DuplicatePolicy switch
            {
                HistoryDuplicatePolicy.SkipConsecutive => _items.Count > 0 && string.Equals(_items[^1], trimmed, StringComparison.Ordinal),
                HistoryDuplicatePolicy.SkipAll => _items.Contains(trimmed, StringComparer.Ordinal),
                _ => false
            };

            if (!skip)
            {
                _items.Add(trimmed);
                this.Trim();
            }

            this.ResetCursor();
        }

        /// <summary>
        /// Appends a set of previously persisted commands, honoring <see cref="DuplicatePolicy"/>
        /// and <see cref="MaxItems"/>, then resets the cursor.
        /// </summary>
        /// <param name="commands">The commands to import, oldest first.</param>
        public void Import(IEnumerable<string>? commands)
        {
            if (commands == null)
            {
                return;
            }

            foreach (string command in commands)
            {
                this.Add(command);
            }
        }

        /// <summary>
        /// Steps the cursor one entry toward the oldest command.
        /// </summary>
        /// <returns>
        /// The command now under the cursor, or <see langword="null"/> when the history is empty or
        /// the cursor is already sitting on the oldest entry.
        /// </returns>
        public string? MoveBack()
        {
            if (_cursor <= 0)
            {
                return null;
            }

            _cursor--;
            return _items[_cursor];
        }

        /// <summary>
        /// Steps the cursor one entry toward the newest command.
        /// </summary>
        /// <returns>
        /// The command now under the cursor; the draft text (or an empty string when there is none)
        /// once the cursor walks past the newest entry; or <see langword="null"/> when the cursor is
        /// already past the end and there is nothing left to restore.
        /// </returns>
        public string? MoveForward()
        {
            if (_cursor >= _items.Count)
            {
                return null;
            }

            _cursor++;

            return _cursor >= _items.Count ? _draft ?? string.Empty : _items[_cursor];
        }

        /// <summary>
        /// Moves the cursor back to past-the-end and replaces the stashed draft.
        /// </summary>
        /// <param name="draft">
        /// The in-progress text to restore when the user later walks forward past the newest entry.
        /// Pass <see langword="null"/> to forget the previous draft.
        /// </param>
        /// <remarks>
        /// A <see cref="CommandBox"/> calls this every time the user types, so the draft always
        /// reflects what was on screen at the moment history navigation began.
        /// </remarks>
        public void ResetCursor(string? draft = null)
        {
            _cursor = _items.Count;
            _draft = draft;
        }

        /// <summary>
        /// Removes every recorded command, clears the draft and resets the cursor.
        /// </summary>
        public void Clear()
        {
            _items.Clear();
            this.ResetCursor();
        }

        /// <summary>
        /// Evicts the oldest entries until the list fits inside <see cref="MaxItems"/>.
        /// </summary>
        private void Trim()
        {
            if (_maxItems <= 0)
            {
                return;
            }

            int excess = _items.Count - _maxItems;

            if (excess > 0)
            {
                _items.RemoveRange(0, excess);
            }

            if (_cursor > _items.Count)
            {
                _cursor = _items.Count;
            }
        }
    }
}
