/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// VT52 terminal emulator hosted inside an AvalonEdit TextEditor.
    /// Call Add(string) or Add(byte[]) with remote data; call SendKey/SendString for local keystrokes.
    /// Subscribe to Transmit to get bytes that the terminal sends back (e.g., Identify response).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class VT52Terminal : TextEditor
    {
        #region Context menu commands

        /// <summary>
        /// Copies the selected terminal text to the clipboard.
        /// </summary>
        public static readonly RoutedUICommand CopyTextCommand = new(
            "Copy", nameof(CopyTextCommand), typeof(VT52Terminal));

        /// <summary>
        /// Represents the command that pastes text into the VT52 terminal control.
        /// </summary>
        /// <remarks>This command can be used to bind paste functionality to UI elements, enabling users
        /// to insert clipboard text into the terminal. It is typically used in command bindings or input gestures
        /// within WPF applications.</remarks>
        public static readonly RoutedUICommand PasteTextCommand = new(
            "Paste", nameof(PasteTextCommand), typeof(VT52Terminal));

        /// <summary>
        /// Clears the terminal screen and resets the internal buffer.
        /// </summary>
        public static readonly RoutedUICommand ClearTerminalCommand = new(
            "Clear Terminal", nameof(ClearTerminalCommand), typeof(VT52Terminal));

        private int _curRow;

        #endregion
        internal int BufferRows => Rows;

        /// <summary>
        /// Gets the number of rows.
        /// </summary>
        public int Rows { get; private set; }

        private int _curCol;

        internal int BufferColumns => Columns;

        /// <summary>
        /// Gets the number of columns.
        /// </summary>
        public int Columns { get; private set; }

        // Expose buffer for colorizer
        internal TerminalCell[,] Buffer => _buf;

        // Number of scrollback lines currently shown ahead of the active screen.
        // The alternate screen buffer never shows scrollback.
        internal int VisibleScrollbackCount => _useAltBuffer ? 0 : _scrollback.Count;

        // Total number of document lines (scrollback + active screen).
        internal int TotalRows => VisibleScrollbackCount + Rows;

        /// <summary>
        /// Returns the attributes for a cell addressed by document line (0-based, scrollback first)
        /// and column. Out-of-range requests return default attributes.
        /// </summary>
        internal TerminalAttributes GetCellAttributes(int documentLine, int col)
        {
            int sbCount = VisibleScrollbackCount;

            if (documentLine < sbCount)
            {
                var row = _scrollback[documentLine];
                return col >= 0 && col < row.Length ? row[col].Attrs : TerminalAttributes.Default;
            }

            int screenRow = documentLine - sbCount;

            if (screenRow >= 0 && screenRow < Rows && col >= 0 && col < Columns &&
                screenRow < _buf.GetLength(0) && col < _buf.GetLength(1))
            {
                return _buf[screenRow, col].Attrs;
            }

            return TerminalAttributes.Default;
        }

        // Screen geometry & buffer
        private TerminalCell[,] _buf = new TerminalCell[1, 1];

        // Current text attributes
        private TerminalAttributes _currentAttrs = TerminalAttributes.Default;

        // Parser state
        private enum ParseState
        {
            Normal,
            Esc,
            EscYRow,
            EscYCol,
            Csi,
            OscString, // Operating System Command (title, etc.)
            EscScs // Select Character Set - consumes one more byte (e.g., ESC ( B)
        }

        private ParseState _state = ParseState.Normal;
        private int _escYRowTmp = 0; // 1-based tmp
        private readonly List<int> _csiParams = new();
        private int _csiCurrentParam = 0;
        private bool _csiHasParam = false;
        private char _csiPrivateMarker = '\0'; // For ? or > markers
        private readonly StringBuilder _oscBuffer = new();

        // Scroll region (1-based, inclusive)
        private int _scrollTop = 1;
        private int _scrollBottom;

        // Alternate screen buffer
        private TerminalCell[,]? _altBuf;
        private int _altCurRow, _altCurCol;
        private TerminalAttributes _altAttrs;
        private int _savedCurRow, _savedCurCol;
        private bool _useAltBuffer = false;

        // Scrollback history (main screen only). Lines that scroll off the top of a
        // full-screen scroll region are captured here so the user can scroll up to review them.
        private TerminalScrollback _scrollback = new(1000);
        private TerminalCell[]? _recycledLine;
        private bool _scrollbackDirty;

        // Modes
        private bool _originMode = false; // DECOM - cursor relative to scroll region
        private bool _autowrapPending = false; // Deferred autowrap
        private bool _autoWrap = true; // DECAWM - automatic wrap at right margin
        private bool _applicationCursorKeys = false; // DECCKM - cursor keys send SS3 sequences
        private bool _applicationKeypad = false; // DECKPAM/DECKPNM - keypad application mode
        private bool _cursorVisible = true; // DECTCEM - text cursor enable
        private bool _insertMode = false; // IRM - insert vs replace
        private bool _newLineMode = false; // LNM - LF also performs CR
        private bool _bracketedPaste = false; // DEC 2004 - bracketed paste mode

        // Saved cursor state (DECSC / DECRC, CSI s / CSI u).
        private TerminalAttributes _savedAttrs = TerminalAttributes.Default;
        private bool _savedOriginMode;
        private bool _hasSavedCursor;

        // Horizontal tab stops (true = stop set at that column).
        private bool[] _tabStops = [];

        // Character sets (DEC line-drawing support). G0/G1 are designated via ESC ( / ESC )
        // and selected into GL via SI (G0) / SO (G1).
        private enum CharSet
        {
            Ascii,
            DecSpecialGraphics
        }

        private CharSet _g0Charset = CharSet.Ascii;
        private CharSet _g1Charset = CharSet.Ascii;
        private bool _shiftOut = false; // false = G0 in GL (SI), true = G1 in GL (SO)
        private char _scsTarget = '('; // which G set the next designator byte applies to
        private char _lastGraphicChar = '\0'; // last printed glyph, used by REP (CSI b)
        private BlockCaretRenderer? _caretRenderer;

        private CharSet ActiveCharset => _shiftOut ? _g1Charset : _g0Charset;

        private readonly Lock _lock = new();
        private readonly SemaphoreSlim _sendGate = new(1, 1);
        private readonly StringBuilder _pendingRemoteInput = new();

        // Stateful UTF-8 decoder for Add(byte[]). Keeping a single decoder instance lets
        // multi-byte sequences that are split across separate Add calls (network packets)
        // be reassembled correctly. Invalid byte sequences are emitted as U+FFFD.
        private readonly Decoder _utf8Decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false).GetDecoder();
        private char[] _decodeBuffer = new char[1024];
        private string[] _lineCache = [];
        private bool _isRemoteInputFlushQueued;
        private bool _isLoaded;

        /// <summary>
        /// Identifies the <see cref="Connection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            nameof(Connection),
            typeof(ITerminalConnection),
            typeof(VT52Terminal),
            new PropertyMetadata(null, OnConnectionChanged));

        /// <summary>
        /// Identifies the <see cref="AutoConnect"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoConnectProperty = DependencyProperty.Register(
            nameof(AutoConnect),
            typeof(bool),
            typeof(VT52Terminal),
            new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="DisconnectOnUnload"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisconnectOnUnloadProperty = DependencyProperty.Register(
            nameof(DisconnectOnUnload),
            typeof(bool),
            typeof(VT52Terminal),
            new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="SendKeyboardInputToConnection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SendKeyboardInputToConnectionProperty = DependencyProperty.Register(
            nameof(SendKeyboardInputToConnection),
            typeof(bool),
            typeof(VT52Terminal),
            new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="LocalEcho"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LocalEchoProperty = DependencyProperty.Register(
            nameof(LocalEcho),
            typeof(bool),
            typeof(VT52Terminal),
            new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="BackspaceSendsDelete"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackspaceSendsDeleteProperty = DependencyProperty.Register(
            nameof(BackspaceSendsDelete),
            typeof(bool),
            typeof(VT52Terminal),
            new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="MaxScrollbackLines"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxScrollbackLinesProperty = DependencyProperty.Register(
            nameof(MaxScrollbackLines),
            typeof(int),
            typeof(VT52Terminal),
            new PropertyMetadata(1000, OnMaxScrollbackLinesChanged));

        /// <summary>
        /// Gets or sets the terminal connection used for remote input and output.
        /// </summary>
        public ITerminalConnection? Connection
        {
            get => (ITerminalConnection?)GetValue(ConnectionProperty);
            set => SetValue(ConnectionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the terminal connects automatically when loaded.
        /// </summary>
        public bool AutoConnect
        {
            get => (bool)GetValue(AutoConnectProperty);
            set => SetValue(AutoConnectProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the connection is disconnected when the control unloads.
        /// </summary>
        public bool DisconnectOnUnload
        {
            get => (bool)GetValue(DisconnectOnUnloadProperty);
            set => SetValue(DisconnectOnUnloadProperty, value);
        }

        /// <summary>
        /// Gets or sets whether keyboard input is sent to <see cref="Connection"/>.
        /// </summary>
        public bool SendKeyboardInputToConnection
        {
            get => (bool)GetValue(SendKeyboardInputToConnectionProperty);
            set => SetValue(SendKeyboardInputToConnectionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether locally typed input is rendered immediately after it is sent.
        /// </summary>
        public bool LocalEcho
        {
            get => (bool)GetValue(LocalEchoProperty);
            set => SetValue(LocalEchoProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether Backspace sends DEL (<c>0x7F</c>)
        /// instead of BS (<c>0x08</c>).
        /// </summary>
        /// <value>
        /// <see langword="true"/> to send DEL; otherwise, <see langword="false"/> to send BS.
        /// The default is <see langword="true"/>.
        /// </value>
        public bool BackspaceSendsDelete
        {
            get => (bool)GetValue(BackspaceSendsDeleteProperty);
            set => SetValue(BackspaceSendsDeleteProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of scrolled-off lines retained for scrollback.
        /// Set to 0 to disable scrollback. Defaults to 1000. Only the main screen has scrollback;
        /// the alternate screen buffer never contributes history.
        /// </summary>
        public int MaxScrollbackLines
        {
            get => (int)GetValue(MaxScrollbackLinesProperty);
            set => SetValue(MaxScrollbackLinesProperty, value);
        }

        private static void OnMaxScrollbackLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var terminal = (VT52Terminal)d;
            int value = Math.Max(0, (int)e.NewValue);

            lock (terminal._lock)
            {
                terminal._scrollback.SetCapacity(value);
                terminal.UpdateDocument(forceFullReplace: true);
            }
        }

        /// <summary>Fired when the terminal needs to transmit bytes back to the host (e.g., ESC / Z identify).</summary>
        public event Action<byte[]?>? Transmit;

        /// <summary>Fired when BEL (^G) is received.</summary>
        public event Action? Bell;

        /// <summary>
        /// Fired when the remote host switches into or out of the alternate screen buffer
        /// (CSI ? 1049 h / l, CSI ? 47 h / l, CSI ? 1047 h / l).
        /// The boolean argument is <c>true</c> when entering the alternate screen and
        /// <c>false</c> when returning to the main screen.
        /// </summary>
        public event Action<bool>? AlternateScreenChanged;

        /// <summary>
        /// Raised when the remote host sets the window/icon title via an OSC sequence
        /// (OSC 0, OSC 1, or OSC 2). The string argument is the requested title text.
        /// </summary>
        public event Action<string>? TitleChanged;

        /// <summary>
        /// Gets the most recent window title requested by the remote host via an OSC sequence.
        /// </summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>
        /// Gets whether the terminal is currently displaying the alternate screen buffer.
        /// </summary>
        public bool IsAlternateScreenActive => _useAltBuffer;

        /// <summary>
        /// Raised when automatic connection plumbing catches a connection exception.
        /// </summary>
        public event EventHandler<Exception>? ConnectionError;

        public VT52Terminal()
        {
            IsReadOnly = true;
            FontFamily = new FontFamily("Consolas");
            ShowLineNumbers = true;
            Options.ConvertTabsToSpaces = false;
            Background = Brushes.Black;
            Foreground = Brushes.LightGray;
            WordWrap = false;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            TextArea.TextView.Options.EnableHyperlinks = false;
            TextArea.TextView.Options.EnableEmailHyperlinks = false;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);

            this.TextArea.TextView.BackgroundRenderers.Add(new TerminalBackgroundRenderer(this));
            this.TextArea.TextView.BackgroundRenderers.Add(_caretRenderer = new BlockCaretRenderer(this.TextArea));
            this.TextArea.TextView.LineTransformers.Add(new VT52Colorizer(this));

            Reset(24, 80);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnControlSizeChanged;
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewTextInput += OnPreviewTextInput;

            // Load the context menu from its resource dictionary.
            // Using a pack URI + new ResourceDictionary() creates a fresh instance every time,
            // so each VT52Terminal gets its own ContextMenu (same effect as x:Shared="false").
            var contextMenuDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Controls/VT52Terminal/VT52ContextMenu.xaml")
            };
            this.ContextMenu = (ContextMenu)contextMenuDict["VT52TerminalContextMenu"];

            // Wire built-in commands to their handlers on this instance.
            CommandBindings.Add(new CommandBinding(CopyTextCommand, OnCopyTextExecuted, OnCopyTextCanExecute));
            CommandBindings.Add(new CommandBinding(PasteTextCommand, OnPasteTextExecuted, OnPasteTextCanExecute));
            CommandBindings.Add(new CommandBinding(ClearTerminalCommand, OnClearTerminalExecuted));

            OnSizeChanged();
        }

        private static void OnConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var terminal = (VT52Terminal)d;
            terminal.DetachConnection(e.OldValue as ITerminalConnection);
            terminal.AttachConnection(e.NewValue as ITerminalConnection);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            // Re-attach connection events to handle load/unload cycles (e.g., tab switching).
            // The -= before += is an idempotent guard that prevents double-subscription.
            var connection = Connection;
            if (connection != null)
            {
                connection.DataReceived -= OnConnectionDataReceived;
                connection.DataReceived += OnConnectionDataReceived;
            }

            Dispatcher.BeginInvoke(OnSizeChanged, DispatcherPriority.Background);

            if (AutoConnect && connection is { IsConnected: false })
            {
                try
                {
                    await ConnectAsync();
                }
                catch
                {
                    // ConnectAsync already reports failures through ConnectionError.
                }
            }
        }

        private async void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            // Detach the DataReceived event so the connection does not hold a reference to this
            // control after it has been removed from the visual tree.
            Connection?.DataReceived -= OnConnectionDataReceived;

            if (DisconnectOnUnload && Connection?.IsConnected == true)
            {
                try
                {
                    await DisconnectAsync();
                }
                catch
                {
                    // DisconnectAsync already reports failures through ConnectionError.
                }
            }
        }

        private void OnControlSizeChanged(object sender, SizeChangedEventArgs e) => OnSizeChanged();

        private void AttachConnection(ITerminalConnection? connection)
        {
            if (connection == null)
            {
                return;
            }

            connection.DataReceived += OnConnectionDataReceived;
            ApplyCurrentTerminalSize(connection, sendWindowChange: connection.IsConnected);

            if (_isLoaded && AutoConnect && !connection.IsConnected)
            {
                _ = Dispatcher.BeginInvoke(async () =>
                {
                    try
                    {
                        await ConnectAsync();
                    }
                    catch
                    {
                        // ConnectAsync already reports failures through ConnectionError.
                    }
                }, DispatcherPriority.Background);
            }
        }

        private void DetachConnection(ITerminalConnection? connection)
        {
            if (connection == null)
            {
                return;
            }

            connection.DataReceived -= OnConnectionDataReceived;
        }

        /// <summary>
        /// Opens the assigned <see cref="Connection"/>.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            var connection = Connection ?? throw new InvalidOperationException("Connection is not set.");
            ApplyCurrentTerminalSize(connection, sendWindowChange: false);

            try
            {
                bool connected = await connection.ConnectAsync();

                if (connected)
                {
                    ApplyCurrentTerminalSize(connection, sendWindowChange: true);
                    Focus();
                }

                return connected;
            }
            catch (Exception ex)
            {
                OnConnectionError(ex);
                throw;
            }
        }

        /// <summary>
        /// Closes the assigned <see cref="Connection"/>.
        /// </summary>
        public async Task<bool> DisconnectAsync()
        {
            var connection = Connection ?? throw new InvalidOperationException("Connection is not set.");

            try
            {
                return await connection.DisconnectAsync();
            }
            catch (Exception ex)
            {
                OnConnectionError(ex);
                throw;
            }
        }

        private void OnConnectionDataReceived(object? sender, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            lock (_lock)
            {
                _pendingRemoteInput.Append(data);

                if (_isRemoteInputFlushQueued)
                {
                    return;
                }

                _isRemoteInputFlushQueued = true;
            }

            Dispatcher.BeginInvoke(FlushPendingRemoteInput, DispatcherPriority.Render);
        }

        private void FlushPendingRemoteInput()
        {
            string data;

            lock (_lock)
            {
                data = _pendingRemoteInput.ToString();
                _pendingRemoteInput.Clear();
                _isRemoteInputFlushQueued = false;

                if (data.Length == 0)
                {
                    return;
                }

                foreach (var ch in data)
                {
                    ProcessChar(ch);
                }

                // Do not reset parser state here; allow incomplete sequences to continue across calls.
                UpdateDocument();
            }
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!SendKeyboardInputToConnection || string.IsNullOrEmpty(e.Text))
            {
                return;
            }

            if (SendToConnection(e.Text, e.Text))
            {
                e.Handled = true;
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!SendKeyboardInputToConnection)
            {
                return;
            }

            string? sequence = GetKeySequence(e);

            if (sequence == null && TryGetControlKeySequence(e, out sequence))
            {
                e.Handled = SendToConnection(sequence!, null);
                return;
            }

            if (sequence != null)
            {
                e.Handled = SendToConnection(sequence, GetLocalEchoText(NormalizeKey(e)));
            }
        }

        private static Key NormalizeKey(KeyEventArgs e)
        {
            return e.Key == Key.System ? e.SystemKey : e.Key;
        }

        private string? GetKeySequence(KeyEventArgs e)
        {
            Key key = NormalizeKey(e);

            if (key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                return "\x1b[Z";
            }

            if (key >= Key.F1 && key <= Key.F12)
            {
                return GetFunctionKeySequence(key);
            }

            int mod = GetXtermModifier();

            // Cursor and editing keys honor application cursor key mode (DECCKM) and keyboard modifiers.
            switch (key)
            {
                case Key.Up:
                    return CursorKey('A', mod);
                case Key.Down:
                    return CursorKey('B', mod);
                case Key.Right:
                    return CursorKey('C', mod);
                case Key.Left:
                    return CursorKey('D', mod);
                case Key.Home:
                    return CursorKey('H', mod);
                case Key.End:
                    return CursorKey('F', mod);
            }

            return key switch
            {
                Key.Insert => EditKey(2, mod),
                Key.Delete => EditKey(3, mod),
                Key.PageUp => EditKey(5, mod),
                Key.PageDown => EditKey(6, mod),
                Key.Enter or Key.Return => _newLineMode ? "\r\n" : "\r",
                Key.Back => BackspaceSendsDelete ? "\x7f" : "\x08",
                Key.Tab => "\t",
                Key.Escape => "\x1b",
                _ => null
            };
        }

        /// <summary>
        /// Computes the xterm modifier parameter (1 + bit flags) for the current keyboard modifiers.
        /// </summary>
        private static int GetXtermModifier()
        {
            var m = Keyboard.Modifiers;
            int mod = 0;
            if ((m & ModifierKeys.Shift) == ModifierKeys.Shift) mod += 1;
            if ((m & ModifierKeys.Alt) == ModifierKeys.Alt) mod += 2;
            if ((m & ModifierKeys.Control) == ModifierKeys.Control) mod += 4;
            return mod + 1;
        }

        /// <summary>
        /// Builds a cursor-key sequence, choosing CSI/SS3 form based on application cursor key mode
        /// and inserting a modifier parameter when modifier keys are held.
        /// </summary>
        private string CursorKey(char final, int mod)
        {
            if (mod > 1)
            {
                return $"\x1b[1;{mod}{final}";
            }

            return _applicationCursorKeys ? $"\x1bO{final}" : $"\x1b[{final}";
        }

        private static string EditKey(int code, int mod)
        {
            return mod > 1 ? $"\x1b[{code};{mod}~" : $"\x1b[{code}~";
        }

        private static string? GetFunctionKeySequence(Key key)
        {
            return key switch
            {
                Key.F1 => "\x1bOP",
                Key.F2 => "\x1bOQ",
                Key.F3 => "\x1bOR",
                Key.F4 => "\x1bOS",
                Key.F5 => "\x1b[15~",
                Key.F6 => "\x1b[17~",
                Key.F7 => "\x1b[18~",
                Key.F8 => "\x1b[19~",
                Key.F9 => "\x1b[20~",
                Key.F10 => "\x1b[21~",
                Key.F11 => "\x1b[23~",
                Key.F12 => "\x1b[24~",
                _ => null
            };
        }

        private static bool TryGetControlKeySequence(KeyEventArgs e, out string? sequence)
        {
            sequence = null;

            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                return false;
            }

            Key key = NormalizeKey(e);

            if (key >= Key.A && key <= Key.Z)
            {
                sequence = ((char)(key - Key.A + 1)).ToString();
                return true;
            }

            sequence = key switch
            {
                Key.Space or Key.D2 or Key.NumPad2 => "\0",
                Key.OemOpenBrackets => "\x1b",
                Key.Oem5 => "\x1c",
                Key.Oem6 => "\x1d",
                Key.D6 or Key.NumPad6 => "\x1e",
                Key.OemMinus or Key.Subtract or Key.Oem2 or Key.Divide => "\x1f",
                _ => null
            };

            return sequence != null;
        }

        private static string? GetLocalEchoText(Key key)
        {
            return key switch
            {
                Key.Enter or Key.Return => "\r\n",
                Key.Back => "\b \b",
                Key.Tab => "\t",
                _ => null
            };
        }

        private bool SendToConnection(string text, string? localEchoText)
        {
            var connection = Connection;

            if (connection?.IsConnected != true || string.IsNullOrEmpty(text))
            {
                return false;
            }

            _ = SendToConnectionAsync(connection, text, localEchoText);
            return true;
        }

        private async Task SendToConnectionAsync(ITerminalConnection connection, string text, string? localEchoText)
        {
            try
            {
                await _sendGate.WaitAsync();
                try
                {
                    await connection.SendAsync(text);
                }
                finally
                {
                    _sendGate.Release();
                }

                EchoLocal(localEchoText);
            }
            catch (Exception ex)
            {
                OnConnectionError(ex);
            }
        }

        private async Task SendToConnectionAsync(ITerminalConnection connection, byte[] data)
        {
            try
            {
                await _sendGate.WaitAsync();
                try
                {
                    await connection.SendAsync(data);
                }
                finally
                {
                    _sendGate.Release();
                }
            }
            catch (Exception ex)
            {
                OnConnectionError(ex);
            }
        }

        private void EchoLocal(string? text)
        {
            if (!LocalEcho || string.IsNullOrEmpty(text))
            {
                return;
            }

            Add(text);
        }

        private void TransmitToHost(byte[]? data)
        {
            Transmit?.Invoke(data);

            if (Connection?.IsConnected != true || data == null || data.Length == 0)
            {
                return;
            }

            _ = SendToConnectionAsync(Connection, data);
        }

        private void ApplyCurrentTerminalSize(ITerminalConnection connection, bool sendWindowChange)
        {
            var (rows, cols, pxW, pxH) = GetTerminalDimensions();

            connection.Rows = rows;
            connection.Columns = cols;
            connection.Width = pxW;
            connection.Height = pxH;

            if (sendWindowChange)
            {
                connection.SendWindowChangeRequest((uint)cols, (uint)rows, (uint)pxW, (uint)pxH);
            }
        }

        private (int Rows, int Columns, int PixelWidth, int PixelHeight) GetTerminalDimensions()
        {
            if (TextArea?.TextView == null)
            {
                return (Rows > 0 ? Rows : 24, Columns > 0 ? Columns : 80, Math.Max(0, (int)ActualWidth), Math.Max(0, (int)ActualHeight));
            }

            var tv = TextArea.TextView;
            double charW = tv.WideSpaceWidth;
            double lineH = tv.DefaultLineHeight;

            if (charW <= 0 || lineH <= 0)
            {
                return (Rows > 0 ? Rows : 24, Columns > 0 ? Columns : 80, Math.Max(0, (int)ActualWidth), Math.Max(0, (int)ActualHeight));
            }

            double padH = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
            double padV = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;

            double clientW = Math.Max(0, ActualWidth - padH);
            double clientH = Math.Max(0, ActualHeight - padV);

            int cols = Math.Max(1, (int)Math.Floor(clientW / charW));
            int rows = Math.Max(1, (int)Math.Floor((clientH - lineH + 0.1) / lineH) - 1);

            var dpi = VisualTreeHelper.GetDpi(this);
            int pxW = (int)Math.Round(clientW * dpi.DpiScaleX);
            int pxH = (int)Math.Round(clientH * dpi.DpiScaleY);

            return (rows, cols, pxW, pxH);
        }

        private void OnConnectionError(Exception ex)
        {
            ConnectionError?.Invoke(this, ex);
        }

        /// <summary>
        /// Reset terminal state and clear the screen.
        /// </summary>
        public void Reset(int rows = 24, int cols = 80)
        {
            lock (_lock)
            {
                Rows = Math.Max(1, rows);
                Columns = Math.Max(1, cols);
                _buf = new TerminalCell[Rows, Columns];
                _currentAttrs = TerminalAttributes.Default;
                FillRect(0, 0, Rows, Columns, ' ');
                _curRow = 0;
                _curCol = 0;
                _state = ParseState.Normal;
                _scrollTop = 1;
                _scrollBottom = Rows;
                _originMode = false;
                _autowrapPending = false;
                _autoWrap = true;
                _applicationCursorKeys = false;
                _applicationKeypad = false;
                _cursorVisible = true;
                _insertMode = false;
                _newLineMode = false;
                _bracketedPaste = false;
                _hasSavedCursor = false;
                _savedAttrs = TerminalAttributes.Default;
                _savedOriginMode = false;
                _altBuf = null;
                _useAltBuffer = false;
                _csiParams.Clear();
                _csiCurrentParam = 0;
                _csiHasParam = false;
                _csiPrivateMarker = '\0';
                _scrollback.Clear();
                _scrollbackDirty = false;
                _g0Charset = CharSet.Ascii;
                _g1Charset = CharSet.Ascii;
                _shiftOut = false;
                ResetTabStops();
                UpdateDocument(forceScrollTop: true);
            }
        }

        /// <summary>
        /// Resize the terminal and preserve top-left content where possible.
        /// </summary>
        public void Resize(int rows, int cols)
        {
            lock (_lock)
            {
                rows = Math.Max(1, rows);
                cols = Math.Max(1, cols);

                if (rows == Rows && cols == Columns)
                {
                    return;
                }

                var newBuf = new TerminalCell[rows, cols];
                var defaultCell = new TerminalCell(' ', TerminalAttributes.Default);
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        newBuf[r, c] = defaultCell;
                    }
                }

                int cr = Math.Min(rows, Rows);
                int cc = Math.Min(cols, Columns);
                for (int r = 0; r < cr; r++)
                {
                    for (int c = 0; c < cc; c++)
                    {
                        newBuf[r, c] = _buf[r, c];
                    }
                }

                Rows = rows;
                Columns = cols;
                _buf = newBuf;

                // If we are on the alternate screen, the saved main buffer (_altBuf) was allocated
                // at the old dimensions. It must be resized in lock-step, otherwise restoring it on
                // alt-screen exit yields a buffer whose dimensions no longer match Rows/Columns and
                // BuildLineSnapshot throws IndexOutOfRangeException.
                if (_altBuf != null)
                {
                    int oldRows = _altBuf.GetLength(0);
                    int oldCols = _altBuf.GetLength(1);
                    var newAlt = new TerminalCell[rows, cols];
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            newAlt[r, c] = defaultCell;
                        }
                    }

                    int ar = Math.Min(rows, oldRows);
                    int ac = Math.Min(cols, oldCols);
                    for (int r = 0; r < ar; r++)
                    {
                        for (int c = 0; c < ac; c++)
                        {
                            newAlt[r, c] = _altBuf[r, c];
                        }
                    }

                    _altBuf = newAlt;
                    _altCurRow = Math.Min(_altCurRow, rows - 1);
                    _altCurCol = Math.Min(_altCurCol, cols - 1);
                }

                _curRow = Math.Min(_curRow, Rows - 1);
                _curCol = Math.Min(_curCol, Columns - 1);

                // Adjust scroll region to new size
                _scrollTop = Math.Min(_scrollTop, Rows);
                _scrollBottom = Rows; // Reset to full screen on resize

                ResetTabStops();

                UpdateDocument();
            }
        }


        /// <summary>
        /// Raised after the terminal is resized. Arguments are rows, columns, pixel width, and pixel height.
        /// </summary>
        public event Action<int, int, int, int>? TerminalResized;

        /// <summary>
        /// Recalculates the terminal dimensions from the current control size and resizes the buffer accordingly.
        /// Called automatically on <see cref="FrameworkElement.SizeChanged"/> and after the control is loaded.
        /// </summary>
        public void OnSizeChanged()
        {
            if (TextArea?.TextView == null)
            {
                return;
            }

            var tv = TextArea.TextView;
            double charW = tv.WideSpaceWidth;
            double lineH = tv.DefaultLineHeight;

            if (charW <= 0 || lineH <= 0)
            {
                Dispatcher.BeginInvoke(OnSizeChanged, DispatcherPriority.Loaded);
                return;
            }

            var (rows, cols, pxW, pxH) = GetTerminalDimensions();

            Resize(rows, cols);

            if (Connection != null)
            {
                ApplyCurrentTerminalSize(Connection, sendWindowChange: Connection.IsConnected);
            }

            TerminalResized?.Invoke(rows, cols, pxW, pxH);
        }

        /// <summary>
        /// Processes raw bytes from the remote host as terminal data.
        /// </summary>
        /// <param name="data">The bytes to process. <see langword="null"/> or empty arrays are ignored.</param>
        public void Add(byte[]? data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            lock (_lock)
            {
                int maxChars = _utf8Decoder.GetCharCount(data, 0, data.Length, flush: false);
                if (maxChars > _decodeBuffer.Length)
                {
                    _decodeBuffer = new char[maxChars];
                }

                int decoded = _utf8Decoder.GetChars(data, 0, data.Length, _decodeBuffer, 0, flush: false);
                for (int i = 0; i < decoded; i++)
                {
                    ProcessChar(_decodeBuffer[i]);
                }

                // Do not reset parser state here; allow incomplete sequences to continue across calls
                UpdateDocument();
            }
        }

        /// <summary>
        /// Processes a string of terminal data from the remote host.
        /// </summary>
        /// <param name="data">The text to process. <see langword="null"/> or empty strings are ignored.</param>
        public void Add(string? data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            lock (_lock)
            {
                foreach (var ch in data)
                {
                    ProcessChar(ch);
                }
                // Do not reset parser state here; allow incomplete sequences to continue across calls
                UpdateDocument();
            }
        }

        // ======================
        // Core VT52 processing
        // ======================
        private void ProcessChar(char ch)
        {
            switch (_state)
            {
                case ParseState.Normal:
                    if (ch == 0x1B)
                    {
                        _state = ParseState.Esc;
                        return;
                    }

                    if (ch <= 0x1F)
                    {
                        HandleC0(ch);
                        return;
                    }

                    Printable(ch);
                    return;

                case ParseState.Esc:
                    // Only handle ESC sequences via HandleEsc
                    HandleEsc(ch);
                    return;

                case ParseState.EscYRow:
                    _escYRowTmp = ch - 0x1F;
                    if (_escYRowTmp < 1)
                    {
                        _escYRowTmp = 1;
                    }

                    _state = ParseState.EscYCol;
                    return;

                case ParseState.EscYCol:
                    int col = ch - 0x1F;
                    if (col < 1)
                    {
                        col = 1;
                    }

                    CursorPosition(_escYRowTmp, col);
                    _state = ParseState.Normal;
                    return;
                case ParseState.Csi:
                    HandleCsiChar(ch);
                    return;
                case ParseState.OscString:
                    if (ch == '\x07' || ch == '\x1B') // BEL or ESC (ST) terminates OSC
                    {
                        ProcessOsc(_oscBuffer.ToString());
                        _oscBuffer.Clear();
                        _state = ch == '\x1B' ? ParseState.Esc : ParseState.Normal;
                    }
                    else
                    {
                        _oscBuffer.Append(ch);
                    }
                    return;
                case ParseState.EscScs:
                    // Apply the character set designator to the target G set.
                    // '0' = DEC Special Graphics (line drawing); 'B'/'1'/'2'/'A' = ASCII-like.
                    {
                        var cs = ch == '0' ? CharSet.DecSpecialGraphics : CharSet.Ascii;
                        switch (_scsTarget)
                        {
                            case '(':
                                _g0Charset = cs;
                                break;
                            case ')':
                                _g1Charset = cs;
                                break;
                        }

                        // Keep GL in sync if it currently points at the set we just changed.
                        _state = ParseState.Normal;
                    }
                    return;
            }
        }

        private void HandleC0(char ch)
        {
            switch (ch)
            {
                case '\a':
                    Bell?.Invoke();
                    return; // BEL
                case '\b':
                    CursorLeft();
                    return; // BS
                case '\t': // HT - move to next horizontal tab stop (non-destructive)
                    _autowrapPending = false;
                    _curCol = NextTabStop(_curCol);
                    return;
                case '\n':
                    LineFeed();
                    if (_newLineMode)
                    {
                        _curCol = 0; // LNM: LF also performs carriage return
                    }
                    return; // LF
                case '\v':
                    LineFeed();
                    if (_newLineMode)
                    {
                        _curCol = 0;
                    }
                    return; // VT
                case '\f':
                    LineFeed();
                    if (_newLineMode)
                    {
                        _curCol = 0;
                    }
                    return; // FF
                case '\r':
                    _curCol = 0;
                    _autowrapPending = false;
                    _state = ParseState.Normal;
                    return; // CR
                case '\x0E': // SO - invoke G1 into GL (line drawing shift-out)
                    _shiftOut = true;
                    return;
                case '\x0F': // SI - invoke G0 into GL (shift-in)
                    _shiftOut = false;
                    return;
                case '\0':
                    return; // NUL
                default: return;
            }
        }

        private void ResetTabStops()
        {
            _tabStops = new bool[Columns];
            for (int c = 8; c < Columns; c += 8)
            {
                _tabStops[c] = true;
            }
        }

        private int NextTabStop(int col)
        {
            for (int c = col + 1; c < Columns; c++)
            {
                if (_tabStops[c])
                {
                    return c;
                }
            }

            return Columns - 1;
        }

        private int PrevTabStop(int col)
        {
            for (int c = col - 1; c > 0; c--)
            {
                if (_tabStops[c])
                {
                    return c;
                }
            }

            return 0;
        }

        private void HandleEsc(char ch)
        {
            switch (ch)
            {
                case 'A': CursorUp(); break; // up
                case 'B': CursorDown(); break; // down
                case 'C': CursorRight(); break; // right
                case 'D': CursorLeft(); break; // left (also VT100 index down)
                case 'H': CursorHome(); break; // home
                case 'I': ReverseLineFeed(); break; // reverse index
                case 'J': EraseToEndOfScreen(); break; // erase to end of screen
                case 'K': EraseToEndOfLine(); break; // erase to end of line
                case 'Y':
                    _state = ParseState.EscYRow;
                    return; // direct cursor address (row/col + 0x20)
                case 'Z': SendIdentify(); break; // identify
                case '[': // CSI Introducer
                    _csiParams.Clear();
                    _csiCurrentParam = 0;
                    _csiHasParam = false;
                    _csiPrivateMarker = '\0';
                    _state = ParseState.Csi;
                    return;
                case ']': // OSC - Operating System Command
                    _oscBuffer.Clear();
                    _state = ParseState.OscString;
                    return;
                case 'M': // VT100: Reverse Index (move up, scroll if needed)
                    ReverseLineFeed();
                    break;
                case 'E': // VT100: Next Line
                    _curCol = 0;
                    LineFeed();
                    break;
                case '7': // DECSC - Save Cursor (position, attributes, origin mode)
                    SaveCursor();
                    break;
                case '8': // DECRC - Restore Cursor
                    RestoreCursor();
                    break;
                case 'c': // RIS - Full Reset
                    Reset(Rows, Columns);
                    break;
                case '=': // DECKPAM - Keypad Application Mode
                    _applicationKeypad = true;
                    break;
                case '>': // DECKPNM - Keypad Numeric Mode
                    _applicationKeypad = false;
                    break;
                case '(': // SCS G0 - Select Character Set
                case ')': // SCS G1
                case '*': // SCS G2
                case '+': // SCS G3
                case '-': // SCS G1 (VT300)
                case '.': // SCS G2 (VT300)
                case '/': // SCS G3 (VT300)
                    _scsTarget = ch;
                    _state = ParseState.EscScs;
                    return; // Don't reset to Normal - wait for designator byte
                default: break; // unknown ESC: ignore
            }
            // Always set state to Normal after handling ESC except for Y, [, ], and SCS
            _state = ParseState.Normal;
        }

        private void HandleCsiChar(char ch)
        {
            // Private marker at start
            if (_csiParams.Count == 0 && !_csiHasParam && (ch == '?' || ch == '>' || ch == '!'))
            {
                _csiPrivateMarker = ch;
                return;
            }

            // Parameter digits
            if (ch >= '0' && ch <= '9')
            {
                _csiCurrentParam = _csiCurrentParam * 10 + (ch - '0');
                _csiHasParam = true;
                return;
            }

            // Parameter separator
            if (ch == ';')
            {
                _csiParams.Add(_csiHasParam ? _csiCurrentParam : 0);
                _csiCurrentParam = 0;
                _csiHasParam = false;
                return;
            }

            // Intermediate bytes (0x20-0x2F) - ignore for now
            if (ch >= 0x20 && ch <= 0x2F)
            {
                return;
            }

            // Final byte (0x40-0x7E) - execute command
            if (ch >= 0x40 && ch <= 0x7E)
            {
                if (_csiHasParam)
                {
                    _csiParams.Add(_csiCurrentParam);
                }

                ExecuteCsi(ch);
                _state = ParseState.Normal;
            }
        }

        private void ExecuteCsi(char cmd)
        {
            int p0 = _csiParams.Count > 0 ? _csiParams[0] : 0;
            int p1 = _csiParams.Count > 1 ? _csiParams[1] : 0;

            // Handle private mode sequences (ESC [ ? ...)
            if (_csiPrivateMarker == '?')
            {
                switch (cmd)
                {
                    case 'h': // DECSET
                        SetDecPrivateMode(p0, true);
                        break;
                    case 'l': // DECRST
                        SetDecPrivateMode(p0, false);
                        break;
                }
                return;
            }

            // DECSTR - Soft Terminal Reset (CSI ! p)
            if (_csiPrivateMarker == '!')
            {
                if (cmd == 'p')
                {
                    SoftReset();
                }

                return;
            }

            // Secondary Device Attributes (CSI > c)
            if (_csiPrivateMarker == '>')
            {
                if (cmd == 'c')
                {
                    // Report as VT220 (terminal type 1), firmware version 0.
                    TransmitToHost(Encoding.ASCII.GetBytes("\x1B[>1;0;0c"));
                }

                return;
            }

            switch (cmd)
            {
                case 'A': // CUU - Cursor Up
                    for (int i = 0; i < Math.Max(1, p0); i++) CursorUp();
                    break;
                case 'B': // CUD - Cursor Down
                    for (int i = 0; i < Math.Max(1, p0); i++) CursorDownNoScroll();
                    break;
                case 'C': // CUF - Cursor Forward (Right)
                    for (int i = 0; i < Math.Max(1, p0); i++) CursorRight();
                    break;
                case 'D': // CUB - Cursor Back (Left)
                    for (int i = 0; i < Math.Max(1, p0); i++) CursorLeft();
                    break;
                case 'E': // CNL - Cursor Next Line
                    _curCol = 0;
                    for (int i = 0; i < Math.Max(1, p0); i++) LineFeed();
                    break;
                case 'F': // CPL - Cursor Previous Line
                    _curCol = 0;
                    for (int i = 0; i < Math.Max(1, p0); i++) CursorUp();
                    break;
                case 'G': // CHA - Cursor Horizontal Absolute
                case '`': // HPA - Horizontal Position Absolute (same as CHA)
                    _curCol = Math.Max(0, Math.Min((p0 > 0 ? p0 : 1) - 1, Columns - 1));
                    break;
                case 'a': // HPR - Horizontal Position Relative (cursor forward)
                    for (int i = 0; i < Math.Max(1, p0); i++) CursorRight();
                    break;
                case 'H': // CUP - Cursor Position
                case 'f': // HVP - same as CUP
                    CursorPositionCsi(p0 > 0 ? p0 : 1, p1 > 0 ? p1 : 1);
                    break;
                case 'I': // CHT - Cursor Horizontal (forward) Tab
                    for (int i = 0; i < Math.Max(1, p0); i++) _curCol = NextTabStop(_curCol);
                    break;
                case 'Z': // CBT - Cursor Backward Tab
                    for (int i = 0; i < Math.Max(1, p0); i++) _curCol = PrevTabStop(_curCol);
                    break;
                case 'J': // ED - Erase in Display
                    EraseInDisplay(p0);
                    break;
                case 'K': // EL - Erase in Line
                    EraseInLine(p0);
                    break;
                case 'L': // IL - Insert Lines
                    InsertLines(Math.Max(1, p0));
                    break;
                case 'M': // DL - Delete Lines
                    DeleteLines(Math.Max(1, p0));
                    break;
                case 'P': // DCH - Delete Characters
                    DeleteChars(Math.Max(1, p0));
                    break;
                case '@': // ICH - Insert Characters
                    InsertChars(Math.Max(1, p0));
                    break;
                case 'X': // ECH - Erase Characters
                    EraseChars(Math.Max(1, p0));
                    break;
                case 'b': // REP - Repeat preceding graphic character
                    RepeatLastChar(Math.Max(1, p0));
                    break;
                case 'd': // VPA - Vertical Position Absolute
                    _curRow = Math.Max(0, Math.Min((p0 > 0 ? p0 : 1) - 1, Rows - 1));
                    break;
                case 'e': // VPR - Vertical Position Relative (cursor down)
                    for (int i = 0; i < Math.Max(1, p0); i++) CursorDownNoScroll();
                    break;
                case 'g': // TBC - Tab Clear
                    if (p0 == 3)
                    {
                        Array.Clear(_tabStops, 0, _tabStops.Length);
                    }
                    else if (_curCol >= 0 && _curCol < _tabStops.Length)
                    {
                        _tabStops[_curCol] = false;
                    }
                    break;
                case 'm': // SGR - Select Graphic Rendition (colors/attributes)
                    HandleSgr();
                    break;
                case 'r': // DECSTBM - Set Top and Bottom Margins (scroll region)
                    {
                        int top = p0 > 0 ? p0 : 1;
                        int bottom = p1 > 0 ? p1 : Rows;
                        if (top < bottom && top >= 1 && bottom <= Rows)
                        {
                            _scrollTop = top;
                            _scrollBottom = bottom;
                            // Move cursor to home (origin-mode aware)
                            CursorPositionCsi(1, 1);
                        }
                    }
                    break;
                case 's': // SCP / DECSC variant - Save Cursor Position
                    SaveCursor();
                    break;
                case 'u': // RCP / DECRC variant - Restore Cursor Position
                    RestoreCursor();
                    break;
                case 'h': // SM - Set Mode
                    SetAnsiMode(p0, true);
                    break;
                case 'l': // RM - Reset Mode
                    SetAnsiMode(p0, false);
                    break;
                case 'n': // DSR - Device Status Report
                    if (p0 == 6) // Report Cursor Position
                    {
                        // Send CPR response: ESC [ row ; col R
                        var response = $"\x1B[{_curRow + 1};{_curCol + 1}R";
                        TransmitToHost(Encoding.ASCII.GetBytes(response));
                    }
                    else if (p0 == 5) // Report device status (always OK)
                    {
                        TransmitToHost(Encoding.ASCII.GetBytes("\x1B[0n"));
                    }
                    break;
                case 'c': // DA - Device Attributes
                    // Report as VT220 with no extra options.
                    TransmitToHost(Encoding.ASCII.GetBytes("\x1B[?62;1;6c"));
                    break;
                case 'S': // SU - Scroll Up
                    for (int i = 0; i < Math.Max(1, p0); i++) ScrollUpRegion();
                    break;
                case 'T': // SD - Scroll Down
                    for (int i = 0; i < Math.Max(1, p0); i++) ScrollDownRegion();
                    break;
            }
            _autowrapPending = false;
        }

        /// <summary>Sets or resets a standard (non-private) ANSI mode.</summary>
        private void SetAnsiMode(int mode, bool set)
        {
            switch (mode)
            {
                case 4: // IRM - Insert/Replace Mode
                    _insertMode = set;
                    break;
                case 20: // LNM - Line Feed/New Line Mode
                    _newLineMode = set;
                    break;
            }
        }

        /// <summary>Repeats the last printed graphic character the supplied number of times.</summary>
        private void RepeatLastChar(int count)
        {
            if (_lastGraphicChar == '\0')
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Printable(_lastGraphicChar);
            }
        }

        /// <summary>
        /// DECSTR soft reset: restores the common modes and cursor state without clearing the screen.
        /// </summary>
        private void SoftReset()
        {
            _currentAttrs = TerminalAttributes.Default;
            _originMode = false;
            _autoWrap = true;
            _autowrapPending = false;
            _applicationCursorKeys = false;
            _applicationKeypad = false;
            _cursorVisible = true;
            _insertMode = false;
            _newLineMode = false;
            _scrollTop = 1;
            _scrollBottom = Rows;
            _hasSavedCursor = false;
            _g0Charset = CharSet.Ascii;
            _g1Charset = CharSet.Ascii;
            _shiftOut = false;
            SetCaretVisible(true);
        }

        private void SetDecPrivateMode(int mode, bool set)
        {
            switch (mode)
            {
                case 1: // DECCKM - Cursor Keys Mode (application vs normal)
                    _applicationCursorKeys = set;
                    break;
                case 6: // DECOM - Origin Mode
                    _originMode = set;
                    CursorPositionCsi(1, 1); // Move to origin
                    break;
                case 7: // DECAWM - Autowrap Mode
                    _autoWrap = set;
                    if (!set)
                    {
                        _autowrapPending = false;
                    }
                    break;
                case 25: // DECTCEM - Text Cursor Enable Mode (show/hide cursor)
                    _cursorVisible = set;
                    SetCaretVisible(set);
                    break;
                case 1049: // Alternate Screen Buffer (with save/restore cursor)
                case 47: // Alternate Screen Buffer (simple)
                case 1047: // Alternate Screen Buffer
                    if (set)
                    {
                        // Switch to alternate buffer
                        if (!_useAltBuffer)
                        {
                            _altBuf = _buf;
                            _altCurRow = _curRow;
                            _altCurCol = _curCol;
                            _altAttrs = _currentAttrs;
                            _buf = new TerminalCell[Rows, Columns];
                            var savedAttrs = _currentAttrs;
                            _currentAttrs = TerminalAttributes.Default;
                            FillRect(0, 0, Rows, Columns, ' ');
                            _currentAttrs = savedAttrs;
                            _curRow = 0;
                            _curCol = 0;
                            _useAltBuffer = true;
                            AlternateScreenChanged?.Invoke(true);
                        }
                    }
                    else
                    {
                        // Switch back to main buffer
                        if (_useAltBuffer && _altBuf != null)
                        {
                            _buf = _altBuf;
                            _curRow = _altCurRow;
                            _curCol = _altCurCol;
                            _currentAttrs = _altAttrs;
                            _altBuf = null;
                            _useAltBuffer = false;
                            AlternateScreenChanged?.Invoke(false);
                        }
                    }
                    break;
                case 2004: // Bracketed Paste Mode
                    _bracketedPaste = set;
                    break;
            }
        }

        /// <summary>
        /// Processes a completed OSC (Operating System Command) string. Currently handles
        /// window/icon title commands (OSC 0/1/2); other commands are ignored.
        /// </summary>
        private void ProcessOsc(string osc)
        {
            if (string.IsNullOrEmpty(osc))
            {
                return;
            }

            int sep = osc.IndexOf(';');
            if (sep < 0)
            {
                return;
            }

            string code = osc.Substring(0, sep);
            string text = osc.Substring(sep + 1);

            // OSC 0 = icon name + window title, 1 = icon name, 2 = window title.
            if (code is "0" or "1" or "2")
            {
                Title = text;
                var handler = TitleChanged;
                if (handler != null)
                {
                    if (Dispatcher.CheckAccess())
                    {
                        handler(text);
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(() => handler(text), DispatcherPriority.Background);
                    }
                }
            }
        }

        /// <summary>
        /// Toggles the block caret visibility (DECTCEM). Marshals to the UI thread when needed.
        /// </summary>
        private void SetCaretVisible(bool visible)
        {
            if (_caretRenderer == null)
            {
                return;
            }

            void Apply()
            {
                _caretRenderer.Visible = visible;
                TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Caret);
            }

            if (Dispatcher.CheckAccess())
            {
                Apply();
            }
            else
            {
                Dispatcher.BeginInvoke(Apply, DispatcherPriority.Render);
            }
        }

        private void Printable(char ch)
        {
            // Zero-width and combining characters do not occupy a grid cell. Skipping them
            // (rather than storing them in their own cell) keeps the fixed-width column grid
            // aligned, which matters for full-screen TUI applications. This control renders one
            // code point per column via AvalonEdit, so true combining-mark composition is not
            // attempted.
            if (IsZeroWidth(ch))
            {
                return;
            }

            // Translate through the active character set (DEC special graphics / line drawing).
            if (_shiftOut ? _g1Charset == CharSet.DecSpecialGraphics : _g0Charset == CharSet.DecSpecialGraphics)
            {
                ch = MapSpecialGraphics(ch);
            }

            // Handle deferred autowrap
            if (_autowrapPending)
            {
                _autowrapPending = false;
                if (_curRow >= _scrollTop - 1 && _curRow <= _scrollBottom - 1)
                {
                    if (_curRow < _scrollBottom - 1)
                    {
                        _curRow++;
                    }
                    else
                    {
                        ScrollUpRegion();
                    }
                }
                else if (_curRow < Rows - 1)
                {
                    _curRow++;
                }
                _curCol = 0;
            }

            // Insert mode (IRM): shift the remainder of the line right before writing.
            if (_insertMode && _curCol < Columns - 1)
            {
                int rowOffset = _curRow * Columns;
                int moveCount = Columns - _curCol - 1;
                Array.Copy(_buf, rowOffset + _curCol, _buf, rowOffset + _curCol + 1, moveCount);
            }

            _buf[_curRow, _curCol] = new TerminalCell(ch, _currentAttrs);
            _lastGraphicChar = ch;

            if (_curCol == Columns - 1)
            {
                // At the right margin: defer the wrap only when autowrap (DECAWM) is enabled.
                // With autowrap disabled the cursor stays put and further glyphs overwrite the
                // last cell.
                if (_autoWrap)
                {
                    _autowrapPending = true;
                }

                return;
            }

            _curCol++;
        }

        /// <summary>
        /// Maps a byte from the DEC Special Graphics character set to its Unicode equivalent
        /// (box-drawing and line-drawing glyphs). Characters outside the 0x5F-0x7E range are
        /// returned unchanged.
        /// </summary>
        private static char MapSpecialGraphics(char ch)
        {
            return ch switch
            {
                '\x5F' => ' ',      // blank
                '\x60' => '\u25C6', // ◆ diamond
                'a' => '\u2592',    // ▒ checkerboard
                'b' => '\u2409',    // HT symbol
                'c' => '\u240C',    // FF symbol
                'd' => '\u240D',    // CR symbol
                'e' => '\u240A',    // LF symbol
                'f' => '\u00B0',    // ° degree
                'g' => '\u00B1',    // ± plus/minus
                'h' => '\u2424',    // NL symbol
                'i' => '\u240B',    // VT symbol
                'j' => '\u2518',    // ┘ lower-right corner
                'k' => '\u2510',    // ┐ upper-right corner
                'l' => '\u250C',    // ┌ upper-left corner
                'm' => '\u2514',    // └ lower-left corner
                'n' => '\u253C',    // ┼ crossing lines
                'o' => '\u23BA',    // ⎺ scan line 1
                'p' => '\u23BB',    // ⎻ scan line 3
                'q' => '\u2500',    // ─ horizontal line
                'r' => '\u23BC',    // ⎼ scan line 7
                's' => '\u23BD',    // ⎽ scan line 9
                't' => '\u251C',    // ├ left tee
                'u' => '\u2524',    // ┤ right tee
                'v' => '\u2534',    // ┴ bottom tee
                'w' => '\u252C',    // ┬ top tee
                'x' => '\u2502',    // │ vertical line
                'y' => '\u2264',    // ≤ less-than-or-equal
                'z' => '\u2265',    // ≥ greater-than-or-equal
                '{' => '\u03C0',    // π pi
                '|' => '\u2260',    // ≠ not equal
                '}' => '\u00A3',    // £ pound
                '~' => '\u00B7',    // · centered dot
                _ => ch
            };
        }

        private void SaveCursor()
        {
            _savedCurRow = _curRow;
            _savedCurCol = _curCol;
            _savedAttrs = _currentAttrs;
            _savedOriginMode = _originMode;
            _hasSavedCursor = true;
        }

        private void RestoreCursor()
        {
            if (!_hasSavedCursor)
            {
                _curRow = 0;
                _curCol = 0;
                return;
            }

            _curRow = Math.Min(_savedCurRow, Rows - 1);
            _curCol = Math.Min(_savedCurCol, Columns - 1);
            _currentAttrs = _savedAttrs;
            _originMode = _savedOriginMode;
            _autowrapPending = false;
        }

        /// <summary>
        /// Returns true for zero-width characters (combining marks, zero-width spaces/joiners)
        /// that should not advance the cursor or occupy a terminal cell.
        /// </summary>
        private static bool IsZeroWidth(char ch)
        {
            if (ch < 0x0300)
            {
                return false;
            }

            switch (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch))
            {
                case System.Globalization.UnicodeCategory.NonSpacingMark:
                case System.Globalization.UnicodeCategory.EnclosingMark:
                case System.Globalization.UnicodeCategory.Format:
                    // U+00AD soft hyphen is a Format char but is conventionally rendered; keep it.
                    return ch != 0x00AD;
                default:
                    return false;
            }
        }

        private void CursorRight()
        {
            _autowrapPending = false;
            if (_curCol < Columns - 1)
            {
                _curCol++;
            }
        }

        private void CursorLeft()
        {
            _autowrapPending = false;
            if (_curCol > 0)
            {
                _curCol--;
            }
        }

        private void CursorUp()
        {
            _autowrapPending = false;
            // Respect scroll region top when cursor is inside the region
            int minRow = (_curRow >= _scrollTop - 1 && _curRow <= _scrollBottom - 1) ? _scrollTop - 1 : 0;
            if (_curRow > minRow)
            {
                _curRow--;
            }
        }

        private void CursorDown()
        {
            _autowrapPending = false;
            if (_curRow < Rows - 1)
            {
                _curRow++;
            }
            else
            {
                ScrollUpRegion();
            }
        }

        private void CursorDownNoScroll()
        {
            _autowrapPending = false;
            // Respect scroll region bottom when cursor is inside the region
            int maxRow = (_curRow >= _scrollTop - 1 && _curRow <= _scrollBottom - 1) ? _scrollBottom - 1 : Rows - 1;
            if (_curRow < maxRow)
            {
                _curRow++;
            }
        }

        private void CursorHome()
        {
            _autowrapPending = false;
            _curRow = 0;
            _curCol = 0;
        }

        private void CursorPosition(int row1, int col1)
        {
            _autowrapPending = false;
            row1 = Math.Max(1, Math.Min(row1, Rows));
            col1 = Math.Max(1, Math.Min(col1, Columns));
            _curRow = row1 - 1;
            _curCol = col1 - 1;
        }

        private void CursorPositionCsi(int row1, int col1)
        {
            _autowrapPending = false;
            if (_originMode)
            {
                // Origin mode: row is relative to scroll region
                row1 = Math.Max(1, Math.Min(row1, _scrollBottom - _scrollTop + 1));
                _curRow = _scrollTop - 1 + row1 - 1;
            }
            else
            {
                row1 = Math.Max(1, Math.Min(row1, Rows));
                _curRow = row1 - 1;
            }
            col1 = Math.Max(1, Math.Min(col1, Columns));
            _curCol = col1 - 1;
        }

        private void LineFeed()
        {
            _autowrapPending = false;
            // Check if we're in the scroll region
            if (_curRow >= _scrollTop - 1 && _curRow < _scrollBottom - 1)
            {
                _curRow++;
            }
            else if (_curRow == _scrollBottom - 1)
            {
                // At bottom of scroll region - scroll
                ScrollUpRegion();
            }
            else if (_curRow < Rows - 1)
            {
                // Outside scroll region
                _curRow++;
            }
        }

        private void ReverseLineFeed()
        {
            _autowrapPending = false;
            if (_curRow == _scrollTop - 1)
            {
                // At top of scroll region - scroll down
                ScrollDownRegion();
            }
            else if (_curRow > 0)
            {
                _curRow--;
            }
        }

        private void ScrollUpRegion()
        {
            // Scroll within scroll region only
            int top = _scrollTop - 1;
            int bottom = _scrollBottom - 1;
            var blankCell = new TerminalCell(' ', TerminalAttributes.Default);

            // Capture the departing top line into scrollback only when the whole screen is
            // scrolling (no custom margins) and we are not on the alternate screen.
            if (!_useAltBuffer && top == 0 && bottom == Rows - 1 && _scrollback.Capacity > 0)
            {
                PushScrollbackLine(0);
            }

            if (bottom > top)
            {
                Array.Copy(_buf, (top + 1) * Columns, _buf, top * Columns, (bottom - top) * Columns);
            }

            FillRow(bottom, 0, Columns, blankCell);
        }

        private void PushScrollbackLine(int row)
        {
            var line = _recycledLine;

            if (line == null || line.Length != Columns)
            {
                line = new TerminalCell[Columns];
            }

            for (int c = 0; c < Columns; c++)
            {
                line[c] = _buf[row, c];
            }

            _recycledLine = _scrollback.AddAndRecycle(line);
            _scrollbackDirty = true;
        }

        private void ScrollDownRegion()
        {
            // Scroll down within scroll region only
            int top = _scrollTop - 1;
            int bottom = _scrollBottom - 1;
            var blankCell = new TerminalCell(' ', TerminalAttributes.Default);

            if (bottom > top)
            {
                Array.Copy(_buf, top * Columns, _buf, (top + 1) * Columns, (bottom - top) * Columns);
            }

            FillRow(top, 0, Columns, blankCell);
        }

        private void EraseToEndOfLine()
        {
            var blankCell = new TerminalCell(' ', _currentAttrs);
            for (int c = _curCol; c < Columns; c++)
            {
                _buf[_curRow, c] = blankCell;
            }
        }

        private void EraseToEndOfScreen()
        {
            EraseToEndOfLine();
            var blankCell = new TerminalCell(' ', _currentAttrs);
            for (int r = _curRow + 1; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    _buf[r, c] = blankCell;
                }
            }
        }

        private void EraseInDisplay(int mode)
        {
            var blankCell = new TerminalCell(' ', _currentAttrs);
            switch (mode)
            {
                case 0: // Erase from cursor to end of screen
                    EraseToEndOfScreen();
                    break;
                case 1: // Erase from start to cursor
                    for (int r = 0; r < _curRow; r++)
                    {
                        for (int c = 0; c < Columns; c++)
                        {
                            _buf[r, c] = blankCell;
                        }
                    }
                    for (int c = 0; c <= _curCol; c++)
                    {
                        _buf[_curRow, c] = blankCell;
                    }
                    break;
                case 3: // Erase entire screen + scrollback buffer
                    _scrollback.Clear();
                    _scrollbackDirty = true;
                    FillRect(0, 0, Rows, Columns, ' ');
                    break;
                case 2: // Erase entire screen
                    FillRect(0, 0, Rows, Columns, ' ');
                    break;
            }
        }

        private void EraseInLine(int mode)
        {
            var blankCell = new TerminalCell(' ', _currentAttrs);
            switch (mode)
            {
                case 0: // Erase from cursor to end of line
                    EraseToEndOfLine();
                    break;
                case 1: // Erase from start of line to cursor
                    for (int c = 0; c <= _curCol; c++)
                    {
                        _buf[_curRow, c] = blankCell;
                    }
                    break;
                case 2: // Erase entire line
                    for (int c = 0; c < Columns; c++)
                    {
                        _buf[_curRow, c] = blankCell;
                    }
                    break;
            }
        }

        private void InsertLines(int count)
        {
            // Insert blank lines at cursor, pushing existing lines down
            // Only works within scroll region
            if (_curRow < _scrollTop - 1 || _curRow > _scrollBottom - 1) return;

            var blankCell = new TerminalCell(' ', _currentAttrs);
            int bottom = _scrollBottom - 1;
            count = Math.Min(count, bottom - _curRow + 1);
            int moveRows = bottom - _curRow + 1 - count;

            if (moveRows > 0)
            {
                Array.Copy(_buf, _curRow * Columns, _buf, (_curRow + count) * Columns, moveRows * Columns);
            }

            FillRows(_curRow, count, blankCell);
            _curCol = 0;
        }

        private void DeleteLines(int count)
        {
            // Delete lines at cursor, pulling up lines from below
            // Only works within scroll region
            if (_curRow < _scrollTop - 1 || _curRow > _scrollBottom - 1) return;

            var blankCell = new TerminalCell(' ', _currentAttrs);
            int bottom = _scrollBottom - 1;
            count = Math.Min(count, bottom - _curRow + 1);
            int moveRows = bottom - _curRow + 1 - count;

            if (moveRows > 0)
            {
                Array.Copy(_buf, (_curRow + count) * Columns, _buf, _curRow * Columns, moveRows * Columns);
            }

            FillRows(bottom - count + 1, count, blankCell);
            _curCol = 0;
        }

        private void DeleteChars(int count)
        {
            // Delete characters at cursor position, shifting rest of line left
            var blankCell = new TerminalCell(' ', _currentAttrs);
            count = Math.Min(count, Columns - _curCol);
            int rowOffset = _curRow * Columns;
            int moveCount = Columns - _curCol - count;

            if (moveCount > 0)
            {
                Array.Copy(_buf, rowOffset + _curCol + count, _buf, rowOffset + _curCol, moveCount);
            }

            FillRow(_curRow, Columns - count, count, blankCell);
        }

        private void InsertChars(int count)
        {
            // Insert blank characters at cursor position, shifting rest of line right
            var blankCell = new TerminalCell(' ', _currentAttrs);
            count = Math.Min(count, Columns - _curCol);
            int rowOffset = _curRow * Columns;
            int moveCount = Columns - _curCol - count;

            if (moveCount > 0)
            {
                Array.Copy(_buf, rowOffset + _curCol, _buf, rowOffset + _curCol + count, moveCount);
            }

            FillRow(_curRow, _curCol, count, blankCell);
        }

        private void EraseChars(int count)
        {
            // Erase characters starting at cursor (replace with spaces, don't shift)
            var blankCell = new TerminalCell(' ', _currentAttrs);
            for (int i = 0; i < count && _curCol + i < Columns; i++)
            {
                _buf[_curRow, _curCol + i] = blankCell;
            }
        }

        private void SendIdentify()
        {
            // DEC VT52 identity response: ESC / Z
            TransmitToHost([0x1B, (byte)'/', (byte)'Z']);
        }

        private void FillRect(int row, int col, int height, int width, char ch)
        {
            var cell = new TerminalCell(ch, _currentAttrs);
            for (int r = row; r < row + height; r++)
            {
                FillRow(r, col, width, cell);
            }
        }

        private void FillRows(int row, int count, TerminalCell cell)
        {
            for (int r = row; r < row + count; r++)
            {
                FillRow(r, 0, Columns, cell);
            }
        }

        private void FillRow(int row, int col, int count, TerminalCell cell)
        {
            for (int c = col; c < col + count; c++)
            {
                _buf[row, c] = cell;
            }
        }

        /// <summary>
        /// Handle SGR (Select Graphic Rendition) escape sequences for colors and text attributes.
        /// </summary>
        private void HandleSgr()
        {
            // If no params, treat as reset (SGR 0)
            if (_csiParams.Count == 0)
            {
                _currentAttrs = TerminalAttributes.Default;
                return;
            }

            for (int i = 0; i < _csiParams.Count; i++)
            {
                int p = _csiParams[i];
                switch (p)
                {
                    case 0: // Reset
                        _currentAttrs = TerminalAttributes.Default;
                        break;
                    case 1: // Bold
                        _currentAttrs.Bold = true;
                        break;
                    case 2: // Dim
                        _currentAttrs.Dim = true;
                        break;
                    case 3: // Italic
                        _currentAttrs.Italic = true;
                        break;
                    case 4: // Underline
                        _currentAttrs.Underline = true;
                        break;
                    case 5: // Slow Blink
                    case 6: // Rapid Blink
                        _currentAttrs.Blink = true;
                        break;
                    case 7: // Reverse video
                        _currentAttrs.Reverse = true;
                        break;
                    case 8: // Hidden
                        _currentAttrs.Hidden = true;
                        break;
                    case 9: // Strikethrough
                        _currentAttrs.Strikethrough = true;
                        break;
                    case 21: // Double underline (treat as underline)
                        _currentAttrs.Underline = true;
                        break;
                    case 22: // Normal intensity (not bold, not dim)
                        _currentAttrs.Bold = false;
                        _currentAttrs.Dim = false;
                        break;
                    case 23: // Not italic
                        _currentAttrs.Italic = false;
                        break;
                    case 24: // Not underlined
                        _currentAttrs.Underline = false;
                        break;
                    case 25: // Not blinking
                        _currentAttrs.Blink = false;
                        break;
                    case 27: // Not reversed
                        _currentAttrs.Reverse = false;
                        break;
                    case 28: // Not hidden
                        _currentAttrs.Hidden = false;
                        break;
                    case 29: // Not strikethrough
                        _currentAttrs.Strikethrough = false;
                        break;

                    // Standard foreground colors (30-37)
                    case >= 30 and <= 37:
                        _currentAttrs.Foreground = (byte)(p - 30);
                        _currentAttrs.DefaultForeground = false;
                        break;
                    case 38: // Extended foreground color
                        i = ParseExtendedColor(i, ref _currentAttrs.Foreground);
                        _currentAttrs.DefaultForeground = false;
                        break;
                    case 39: // Default foreground
                        _currentAttrs.Foreground = 7;
                        _currentAttrs.DefaultForeground = true;
                        break;

                    // Standard background colors (40-47)
                    case >= 40 and <= 47:
                        _currentAttrs.Background = (byte)(p - 40);
                        _currentAttrs.DefaultBackground = false;
                        break;
                    case 48: // Extended background color
                        i = ParseExtendedColor(i, ref _currentAttrs.Background);
                        _currentAttrs.DefaultBackground = false;
                        break;
                    case 49: // Default background
                        _currentAttrs.Background = 0;
                        _currentAttrs.DefaultBackground = true;
                        break;

                    // Bright foreground colors (90-97)
                    case >= 90 and <= 97:
                        _currentAttrs.Foreground = (byte)(p - 90 + 8);
                        _currentAttrs.DefaultForeground = false;
                        break;

                    // Bright background colors (100-107)
                    case >= 100 and <= 107:
                        _currentAttrs.Background = (byte)(p - 100 + 8);
                        _currentAttrs.DefaultBackground = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Parse extended color (256-color or 24-bit RGB) from SGR parameters.
        /// </summary>
        private int ParseExtendedColor(int index, ref byte colorOut)
        {
            if (index + 1 >= _csiParams.Count) return index;

            int mode = _csiParams[index + 1];
            if (mode == 5 && index + 2 < _csiParams.Count)
            {
                // 256-color mode: 38;5;n or 48;5;n
                colorOut = (byte)Math.Clamp(_csiParams[index + 2], 0, 255);
                return index + 2;
            }
            else if (mode == 2 && index + 4 < _csiParams.Count)
            {
                // 24-bit RGB mode: 38;2;r;g;b or 48;2;r;g;b
                // Map to nearest 256-color (simplified - use color cube)
                int r = _csiParams[index + 2];
                int g = _csiParams[index + 3];
                int b = _csiParams[index + 4];
                colorOut = RgbTo256Color(r, g, b);
                return index + 4;
            }
            return index + 1;
        }

        /// <summary>
        /// Convert 24-bit RGB to nearest 256-color palette index.
        /// </summary>
        private static byte RgbTo256Color(int r, int g, int b)
        {
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);

            // Standard xterm cube levels.
            ReadOnlySpan<int> levels = [0, 95, 135, 175, 215, 255];

            int ri = NearestLevel(r, levels);
            int gi = NearestLevel(g, levels);
            int bi = NearestLevel(b, levels);

            int cubeR = levels[ri], cubeG = levels[gi], cubeB = levels[bi];
            int cubeIndex = 16 + 36 * ri + 6 * gi + bi;
            int cubeDist = Distance(r, g, b, cubeR, cubeG, cubeB);

            // Candidate grayscale ramp value (232-255 map to 8..238 in steps of 10).
            int avg = (r + g + b) / 3;
            int grayIdx = Math.Clamp((avg - 8 + 5) / 10, 0, 23);
            int grayVal = 8 + grayIdx * 10;
            int grayDist = Distance(r, g, b, grayVal, grayVal, grayVal);

            return grayDist < cubeDist ? (byte)(232 + grayIdx) : (byte)cubeIndex;
        }

        private static int NearestLevel(int value, ReadOnlySpan<int> levels)
        {
            int best = 0;
            int bestDist = int.MaxValue;
            for (int i = 0; i < levels.Length; i++)
            {
                int d = Math.Abs(levels[i] - value);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        private static int Distance(int r1, int g1, int b1, int r2, int g2, int b2)
        {
            int dr = r1 - r2, dg = g1 - g2, db = b1 - b2;
            return dr * dr + dg * dg + db * db;
        }

        // ======================
        // Rendering
        // ======================
        private void UpdateDocument(bool forceScrollTop = false, bool forceFullReplace = false)
        {
            int curRow = _curRow;
            int curCol = _curCol;

            if (!Dispatcher.CheckAccess())
            {
                var lines = BuildLineSnapshot();
                var text = BuildDocumentText(lines);
                Dispatcher.BeginInvoke(() => ApplyFullDocumentUpdate(text, lines, curRow, curCol, forceScrollTop), DispatcherPriority.Render);
                return;
            }

            ApplyIncrementalDocumentUpdate(curRow, curCol, forceScrollTop, forceFullReplace);
        }

        private string[] BuildLineSnapshot()
        {
            int sbCount = VisibleScrollbackCount;
            var lines = new string[sbCount + Rows];
            var chars = new char[Columns];

            for (int i = 0; i < sbCount; i++)
            {
                var row = _scrollback[i];
                int n = Math.Min(Columns, row.Length);

                for (int c = 0; c < n; c++)
                {
                    chars[c] = row[c].Char;
                }

                for (int c = n; c < Columns; c++)
                {
                    chars[c] = ' ';
                }

                lines[i] = new string(chars);
            }

            int bufRows = _buf.GetLength(0);
            int bufCols = _buf.GetLength(1);

            for (int r = 0; r < Rows; r++)
            {
                if (r < bufRows)
                {
                    int n = Math.Min(Columns, bufCols);
                    for (int c = 0; c < n; c++)
                    {
                        chars[c] = _buf[r, c].Char;
                    }

                    for (int c = n; c < Columns; c++)
                    {
                        chars[c] = ' ';
                    }
                }
                else
                {
                    for (int c = 0; c < Columns; c++)
                    {
                        chars[c] = ' ';
                    }
                }

                lines[sbCount + r] = new string(chars);
            }

            return lines;
        }

        private string BuildDocumentText(string[] lines)
        {
            var sb = new StringBuilder((lines.Length * (Columns + 1)) + 1);

            foreach (string line in lines)
            {
                sb.Append(line);
                sb.Append('\n'); // Always append LF, even for the last line.
            }

            return sb.ToString();
        }

        private void ApplyIncrementalDocumentUpdate(int curRow, int curCol, bool forceScrollTop, bool forceFullReplace)
        {
            if (Document == null)
            {
                Document = new TextDocument();
            }

            bool stickToBottom = IsScrolledToBottom();
            var lines = BuildLineSnapshot();
            bool requiresFullReplace = forceScrollTop ||
                                       forceFullReplace ||
                                       _scrollbackDirty ||
                                       Document.LineCount < lines.Length ||
                                       _lineCache.Length != lines.Length ||
                                       (_lineCache.Length > 0 && _lineCache[0].Length != Columns);

            if (requiresFullReplace)
            {
                ApplyFullDocumentUpdate(BuildDocumentText(lines), lines, curRow, curCol, forceScrollTop);
                return;
            }

            bool beganUpdate = false;

            try
            {
                for (int r = 0; r < lines.Length; r++)
                {
                    if (string.Equals(lines[r], _lineCache[r], StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!beganUpdate)
                    {
                        Document.BeginUpdate();
                        beganUpdate = true;
                    }

                    var line = Document.GetLineByNumber(r + 1);
                    Document.Replace(line.Offset, line.Length, lines[r]);
                }
            }
            finally
            {
                if (beganUpdate)
                {
                    Document.EndUpdate();
                }
            }

            _lineCache = lines;
            UpdateCaret(curRow, curCol, forceScrollTop, stickToBottom);
        }

        private void ApplyFullDocumentUpdate(string text, string[] lines, int curRow, int curCol, bool forceScrollTop)
        {
            if (Document == null)
            {
                Document = new TextDocument();
            }

            bool stickToBottom = IsScrolledToBottom();

            Document.BeginUpdate();
            try
            {
                Document.Replace(0, Document.TextLength, text);
            }
            finally
            {
                Document.EndUpdate();
            }

            _lineCache = lines;
            _scrollbackDirty = false;
            UpdateCaret(curRow, curCol, forceScrollTop, stickToBottom);
        }

        private void UpdateCaret(int curRow, int curCol, bool forceScrollTop, bool stickToBottom)
        {
            int offset = VisibleScrollbackCount;
            int targetLine = Math.Max(1, Math.Min(offset + curRow + 1, Document.LineCount));
            var line = Document.GetLineByNumber(targetLine);
            int targetColumn = Math.Max(1, Math.Min(curCol + 1, line.Length + 1)); // allow caret after last char
            var position = TextArea.Caret.Position;

            if (position.Line != targetLine || position.Column != targetColumn)
            {
                TextArea.Caret.Position = new TextViewPosition(targetLine, targetColumn);
            }

            // Keep the header pinned at the very top when explicitly requested (e.g., after Reset).
            if (forceScrollTop)
            {
                ScrollTo(1, 1);
                return;
            }

            // Follow new output only when the view was already at the bottom. If the user has
            // scrolled up into the scrollback history, leave the viewport where they left it.
            if (stickToBottom)
            {
                ScrollToEnd();
            }
        }

        /// <summary>
        /// Returns true when the viewport is at (or within one line of) the bottom of the document,
        /// or when the document is short enough that no scrolling is possible.
        /// </summary>
        private bool IsScrolledToBottom()
        {
            double extent = ExtentHeight;
            double viewport = ViewportHeight;

            if (extent <= 0 || viewport <= 0 || extent <= viewport)
            {
                return true;
            }

            double lineHeight = TextArea.TextView.DefaultLineHeight;
            if (lineHeight <= 0)
            {
                lineHeight = 1;
            }

            return (extent - viewport - VerticalOffset) <= lineHeight + 1;
        }

        #region Context menu command handlers

        private void OnCopyTextCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(this.TextArea.Selection.GetText());
        }

        private void OnCopyTextExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var text = this.TextArea.Selection.GetText();

            if (!string.IsNullOrEmpty(text))
            {
                Clipboard.SetText(text);
            }
        }

        private void OnPasteTextCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsText() && this.Connection is { IsConnected: true };
        }

        private void OnPasteTextExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.Connection is not { IsConnected: true })
            {
                return;
            }

            string? text;

            try
            {
                text = Clipboard.GetText();

                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                // Normalize line endings to CR which is what hosts expect from a terminal.
                text = text.Replace("\r\n", "\r").Replace("\n", "\r");

                if (_bracketedPaste)
                {
                    this.Connection.Send("\x1b[200~" + text + "\x1b[201~");
                }
                else
                {
                    this.Connection.Send(text);
                }
            }
            catch
            {
                return;
            }
        }

        private void OnClearTerminalExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Reset(Rows, Columns);
        }

        #endregion
    }
}
