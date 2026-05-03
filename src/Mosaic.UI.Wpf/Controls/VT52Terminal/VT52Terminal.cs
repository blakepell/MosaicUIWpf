using ICSharpCode.AvalonEdit;
using TextDocument = ICSharpCode.AvalonEdit.Document.TextDocument;

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// VT52 terminal emulator hosted inside an AvalonEdit TextEditor.
    /// Call Add(string) or Add(byte[]) with remote data; call SendKey/SendString for local keystrokes.
    /// Subscribe to Transmit to get bytes that the terminal sends back (e.g., Identify response).
    /// </summary>
    public class VT52Terminal : TextEditor
    {
        // Screen geometry & buffer
        public int Rows { get; private set; }
        public int Columns { get; private set; }
        private TerminalCell[,] _buf = new TerminalCell[1, 1];
        private int _curRow;
        private int _curCol;

        // Current text attributes
        private TerminalAttributes _currentAttrs = TerminalAttributes.Default;

        // Expose buffer for colorizer
        internal TerminalCell[,] Buffer => _buf;
        internal int BufferRows => Rows;
        internal int BufferColumns => Columns;

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

        // Modes
        private bool _originMode = false; // DECOM - cursor relative to scroll region
        private bool _autowrapPending = false; // Deferred autowrap

        private readonly object _lock = new();
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

        /// <summary>Fired when the terminal needs to transmit bytes back to the host (e.g., ESC / Z identify).</summary>
        public event Action<byte[]?>? Transmit;

        /// <summary>Fired when BEL (^G) is received.</summary>
        public event Action? Bell;

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

            this.TextArea.TextView.BackgroundRenderers.Add(new BlockCaretRenderer(this.TextArea));
            this.TextArea.TextView.LineTransformers.Add(new VT52Colorizer(this));

            Reset(24, 80);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += (_, __) => OnSizeChanged();
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewTextInput += OnPreviewTextInput;

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
            Dispatcher.Invoke(OnSizeChanged, DispatcherPriority.Background);

            if (AutoConnect && Connection is { IsConnected: false })
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
            if (Dispatcher.CheckAccess())
            {
                Add(data);
                return;
            }

            Dispatcher.BeginInvoke(() => Add(data), DispatcherPriority.Background);
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

        private static string? GetKeySequence(KeyEventArgs e)
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

            return key switch
            {
                Key.Up => "\x1b[A",
                Key.Down => "\x1b[B",
                Key.Left => "\x1b[D",
                Key.Right => "\x1b[C",
                Key.Home => "\x1b[H",
                Key.End => "\x1b[F",
                Key.Insert => "\x1b[2~",
                Key.Delete => "\x1b[3~",
                Key.PageUp => "\x1b[5~",
                Key.PageDown => "\x1b[6~",
                Key.Enter or Key.Return => "\r",
                Key.Back => "\x7f",
                Key.Tab => "\t",
                Key.Escape => "\x1b",
                _ => null
            };
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

            if (sequence != null)
            {
                return true;
            }

            return false;
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

            try
            {
                connection.Send(text);
                EchoLocal(localEchoText);
                return true;
            }
            catch (Exception ex)
            {
                OnConnectionError(ex);
                return false;
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

            try
            {
                Connection.Send(data);
            }
            catch (Exception ex)
            {
                OnConnectionError(ex);
            }
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
                _altBuf = null;
                _useAltBuffer = false;
                _csiParams.Clear();
                _csiCurrentParam = 0;
                _csiHasParam = false;
                _csiPrivateMarker = '\0';
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
                _curRow = Math.Min(_curRow, Rows - 1);
                _curCol = Math.Min(_curCol, Columns - 1);

                // Adjust scroll region to new size
                _scrollTop = Math.Min(_scrollTop, Rows);
                _scrollBottom = Rows; // Reset to full screen on resize

                UpdateDocument();
            }
        }


        public event Action<int, int, int, int>? TerminalResized; // rows, cols, pxW, pxH

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

        public void Add(byte[]? data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            lock (_lock)
            {
                foreach (var b in data)
                {
                    ProcessChar((char)(b & 0xFF));
                }
                // Do not reset parser state here; allow incomplete sequences to continue across calls
                UpdateDocument();
            }
        }

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
                    if (ch == '\x07' || ch == '\x1B') // BEL or ESC terminates OSC
                    {
                        // OSC string complete - we can ignore it for now (window titles, etc.)
                        _oscBuffer.Clear();
                        _state = ch == '\x1B' ? ParseState.Esc : ParseState.Normal;
                    }
                    else
                    {
                        _oscBuffer.Append(ch);
                    }
                    return;
                case ParseState.EscScs:
                    // Consume the character set designator (B=ASCII, 0=DEC Special Graphics, etc.)
                    // We don't actually switch character sets, just consume the byte
                    _state = ParseState.Normal;
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
                case '\t': // TAB every 8 cols (paint spaces over the span)
                    {
                        _autowrapPending = false;
                        int next = ((_curCol / 8) + 1) * 8;
                        int target = Math.Min(next, Columns);
                        while (_curCol < target)
                        {
                            _buf[_curRow, _curCol] = new TerminalCell(' ', _currentAttrs);
                            if (_curCol == Columns - 1)
                            {
                                // behave like printing a space at the margin
                                if (_curRow < Rows - 1)
                                {
                                    _curRow++;
                                }
                                else
                                {
                                    ScrollUpRegion();
                                }

                                _curCol = 0;
                                break;
                            }
                            _curCol++;
                        }
                        return;
                    }
                case '\n':
                    LineFeed();
                    return; // LF
                case '\v':
                    LineFeed();
                    return; // VT
                case '\f':
                    LineFeed();
                    return; // FF
                case '\r':
                    _curCol = 0;
                    _autowrapPending = false;
                    _state = ParseState.Normal;
                    return; // CR
                case '\0':
                    return; // NUL
                default: return;
            }
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
                case '7': // VT100: Save Cursor
                    _savedCurRow = _curRow;
                    _savedCurCol = _curCol;
                    break;
                case '8': // VT100: Restore Cursor
                    _curRow = _savedCurRow;
                    _curCol = _savedCurCol;
                    break;
                case 'c': // RIS - Full Reset
                    Reset(Rows, Columns);
                    break;
                case '=': // DECKPAM - Keypad Application Mode (ignore)
                case '>': // DECKPNM - Keypad Numeric Mode (ignore)
                    break;
                case '(': // SCS G0 - Select Character Set
                case ')': // SCS G1
                case '*': // SCS G2
                case '+': // SCS G3
                case '-': // SCS G1 (VT300)
                case '.': // SCS G2 (VT300)
                case '/': // SCS G3 (VT300)
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
                    _curCol = Math.Max(0, Math.Min((p0 > 0 ? p0 : 1) - 1, Columns - 1));
                    break;
                case 'H': // CUP - Cursor Position
                case 'f': // HVP - same as CUP
                    CursorPositionCsi(p0 > 0 ? p0 : 1, p1 > 0 ? p1 : 1);
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
                case 'd': // VPA - Vertical Position Absolute
                    _curRow = Math.Max(0, Math.Min((p0 > 0 ? p0 : 1) - 1, Rows - 1));
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
                case 'h': // SM - Set Mode
                case 'l': // RM - Reset Mode
                    // Standard modes (not private) - ignore for now
                    break;
                case 'n': // DSR - Device Status Report
                    if (p0 == 6) // Report Cursor Position
                    {
                        // Send CPR response: ESC [ row ; col R
                        var response = $"\x1B[{_curRow + 1};{_curCol + 1}R";
                        TransmitToHost(System.Text.Encoding.ASCII.GetBytes(response));
                    }
                    break;
                case 'c': // DA - Device Attributes
                    // Report as VT100
                    TransmitToHost(System.Text.Encoding.ASCII.GetBytes("\x1B[?1;0c"));
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

        private void SetDecPrivateMode(int mode, bool set)
        {
            switch (mode)
            {
                case 1: // DECCKM - Cursor Keys Mode (application vs normal) - affects key sending
                    break;
                case 6: // DECOM - Origin Mode
                    _originMode = set;
                    CursorPositionCsi(1, 1); // Move to origin
                    break;
                case 7: // DECAWM - Autowrap Mode - we always autowrap
                    break;
                case 25: // DECTCEM - Text Cursor Enable Mode (show/hide cursor)
                    // Could control caret visibility if needed
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
                        }
                    }
                    break;
                case 2004: // Bracketed Paste Mode - ignore
                    break;
            }
        }

        private void Printable(char ch)
        {
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

            _buf[_curRow, _curCol] = new TerminalCell(ch, _currentAttrs);

            if (_curCol == Columns - 1)
            {
                // Deferred autowrap - don't move cursor yet
                _autowrapPending = true;
                return;
            }

            _curCol++;
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

            for (int r = top; r < bottom; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    _buf[r, c] = _buf[r + 1, c];
                }
            }

            for (int c = 0; c < Columns; c++)
            {
                _buf[bottom, c] = blankCell;
            }
        }

        private void ScrollDownRegion()
        {
            // Scroll down within scroll region only
            int top = _scrollTop - 1;
            int bottom = _scrollBottom - 1;
            var blankCell = new TerminalCell(' ', TerminalAttributes.Default);

            for (int r = bottom; r > top; r--)
            {
                for (int c = 0; c < Columns; c++)
                {
                    _buf[r, c] = _buf[r - 1, c];
                }
            }

            for (int c = 0; c < Columns; c++)
            {
                _buf[top, c] = blankCell;
            }
        }

        private void ScrollUp()
        {
            var blankCell = new TerminalCell(' ', TerminalAttributes.Default);
            for (int r = 0; r < Rows - 1; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    _buf[r, c] = _buf[r + 1, c];
                }
            }

            for (int c = 0; c < Columns; c++)
            {
                _buf[Rows - 1, c] = blankCell;
            }
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
                case 2: // Erase entire screen
                case 3: // Erase entire screen + scrollback (treat as 2)
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
            for (int i = 0; i < count; i++)
            {
                // Shift lines down
                for (int r = bottom; r > _curRow; r--)
                {
                    for (int c = 0; c < Columns; c++)
                    {
                        _buf[r, c] = _buf[r - 1, c];
                    }
                }
                // Clear current line
                for (int c = 0; c < Columns; c++)
                {
                    _buf[_curRow, c] = blankCell;
                }
            }
            _curCol = 0;
        }

        private void DeleteLines(int count)
        {
            // Delete lines at cursor, pulling up lines from below
            // Only works within scroll region
            if (_curRow < _scrollTop - 1 || _curRow > _scrollBottom - 1) return;

            var blankCell = new TerminalCell(' ', _currentAttrs);
            int bottom = _scrollBottom - 1;
            for (int i = 0; i < count; i++)
            {
                // Shift lines up
                for (int r = _curRow; r < bottom; r++)
                {
                    for (int c = 0; c < Columns; c++)
                    {
                        _buf[r, c] = _buf[r + 1, c];
                    }
                }
                // Clear bottom line
                for (int c = 0; c < Columns; c++)
                {
                    _buf[bottom, c] = blankCell;
                }
            }
            _curCol = 0;
        }

        private void DeleteChars(int count)
        {
            // Delete characters at cursor position, shifting rest of line left
            var blankCell = new TerminalCell(' ', _currentAttrs);
            for (int i = 0; i < count; i++)
            {
                for (int c = _curCol; c < Columns - 1; c++)
                {
                    _buf[_curRow, c] = _buf[_curRow, c + 1];
                }
                _buf[_curRow, Columns - 1] = blankCell;
            }
        }

        private void InsertChars(int count)
        {
            // Insert blank characters at cursor position, shifting rest of line right
            var blankCell = new TerminalCell(' ', _currentAttrs);
            for (int i = 0; i < count; i++)
            {
                for (int c = Columns - 1; c > _curCol; c--)
                {
                    _buf[_curRow, c] = _buf[_curRow, c - 1];
                }
                _buf[_curRow, _curCol] = blankCell;
            }
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
                for (int c = col; c < col + width; c++)
                {
                    _buf[r, c] = cell;
                }
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
                        break;
                    case 38: // Extended foreground color
                        i = ParseExtendedColor(i, ref _currentAttrs.Foreground);
                        break;
                    case 39: // Default foreground
                        _currentAttrs.Foreground = 7;
                        break;

                    // Standard background colors (40-47)
                    case >= 40 and <= 47:
                        _currentAttrs.Background = (byte)(p - 40);
                        break;
                    case 48: // Extended background color
                        i = ParseExtendedColor(i, ref _currentAttrs.Background);
                        break;
                    case 49: // Default background
                        _currentAttrs.Background = 0;
                        break;

                    // Bright foreground colors (90-97)
                    case >= 90 and <= 97:
                        _currentAttrs.Foreground = (byte)(p - 90 + 8);
                        break;

                    // Bright background colors (100-107)
                    case >= 100 and <= 107:
                        _currentAttrs.Background = (byte)(p - 100 + 8);
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

            // Check grayscale ramp (232-255)
            if (r == g && g == b)
            {
                if (r < 8) return 16; // black
                if (r > 248) return 231; // white
                return (byte)(232 + (r - 8) / 10);
            }

            // Map to 6x6x6 color cube (16-231)
            int ri = (r < 48) ? 0 : (r < 115) ? 1 : (r - 35) / 40;
            int gi = (g < 48) ? 0 : (g < 115) ? 1 : (g - 35) / 40;
            int bi = (b < 48) ? 0 : (b < 115) ? 1 : (b - 35) / 40;
            return (byte)(16 + 36 * ri + 6 * gi + bi);
        }

        private readonly StringBuilder _updateSb = new();

        // ======================
        // Rendering
        // ======================
        private void UpdateDocument(bool forceScrollTop = false)
        {
            _updateSb.Clear();
            _updateSb.EnsureCapacity((Rows * (Columns + 1)) + 1);
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    _updateSb.Append(_buf[r, c].Char);
                }

                _updateSb.Append('\n'); // Always append LF, even for the last line
            }

            var text = _updateSb.ToString();

            // Capture state for the Apply closure (safe if dispatched to UI thread later)
            int curRow = _curRow;
            int curCol = _curCol;

            void Apply()
            {
                if (Document == null)
                {
                    Document = new TextDocument();
                }

                Document.BeginUpdate();
                try
                {
                    SelectionStart = 0;
                    Document.Replace(0, Document.TextLength, text);
                }
                finally
                {
                    Document.EndUpdate();
                }

                // Caret placement using 1-based line/column
                int targetLine = Math.Max(1, Math.Min(curRow + 1, Document.LineCount));
                var line = Document.GetLineByNumber(targetLine);
                int targetColumn = Math.Max(1, Math.Min(curCol + 1, line.Length + 1)); // allow caret after last char

                TextArea.Caret.Position = new TextViewPosition(targetLine, targetColumn);

                // Standard caret visibility
                TextArea.Caret.BringCaretToView();

                // Keep nano header visible at the very top when we're on it
                if (forceScrollTop)
                {
                    ScrollTo(1, 1);
                }
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
    }
}
