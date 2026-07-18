/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using BbsNavigator.Models;
using BbsNavigator.Networking;
using BbsNavigator.Transfers;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Themes;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Hosts a VT terminal and owns the lifetime of one BBS connection, including file
    /// transfers, session capture, and live connection statistics.
    /// </summary>
    public partial class BbsTerminalView : UserControl, IAsyncDisposable
    {
        /// <summary>
        /// Matches ANSI/VT escape sequences so captured session logs contain readable text.
        /// </summary>
        private static readonly Regex _escapeSequences = new(
            @"\x1B(\[[0-9;?]*[@-~]|\][^\x07\x1B]*(\x07|\x1B\\)?|[@-Z\\-_])",
            RegexOptions.Compiled);

        private readonly AppSettings _settings;
        private readonly BbsTelnetConnection _connection;
        private readonly CancellationTokenSource _lifetimeCancellation = new();
        private readonly CancellationToken _lifetimeToken;
        private readonly SemaphoreSlim _connectionGate = new(1, 1);
        private readonly object _disposeLock = new();
        private readonly object _captureLock = new();
        private readonly DispatcherTimer _statsTimer;
        private readonly Stopwatch _transferStopwatch = new();
        private Task? _disposeTask;
        private volatile bool _disposed;
        private bool _manualDisconnect;
        private int _reconnectScheduled;
        private int _scrollToEndQueued;
        private volatile bool _transferActive;
        private CancellationTokenSource? _transferCts;
        private DispatcherTimer? _transferUiTimer;
        private StreamWriter? _captureWriter;
        private string _zmodemDetectTail = string.Empty;

        /// <summary>
        /// Initializes a terminal document for the specified profile.
        /// </summary>
        /// <param name="profile">The saved BBS profile to connect to.</param>
        /// <param name="settings">The application settings that supply terminal and transfer options.</param>
        public BbsTerminalView(BbsProfile profile, AppSettings settings)
        {
            InitializeComponent();
            _lifetimeToken = _lifetimeCancellation.Token;
            Profile = profile;
            _settings = settings;
            _connection = new BbsTelnetConnection(profile.Host, profile.Port)
            {
                Encoding = profile.TerminalEncoding.ToEncoding(),
                KeepAliveInterval = TimeSpan.FromSeconds(Math.Max(0, settings.KeepAliveSeconds))
            };
            _connection.ConnectionLost += Connection_OnConnectionLost;
            Terminal.Connection = _connection;
            _connection.DataReceived += Connection_OnDataReceived;
            Terminal.FontSize = settings.FontSize;
            DataContext = profile;
            Loaded += BbsTerminalView_OnLoaded;
            Terminal.PreviewMouseWheel += Terminal_OnPreviewMouseWheel;

            ProtocolComboBox.ItemsSource = Enum.GetValues<TransferProtocol>();
            ProtocolComboBox.SelectedItem = settings.DefaultTransferProtocol;
            EncodingText.Text = profile.TerminalEncoding switch
            {
                BbsEncoding.Cp437 => "CP437",
                BbsEncoding.Latin1 => "Latin-1",
                _ => "UTF-8"
            };

            _statsTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statsTimer.Tick += StatsTimer_OnTick;
            _statsTimer.Start();

            UpdateStatus(BbsConnectionState.Disconnected, $"Ready to connect to {profile.Endpoint}");
        }

        /// <summary>
        /// Gets the saved profile represented by this document.
        /// </summary>
        public BbsProfile Profile { get; }

        /// <summary>
        /// Gets whether a file transfer is currently running.
        /// </summary>
        public bool IsTransferActive => _transferActive;

        /// <summary>
        /// Gets the transfer protocol selected in this session's status bar.
        /// </summary>
        private TransferProtocol SelectedProtocol =>
            ProtocolComboBox.SelectedItem is TransferProtocol protocol ? protocol : _settings.DefaultTransferProtocol;

        #region Connection lifecycle

        /// <summary>
        /// Attempts to establish the BBS connection.
        /// </summary>
        /// <param name="reconnecting">Whether this is an automatic reconnection attempt.</param>
        public async Task ConnectAsync(bool reconnecting = false)
        {
            if (_disposed)
            {
                return;
            }

            bool gateEntered = false;
            try
            {
                await _connectionGate.WaitAsync(_lifetimeToken);
                gateEntered = true;

                if (_disposed || _connection.IsConnected)
                {
                    return;
                }

                _manualDisconnect = false;
                UpdateStatus(
                    reconnecting ? BbsConnectionState.Reconnecting : BbsConnectionState.Connecting,
                    reconnecting ? $"Reconnecting to {Profile.Endpoint}…" : $"Connecting to {Profile.Endpoint}…");

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_settings.ConnectTimeoutSeconds, 1, 300)));

                await _connection.ConnectAsync(timeoutCts.Token);
                UpdateStatus(BbsConnectionState.Connected, $"Connected to {Profile.Endpoint}");
                Terminal.Focus();
                Profile.LastConnected = DateTime.Now;
                Profile.ConnectionCount++;
            }
            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
            {
            }
            catch (OperationCanceledException)
            {
                UpdateStatus(BbsConnectionState.Faulted, $"The connection attempt to {Profile.Endpoint} timed out.");
                ScheduleReconnect();
            }
            catch (ObjectDisposedException) when (_disposed)
            {
            }
            catch (Exception ex)
            {
                UpdateStatus(BbsConnectionState.Faulted, DescribeConnectError(ex));
                ScheduleReconnect();
            }
            finally
            {
                if (gateEntered)
                {
                    _connectionGate.Release();
                }
            }
        }

        /// <summary>
        /// Produces a friendly description for a failed connection attempt.
        /// </summary>
        private string DescribeConnectError(Exception ex)
        {
            return ex switch
            {
                System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.HostNotFound } =>
                    $"The host name '{Profile.Host}' could not be resolved.",
                System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.ConnectionRefused } =>
                    $"{Profile.Endpoint} refused the connection.",
                System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.TimedOut } =>
                    $"The connection attempt to {Profile.Endpoint} timed out.",
                _ => ex.Message
            };
        }

        private async void BbsTerminalView_OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= BbsTerminalView_OnLoaded;

            if (FindResource("ViewFadeIn") is Storyboard fadeIn)
            {
                BeginStoryboard(fadeIn);
            }

            UpdateStatus(BbsConnectionState.Connecting, $"Connecting to {Profile.Endpoint}…");
            await Dispatcher.Yield(DispatcherPriority.Background);
            await ConnectAsync();
        }

        private void Connection_OnConnectionLost(object? sender, Exception? exception)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_disposed || _manualDisconnect)
                {
                    return;
                }

                UpdateStatus(
                    BbsConnectionState.Faulted,
                    exception?.Message ?? "The remote system closed the connection.");
                ScheduleReconnect();
            });
        }

        private void Connection_OnDataReceived(object? sender, string data)
        {
            if (_disposed)
            {
                return;
            }

            WriteCapture(data);
            DetectZmodemStart(data);

            if (Interlocked.Exchange(ref _scrollToEndQueued, 1) != 0)
            {
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (!_disposed)
                    {
                        Terminal.ScrollToEnd();
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _scrollToEndQueued, 0);
                }
            }, DispatcherPriority.Render);
        }

        private void ScheduleReconnect()
        {
            if (!Profile.AutoReconnect || _manualDisconnect || _disposed)
            {
                return;
            }

            if (Interlocked.Exchange(ref _reconnectScheduled, 1) != 0)
            {
                return;
            }

            _ = ReconnectAfterDelayAsync();
        }

        private async Task ReconnectAfterDelayAsync()
        {
            int delaySeconds = Math.Max(1, _settings.ReconnectDelaySeconds);

            try
            {
                UpdateStatus(BbsConnectionState.Reconnecting, $"Reconnecting in {delaySeconds} seconds…");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _lifetimeToken);
                await ConnectAsync(reconnecting: true);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Interlocked.Exchange(ref _reconnectScheduled, 0);

                if (!_connection.IsConnected && !_manualDisconnect && !_disposed)
                {
                    ScheduleReconnect();
                }
            }
        }

        private async void Reconnect_OnClick(object sender, RoutedEventArgs e)
        {
            await ConnectAsync(reconnecting: true);
        }

        private async void Disconnect_OnClick(object sender, RoutedEventArgs e)
        {
            await DisconnectAsync();
        }

        /// <summary>
        /// Closes the connection at the user's request, canceling any active transfer.
        /// </summary>
        private async Task DisconnectAsync()
        {
            if (_disposed)
            {
                return;
            }

            _transferCts?.Cancel();

            bool gateEntered = false;
            try
            {
                await _connectionGate.WaitAsync(_lifetimeToken);
                gateEntered = true;

                if (_disposed)
                {
                    return;
                }

                _manualDisconnect = true;
                Interlocked.Exchange(ref _reconnectScheduled, 0);
                await _connection.DisconnectAsync();

                if (!_disposed)
                {
                    UpdateStatus(BbsConnectionState.Disconnected, $"Disconnected from {Profile.Endpoint}");
                }
            }
            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (_disposed)
            {
            }
            finally
            {
                if (gateEntered)
                {
                    _connectionGate.Release();
                }
            }
        }

        private void UpdateStatus(BbsConnectionState state, string message)
        {
            Profile.ConnectionState = state;
            StatusText.Text = message;
            StatusBadge.Text = state.ToString();
            BusyRing.IsActive = state is BbsConnectionState.Connecting or BbsConnectionState.Reconnecting;
            ReconnectButton.IsEnabled = state is not BbsConnectionState.Connecting and not BbsConnectionState.Reconnecting;
            DisconnectButton.IsEnabled = state == BbsConnectionState.Connected;
            StatusBadge.Background = FindResource(state switch
            {
                BbsConnectionState.Connected => MosaicTheme.SuccessBrush,
                BbsConnectionState.Faulted => MosaicTheme.ErrorBrush,
                BbsConnectionState.Connecting or BbsConnectionState.Reconnecting => MosaicTheme.WarningBrush,
                _ => MosaicTheme.ControlBorderBrush
            }) as System.Windows.Media.Brush;

            UpdateTransferButtons();

            if (FindResource("StatusPulse") is Storyboard pulse)
            {
                BeginStoryboard(pulse);
            }
        }

        /// <summary>
        /// Shows an informational message in the banner without changing the connection state.
        /// </summary>
        private void ShowTransientStatus(string message)
        {
            StatusText.Text = message;

            if (FindResource("StatusPulse") is Storyboard pulse)
            {
                BeginStoryboard(pulse);
            }
        }

        private void UpdateTransferButtons()
        {
            bool enabled = _connection.IsConnected && !_transferActive;
            UploadButton.IsEnabled = enabled;
            DownloadButton.IsEnabled = enabled;
            ProtocolComboBox.IsEnabled = !_transferActive;
        }

        #endregion

        #region Statistics and zoom

        private void StatsTimer_OnTick(object? sender, EventArgs e)
        {
            if (_disposed)
            {
                return;
            }

            TrafficText.Text = $"↓ {FormatBytes(_connection.BytesReceived)}   ↑ {FormatBytes(_connection.BytesSent)}";

            DateTime? connectedAt = _connection.ConnectedAtUtc;
            DurationText.Text = connectedAt.HasValue
                ? (DateTime.UtcNow - connectedAt.Value).ToString(@"hh\:mm\:ss")
                : "--:--:--";
        }

        /// <summary>
        /// Formats a byte count using the most readable unit.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
                < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
                _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB"
            };
        }

        private void Terminal_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                return;
            }

            double newSize = Math.Clamp(Terminal.FontSize + (e.Delta > 0 ? 1 : -1), 8, 32);
            Terminal.FontSize = newSize;
            _settings.FontSize = newSize;
            e.Handled = true;
        }

        #endregion

        #region Session capture

        private void CaptureToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string logFolder = Path.Combine(_settings.ResolveDownloadFolder(), "Logs");
                Directory.CreateDirectory(logFolder);
                string safeName = string.Join("_", Profile.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
                string path = Path.Combine(logFolder, $"{safeName} {DateTime.Now:yyyy-MM-dd HHmmss}.log");

                lock (_captureLock)
                {
                    _captureWriter = new StreamWriter(path, append: false) { AutoFlush = true };
                }

                CaptureDot.Foreground = FindResource(MosaicTheme.ErrorBrush) as System.Windows.Media.Brush;
                CaptureDot.Opacity = 1.0;
                ShowTransientStatus($"Capturing session to {path}");
            }
            catch (Exception ex)
            {
                CaptureToggle.IsChecked = false;
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    $"The capture file could not be created.\n\n{ex.Message}",
                    "Session Capture",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void CaptureToggle_OnUnchecked(object sender, RoutedEventArgs e)
        {
            StopCapture();
            CaptureDot.ClearValue(TextBlock.ForegroundProperty);
            CaptureDot.Opacity = 0.6;
            ShowTransientStatus("Session capture stopped.");
        }

        /// <summary>
        /// Toggles session capture; used by the main window menu.
        /// </summary>
        public void ToggleCapture()
        {
            CaptureToggle.IsChecked = CaptureToggle.IsChecked != true;
        }

        private void StopCapture()
        {
            lock (_captureLock)
            {
                _captureWriter?.Dispose();
                _captureWriter = null;
            }
        }

        /// <summary>
        /// Appends received text to the capture log with escape sequences removed. Runs on
        /// the socket thread.
        /// </summary>
        private void WriteCapture(string data)
        {
            if (_captureWriter == null)
            {
                return;
            }

            try
            {
                string text = _escapeSequences.Replace(data, string.Empty).Replace("\r", string.Empty);

                lock (_captureLock)
                {
                    _captureWriter?.Write(text);
                }
            }
            catch (Exception)
            {
                // Disk trouble must never take down the session; stop capturing instead.
                StopCapture();
                Dispatcher.BeginInvoke(() => CaptureToggle.IsChecked = false);
            }
        }

        #endregion

        #region File transfers

        /// <summary>
        /// Watches the incoming text stream for a ZMODEM start signature so downloads can
        /// begin automatically and uploads can be suggested. Runs on the socket thread.
        /// </summary>
        private void DetectZmodemStart(string data)
        {
            if (_transferActive)
            {
                return;
            }

            string combined = _zmodemDetectTail + data;
            _zmodemDetectTail = combined.Length > 6 ? combined[^6..] : combined;

            // "*\x18B00" announces ZRQINIT (the remote is about to send files);
            // "*\x18B01" announces ZRINIT (the remote is waiting to receive files).
            if (combined.Contains("B00", StringComparison.Ordinal))
            {
                _zmodemDetectTail = string.Empty;

                if (_settings.AutoStartZmodemDownloads)
                {
                    Dispatcher.BeginInvoke(async () =>
                    {
                        if (!_transferActive && !_disposed)
                        {
                            ShowTransientStatus("ZMODEM download detected — receiving…");
                            await RunTransferAsync(TransferDirection.Download, TransferProtocol.Zmodem, interactive: false);
                        }
                    });
                }
                else
                {
                    Dispatcher.BeginInvoke(() => ShowTransientStatus("The remote system started a ZMODEM send. Click Download to receive."));
                }
            }
            else if (combined.Contains("B01", StringComparison.Ordinal))
            {
                _zmodemDetectTail = string.Empty;
                Dispatcher.BeginInvoke(() => ShowTransientStatus("The remote system is ready to receive — click Upload to send files."));
            }
        }

        private async void Download_OnClick(object sender, RoutedEventArgs e)
        {
            await RunTransferAsync(TransferDirection.Download, SelectedProtocol, interactive: true);
        }

        private async void Upload_OnClick(object sender, RoutedEventArgs e)
        {
            await RunTransferAsync(TransferDirection.Upload, SelectedProtocol, interactive: true);
        }

        /// <summary>
        /// Starts a download with the currently selected protocol; used by the main window menu.
        /// </summary>
        public Task StartDownloadAsync() => RunTransferAsync(TransferDirection.Download, SelectedProtocol, interactive: true);

        /// <summary>
        /// Starts an upload with the currently selected protocol; used by the main window menu.
        /// </summary>
        public Task StartUploadAsync() => RunTransferAsync(TransferDirection.Upload, SelectedProtocol, interactive: true);

        private void TransferCancel_OnClick(object sender, RoutedEventArgs e)
        {
            _transferCts?.Cancel();
            TransferCancelButton.IsEnabled = false;
            TransferTitle.Text = "Canceling…";
        }

        /// <summary>
        /// Runs one upload or download session end to end: prompts for files, takes over
        /// the byte stream, drives the protocol engine, and restores the terminal.
        /// </summary>
        /// <param name="direction">Whether files are being sent or received.</param>
        /// <param name="protocol">The protocol to use.</param>
        /// <param name="interactive">Whether the transfer was requested by the user (shows pickers and result dialogs).</param>
        private async Task RunTransferAsync(TransferDirection direction, TransferProtocol protocol, bool interactive)
        {
            if (_transferActive || _disposed || !_connection.IsConnected)
            {
                return;
            }

            // Gather everything that needs user input before touching the connection.
            List<string>? uploadFiles = null;
            string downloadFolder = _settings.ResolveDownloadFolder();
            string xmodemFileName = "download.bin";

            if (direction == TransferDirection.Upload)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Choose files to upload",
                    Multiselect = protocol is TransferProtocol.Zmodem or TransferProtocol.Ymodem,
                    CheckFileExists = true
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                uploadFiles = dialog.FileNames.ToList();
            }
            else if (protocol is TransferProtocol.Xmodem or TransferProtocol.XmodemCrc or TransferProtocol.Xmodem1K)
            {
                // XMODEM does not transmit a file name, so ask where to save.
                var dialog = new SaveFileDialog
                {
                    Title = "Save downloaded file as",
                    InitialDirectory = downloadFolder,
                    FileName = "download.bin"
                };

                if (dialog.ShowDialog(Window.GetWindow(this)) != true)
                {
                    return;
                }

                downloadFolder = Path.GetDirectoryName(dialog.FileName) ?? downloadFolder;
                xmodemFileName = Path.GetFileName(dialog.FileName);
            }

            _transferActive = true;
            _transferCts = new CancellationTokenSource();
            Terminal.SendKeyboardInputToConnection = false;
            UpdateTransferButtons();
            ShowTransferPanel(direction, protocol);

            var progressSink = new LatestProgressSink();
            _transferStopwatch.Restart();

            _transferUiTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _transferUiTimer.Tick += (_, _) => RenderTransferProgress(progressSink);
            _transferUiTimer.Start();

            string resultMessage;
            bool succeeded = false;

            try
            {
                using TelnetBinaryChannel channel = _connection.EnterBinaryMode();
                CancellationToken token = _transferCts.Token;

                TransferResult result = await Task.Run(() => direction == TransferDirection.Download
                    ? protocol == TransferProtocol.Zmodem
                        ? new ZmodemReceiver().ReceiveAsync(channel, downloadFolder, progressSink, token)
                        : new XymodemReceiver(protocol).ReceiveAsync(channel, downloadFolder, xmodemFileName, progressSink, token)
                    : protocol == TransferProtocol.Zmodem
                        ? new ZmodemSender().SendAsync(channel, uploadFiles!, progressSink, token)
                        : new XymodemSender(protocol).SendAsync(channel, uploadFiles!, progressSink, token));

                succeeded = true;
                string verb = direction == TransferDirection.Download ? "Received" : "Sent";
                string fileSummary = result.Files.Count == 1
                    ? $"'{result.Files[0]}'"
                    : $"{result.Files.Count} files";
                resultMessage = $"{verb} {fileSummary} ({FormatBytes(result.TotalBytes)}) in {result.Elapsed.TotalSeconds:0.#}s.";
            }
            catch (OperationCanceledException)
            {
                resultMessage = "The transfer was canceled.";
            }
            catch (TransferException ex)
            {
                resultMessage = $"The transfer failed: {ex.Message}";
            }
            catch (InvalidOperationException ex)
            {
                resultMessage = $"The transfer could not start: {ex.Message}";
            }
            catch (IOException ex)
            {
                resultMessage = $"A file error interrupted the transfer: {ex.Message}";
            }
            catch (Exception ex)
            {
                resultMessage = $"An unexpected error interrupted the transfer: {ex.Message}";
            }
            finally
            {
                _transferUiTimer?.Stop();
                _transferUiTimer = null;
                _transferCts?.Dispose();
                _transferCts = null;
                _transferActive = false;

                if (!_disposed)
                {
                    Terminal.SendKeyboardInputToConnection = true;
                    HideTransferPanel();
                    UpdateTransferButtons();
                    Terminal.Focus();
                }
            }

            if (_disposed)
            {
                return;
            }

            ShowTransientStatus(resultMessage);

            if (interactive && !succeeded)
            {
                Mosaic.UI.Wpf.Controls.MessageBox.Show(
                    resultMessage,
                    "File Transfer",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Renders the most recent progress snapshot into the transfer card.
        /// </summary>
        private void RenderTransferProgress(LatestProgressSink sink)
        {
            if (!sink.TryRead(out TransferSnapshot snapshot))
            {
                return;
            }

            TransferTitle.Text = string.IsNullOrEmpty(snapshot.FileName)
                ? snapshot.Message
                : $"{snapshot.Message} {snapshot.FileName}  (file {snapshot.FileIndex})";

            if (snapshot.FileSize > 0)
            {
                TransferProgressBar.IsIndeterminate = false;
                TransferProgressBar.Value = Math.Min(100, snapshot.BytesTransferred * 100.0 / snapshot.FileSize);
                TransferDetailText.Text = $"{FormatBytes(snapshot.BytesTransferred)} of {FormatBytes(snapshot.FileSize)}";
            }
            else
            {
                TransferProgressBar.IsIndeterminate = snapshot.BytesTransferred == 0;
                TransferProgressBar.Value = 0;
                TransferDetailText.Text = FormatBytes(snapshot.BytesTransferred);
            }

            double seconds = _transferStopwatch.Elapsed.TotalSeconds;
            if (seconds > 0.5 && snapshot.BytesTransferred > 0)
            {
                double cps = snapshot.BytesTransferred / seconds;
                string eta = snapshot.FileSize > 0 && cps > 1
                    ? $" — {TimeSpan.FromSeconds((snapshot.FileSize - snapshot.BytesTransferred) / cps):mm\\:ss} left"
                    : string.Empty;
                TransferStatsText.Text = $"{FormatBytes((long)cps)}/s{eta}";
            }
        }

        private void ShowTransferPanel(TransferDirection direction, TransferProtocol protocol)
        {
            TransferTitle.Text = direction == TransferDirection.Download
                ? "Waiting for the remote system to send…"
                : "Waiting for the remote system to receive…";
            TransferDetailText.Text = protocol.ToString().ToUpperInvariant();
            TransferStatsText.Text = string.Empty;
            TransferProgressBar.Value = 0;
            TransferProgressBar.IsIndeterminate = true;
            TransferCancelButton.IsEnabled = true;
            TransferPanel.Visibility = Visibility.Visible;

            if (FindResource("TransferPanelShow") is Storyboard show)
            {
                BeginStoryboard(show);
            }
        }

        private void HideTransferPanel()
        {
            if (FindResource("TransferPanelHide") is Storyboard hide)
            {
                BeginStoryboard(hide);
            }
            else
            {
                TransferPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void TransferPanelHide_OnCompleted(object? sender, EventArgs e)
        {
            if (!_transferActive)
            {
                TransferPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// A lock-free progress sink that keeps only the most recent snapshot; the UI
        /// renders it on a timer so high-rate protocol progress cannot flood the dispatcher.
        /// </summary>
        private sealed class LatestProgressSink : IProgress<TransferSnapshot>
        {
            private readonly object _gate = new();
            private TransferSnapshot _snapshot;
            private bool _hasValue;

            /// <inheritdoc />
            public void Report(TransferSnapshot value)
            {
                lock (_gate)
                {
                    _snapshot = value;
                    _hasValue = true;
                }
            }

            /// <summary>
            /// Reads the latest snapshot if one has arrived since the last read.
            /// </summary>
            public bool TryRead(out TransferSnapshot snapshot)
            {
                lock (_gate)
                {
                    snapshot = _snapshot;
                    bool had = _hasValue;
                    _hasValue = false;
                    return had;
                }
            }
        }

        #endregion

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            lock (_disposeLock)
            {
                return new ValueTask(_disposeTask ??= DisposeCoreAsync());
            }
        }

        private async Task DisposeCoreAsync()
        {
            _disposed = true;
            _manualDisconnect = true;
            _transferCts?.Cancel();
            _lifetimeCancellation.Cancel();
            _statsTimer.Stop();
            _transferUiTimer?.Stop();
            StopCapture();
            _connection.ConnectionLost -= Connection_OnConnectionLost;
            _connection.DataReceived -= Connection_OnDataReceived;
            Terminal.Connection = null;

            await _connectionGate.WaitAsync();
            try
            {
                await _connection.DisposeAsync();
            }
            finally
            {
                _connectionGate.Release();
                _connectionGate.Dispose();
                _lifetimeCancellation.Dispose();
                Profile.ConnectionState = BbsConnectionState.Disconnected;
            }
        }
    }
}
