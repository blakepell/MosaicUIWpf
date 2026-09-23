/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.AvalonDock.Layout;
using Mosaic.UI.Wpf.Controls.VT52Terminal;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace BbsNavigator.Views
{
    /// <summary>
    /// A connectionless ANSI terminal that scripts and the host write into, used for info panels
    /// that sit beside a BBS session as a document or a tool window.
    /// </summary>
    /// <remarks>
    /// Every member is safe to call from a script's worker thread; calls are marshalled to the UI
    /// thread. Rows and columns are 1-based, matching ANSI cursor addressing. Text written through
    /// these methods may contain ANSI escape sequences, and a lone <c>\n</c> is treated as a new line.
    /// </remarks>
    public partial class TerminalPanel : UserControl
    {
        private const string Esc = "\u001b[";
        private bool _isEditable;
        private LayoutContent? _host;

        /// <summary>
        /// Initializes a panel with the given identifier.
        /// </summary>
        /// <param name="panelId">The key the host uses to find this panel again.</param>
        public TerminalPanel(string panelId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(panelId);
            PanelId = panelId;
            InitializeComponent();
            PanelTerminal.PreviewKeyDown += OnTerminalPreviewKeyDown;
            PanelTerminal.PreviewTextInput += OnTerminalPreviewTextInput;
        }

        /// <summary>
        /// Raised on the UI thread after the document or tool window hosting this panel closes.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// Gets the key the host uses to find this panel again.
        /// </summary>
        public string PanelId { get; }

        /// <summary>
        /// Gets the terminal that renders the panel, for writing raw data or changing its appearance.
        /// </summary>
        public VT52Terminal Terminal => PanelTerminal;

        /// <summary>
        /// Gets whether the panel is hosted as a tool window rather than a document.
        /// </summary>
        public bool IsToolWindow => Invoke(() => _host is LayoutAnchorable);

        /// <summary>
        /// Gets or sets the title of the hosting document or tool window.
        /// </summary>
        public string Title
        {
            get => Invoke(() => _host?.Title ?? string.Empty);
            set => Invoke(() =>
            {
                if (_host != null)
                {
                    _host.Title = value;
                }
            });
        }

        /// <summary>
        /// Gets or sets whether the user can type into the panel. Typed text is written at the
        /// cursor; arrows move it, and Backspace/Delete remove characters.
        /// </summary>
        public bool IsEditable
        {
            get => Invoke(() => _isEditable);
            set => Invoke(() => _isEditable = value);
        }

        /// <summary>
        /// Gets the number of visible rows.
        /// </summary>
        public int Rows => Invoke(() => PanelTerminal.Rows);

        /// <summary>
        /// Gets the number of visible columns.
        /// </summary>
        public int Columns => Invoke(() => PanelTerminal.Columns);

        /// <summary>
        /// Clears the panel and writes <paramref name="text"/> from the top-left corner.
        /// </summary>
        /// <param name="text">The text to show.</param>
        public void SetText(string? text) => Invoke(() =>
        {
            ResetTerminal();
            PanelTerminal.Add(Normalize(text));
        });

        /// <summary>
        /// Writes <paramref name="text"/> at the cursor.
        /// </summary>
        /// <param name="text">The text to write.</param>
        public new void AddText(string? text) => Invoke(() => PanelTerminal.Add(Normalize(text)));

        /// <summary>
        /// Writes <paramref name="text"/> at the cursor followed by a new line.
        /// </summary>
        /// <param name="text">The text to write.</param>
        public void AddLine(string? text) => Invoke(() => PanelTerminal.Add(Normalize(text) + "\r\n"));

        /// <summary>
        /// Writes a new line at the cursor.
        /// </summary>
        public void AddLine() => AddLine(string.Empty);

        /// <summary>
        /// Writes <paramref name="text"/> starting at a position, leaving the cursor after it.
        /// Text past the right edge wraps.
        /// </summary>
        /// <param name="row">The 1-based row.</param>
        /// <param name="column">The 1-based column.</param>
        /// <param name="text">The text to write.</param>
        public void WriteAt(int row, int column, string? text) => Invoke(() => PanelTerminal.Add(CursorTo(row, column) + Normalize(text)));

        /// <summary>
        /// Replaces the contents of a row with <paramref name="text"/>.
        /// </summary>
        /// <param name="row">The 1-based row.</param>
        /// <param name="text">The text to write.</param>
        public void SetLine(int row, string? text) => Invoke(() => PanelTerminal.Add(CursorTo(row, 1) + Esc + "2K" + Normalize(text)));

        /// <summary>
        /// Blanks a row and moves the cursor to its start.
        /// </summary>
        /// <param name="row">The 1-based row.</param>
        public void ClearLine(int row) => SetLine(row, string.Empty);

        /// <summary>
        /// Clears the panel, its scrollback, and any colors, and homes the cursor.
        /// </summary>
        public void Clear() => Invoke(ResetTerminal);

        /// <summary>
        /// Moves the cursor.
        /// </summary>
        /// <param name="row">The 1-based row.</param>
        /// <param name="column">The 1-based column.</param>
        public void MoveCursor(int row, int column) => Invoke(() => PanelTerminal.Add(CursorTo(row, column)));

        /// <summary>
        /// Sets the foreground color for text written afterwards.
        /// </summary>
        /// <param name="foreground">A palette index: 0-7 normal, 8-15 bright, 16-255 extended.</param>
        public void SetColor(int foreground) => Invoke(() => PanelTerminal.Add(Esc + ColorCode(foreground, background: false) + "m"));

        /// <summary>
        /// Sets the foreground and background colors for text written afterwards.
        /// </summary>
        /// <param name="foreground">A palette index: 0-7 normal, 8-15 bright, 16-255 extended.</param>
        /// <param name="background">A palette index: 0-7 normal, 8-15 bright, 16-255 extended.</param>
        public void SetColor(int foreground, int background) => Invoke(() =>
            PanelTerminal.Add(Esc + ColorCode(foreground, background: false) + ";" + ColorCode(background, background: true) + "m"));

        /// <summary>
        /// Restores the default colors and attributes for text written afterwards.
        /// </summary>
        public void ResetColor() => Invoke(() => PanelTerminal.Add(Esc + "0m"));

        /// <summary>
        /// Fixes the panel to a size instead of following the size of its window.
        /// </summary>
        /// <param name="rows">The number of rows.</param>
        /// <param name="columns">The number of columns.</param>
        public void Resize(int rows, int columns) => Invoke(() =>
        {
            PanelTerminal.AutoResizeTerminal = false;
            PanelTerminal.Resize(rows, columns);
        });

        /// <summary>
        /// Gets the text of a visible row without trailing spaces.
        /// </summary>
        /// <param name="row">The 1-based row.</param>
        /// <returns>The row's text, or an empty string when the row is outside the screen.</returns>
        public string GetLine(int row) => Invoke(() =>
        {
            if (row < 1 || row > PanelTerminal.Rows)
            {
                return string.Empty;
            }

            var document = PanelTerminal.Document;
            int lineNumber = document.LineCount - PanelTerminal.Rows + row;
            return lineNumber < 1 ? string.Empty : document.GetText(document.GetLineByNumber(lineNumber)).TrimEnd();
        });

        /// <summary>
        /// Gets the visible screen as text, without trailing spaces or trailing blank rows.
        /// </summary>
        /// <returns>The screen's text with rows separated by <see cref="Environment.NewLine"/>.</returns>
        public string GetText() => Invoke(() =>
        {
            var lines = new List<string>(PanelTerminal.Rows);
            for (int row = 1; row <= PanelTerminal.Rows; row++)
            {
                lines.Add(GetLine(row));
            }

            int count = lines.Count;
            while (count > 0 && lines[count - 1].Length == 0)
            {
                count--;
            }

            return string.Join(Environment.NewLine, lines.Take(count));
        });

        /// <summary>
        /// Closes the document or tool window hosting this panel.
        /// </summary>
        public void Close() => Invoke(() => _host?.Close());

        /// <summary>
        /// Brings the hosting document or tool window forward.
        /// </summary>
        public void Activate() => Invoke(() =>
        {
            if (_host is LayoutAnchorable { IsHidden: true } anchorable)
            {
                anchorable.Show();
            }

            if (_host != null)
            {
                _host.IsActive = true;
            }
        });

        /// <summary>
        /// Associates the panel with the layout item that hosts it so title and close requests reach it.
        /// </summary>
        /// <param name="host">The hosting document or tool window.</param>
        internal void AttachHost(LayoutContent host)
        {
            _host = host;
            host.Closed += OnHostClosed;
        }

        private void OnHostClosed(object? sender, EventArgs e)
        {
            if (_host != null)
            {
                _host.Closed -= OnHostClosed;
                _host = null;
            }

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void ResetTerminal() => PanelTerminal.Reset(PanelTerminal.Rows, PanelTerminal.Columns);

        private void OnTerminalPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!_isEditable || string.IsNullOrEmpty(e.Text) || e.Text.Any(char.IsControl))
            {
                return;
            }

            PanelTerminal.Add(e.Text);
            e.Handled = true;
        }

        private void OnTerminalPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isEditable || Keyboard.Modifiers is not (ModifierKeys.None or ModifierKeys.Shift))
            {
                return;
            }

            string? sequence = e.Key switch
            {
                Key.Enter => "\r\n",
                Key.Back => "\b" + Esc + "P",
                Key.Delete => Esc + "P",
                Key.Left => Esc + "D",
                Key.Right => Esc + "C",
                Key.Up => Esc + "A",
                Key.Down => Esc + "B",
                Key.Home => "\r",
                _ => null
            };

            if (sequence == null)
            {
                return;
            }

            PanelTerminal.Add(sequence);
            e.Handled = true;
        }

        private static string CursorTo(int row, int column) => $"{Esc}{Math.Max(1, row)};{Math.Max(1, column)}H";

        private static string ColorCode(int index, bool background)
        {
            index = Math.Clamp(index, 0, 255);
            int normal = background ? 40 : 30;
            int bright = background ? 100 : 90;
            return index switch
            {
                < 8 => (normal + index).ToString(),
                < 16 => (bright + index - 8).ToString(),
                _ => $"{(background ? 48 : 38)};5;{index}"
            };
        }

        /// <summary>
        /// Turns a lone line feed into CR LF so script text starts each line at the left edge.
        /// </summary>
        private static string Normalize(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (!text.Contains('\n'))
            {
                return text;
            }

            var sb = new StringBuilder(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n' && (i == 0 || text[i - 1] != '\r'))
                {
                    sb.Append('\r');
                }

                sb.Append(text[i]);
            }

            return sb.ToString();
        }

        private void Invoke(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.Invoke(action);
            }
        }

        private T Invoke<T>(Func<T> func) => Dispatcher.CheckAccess() ? func() : Dispatcher.Invoke(func);
    }
}
