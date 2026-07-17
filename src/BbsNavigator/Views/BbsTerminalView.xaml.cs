/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using BbsNavigator.Networking;
using Mosaic.UI.Wpf.Themes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BbsNavigator.Views
{
    /// <summary>
    /// Hosts a VT terminal and owns the lifetime of one BBS connection.
    /// </summary>
    public partial class BbsTerminalView : UserControl, IAsyncDisposable
    {
        private readonly BbsTelnetConnection _connection;
        private readonly CancellationTokenSource _lifetimeCancellation = new();
        private readonly CancellationToken _lifetimeToken;
        private readonly SemaphoreSlim _connectionGate = new(1, 1);
        private readonly object _disposeLock = new();
        private readonly int _reconnectDelaySeconds;
        private Task? _disposeTask;
        private volatile bool _disposed;
        private bool _manualDisconnect;
        private int _reconnectScheduled;
        private int _scrollToEndQueued;

        /// <summary>
        /// Initializes a terminal document for the specified profile.
        /// </summary>
        public BbsTerminalView(BbsProfile profile, double fontSize, int reconnectDelaySeconds)
        {
            InitializeComponent();
            _lifetimeToken = _lifetimeCancellation.Token;
            Profile = profile;
            _reconnectDelaySeconds = Math.Max(1, reconnectDelaySeconds);
            _connection = new BbsTelnetConnection(profile.Host, profile.Port);
            _connection.ConnectionLost += Connection_OnConnectionLost;
            Terminal.Connection = _connection;
            _connection.DataReceived += Connection_OnDataReceived;
            Terminal.FontSize = fontSize;
            DataContext = profile;
            Loaded += BbsTerminalView_OnLoaded;
            UpdateStatus(BbsConnectionState.Disconnected, $"Ready to connect to {profile.Endpoint}");
        }

        /// <summary>
        /// Gets the saved profile represented by this document.
        /// </summary>
        public BbsProfile Profile { get; }

        /// <summary>
        /// Attempts to establish the BBS connection.
        /// </summary>
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

                if (_disposed)
                {
                    return;
                }

                if (_connection.IsConnected)
                {
                    return;
                }

                _manualDisconnect = false;
                UpdateStatus(
                    reconnecting ? BbsConnectionState.Reconnecting : BbsConnectionState.Connecting,
                    reconnecting ? $"Reconnecting to {Profile.Endpoint}…" : $"Connecting to {Profile.Endpoint}…");

                await _connection.ConnectAsync(_lifetimeToken);
                UpdateStatus(BbsConnectionState.Connected, $"Connected to {Profile.Endpoint}");
                Terminal.Focus();
                Profile.LastConnected = DateTime.Now;
            }
            catch (OperationCanceledException) when (_lifetimeToken.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (_disposed)
            {
            }
            catch (Exception ex)
            {
                UpdateStatus(BbsConnectionState.Faulted, ex.Message);
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

        private async void BbsTerminalView_OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= BbsTerminalView_OnLoaded;
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
            if (_disposed || Interlocked.Exchange(ref _scrollToEndQueued, 1) != 0)
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
            try
            {
                UpdateStatus(BbsConnectionState.Reconnecting, $"Reconnecting in {_reconnectDelaySeconds} seconds…");
                await Task.Delay(TimeSpan.FromSeconds(_reconnectDelaySeconds), _lifetimeToken);
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

        private async Task DisconnectAsync()
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
        }

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
            _lifetimeCancellation.Cancel();
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
