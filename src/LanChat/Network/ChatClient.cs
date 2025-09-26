/*
 * Mosaic UI for WPF
 *
 * @project lead      : [Your Name]
 * @website           : https://www.yourwebsite.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

// ReSharper disable CheckNamespace

namespace LanChat.Network
{
    /// <summary>
    /// Provides asynchronous TCP client functionality for the LanChat application.
    /// </summary>
    /// <remarks>
    /// The <see cref="ChatClient"/> class manages a <see cref="TcpClient"/> connection,
    /// handles reconnection logic, and exposes events for received messages and errors.
    /// </remarks>
    public partial class ChatClient : ObservableObject, IAsyncDisposable
    {
        private TcpClient? _tcp;
        private CancellationTokenSource? _cts;
        private Task? _readLoop;
        private string _host = string.Empty;
        private int _port;
        private readonly SemaphoreSlim _reconnectGate = new(1, 1);
        private volatile bool _isDisconnecting;

        /// <summary>
        /// Occurs when a text message is received from the server.
        /// </summary>
        public event EventHandler<TextMessageEventArgs>? TextReceived;

        /// <summary>
        /// Occurs when a JSON envelope is received from the server.
        /// </summary>
        public event EventHandler<EnvelopeEventArgs>? EnvelopeReceived;

        /// <summary>
        /// Occurs when an error is encountered during communication.
        /// </summary>
        public event EventHandler<ErrorEventArgs>? Error;

        /// <summary>
        /// Occurs when the client has been disconnected from the server.
        /// </summary>
        public event EventHandler? Disconnected;

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private string _username = string.Empty;

        private void UpdateIsConnected()
        {
            IsConnected = _tcp is { Connected: true };
        }

        /// <summary>
        /// Connects to the specified host and port asynchronously.
        /// </summary>
        /// <param name="host">The server host name or IP address.</param>
        /// <param name="port">The server port number.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            await DisconnectAsync();

            _host = host;
            _port = port;

            await ConnectInternalAsync(cancellationToken);
            UpdateIsConnected();
        }

        private async Task ConnectInternalAsync(CancellationToken cancellationToken = default)
        {
            _tcp = new TcpClient(AddressFamily.InterNetwork);
            _cts = new CancellationTokenSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);

            await _tcp.ConnectAsync(_host, _port, linked.Token).ConfigureAwait(false);

            UpdateIsConnected();

            _readLoop = Task.Run(() => ReadLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Disconnects from the server asynchronously.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_isDisconnecting)
            {
                return;
            }

            _isDisconnecting = true;

            try
            {
                _cts?.Cancel();

                var readLoopTask = _readLoop;
                if (readLoopTask != null)
                {
                    try
                    {
                        await readLoopTask;
                    }
                    catch { /* Ignore exceptions on disconnect */ }
                }

                _tcp?.Close();
                _tcp?.Dispose();
                _tcp = null;
                _readLoop = null;
                UpdateIsConnected();
            }
            finally
            {
                _isDisconnecting = false;
            }
        }

        // Also update IsConnected after reconnection attempts
        private async Task EnsureConnectedAsync(CancellationToken ct = default)
        {
            if (IsConnected)
            {
                return;
            }

            await _reconnectGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (IsConnected)
                {
                    return;
                }

                if (string.IsNullOrEmpty(_host) || _port == 0)
                {
                    throw new InvalidOperationException("Must call ConnectAsync first.");
                }

                var savedUsername = Username;

                var maxRetries = 3;
                var baseDelay = TimeSpan.FromMilliseconds(500);

                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    try
                    {
                        await DisconnectAsync();
                        await ConnectInternalAsync(ct);
                        UpdateIsConnected();

                        if (!string.IsNullOrEmpty(savedUsername))
                        {
                            Username = savedUsername;
                            var loginData = new Dictionary<string, string> { ["username"] = Username };
                            var envelope = MessageEnvelope.Create(loginData, typeName: "Login");
                            await SendEnvelopeInternalAsync(envelope, ct).ConfigureAwait(false);
                        }

                        return;
                    }
                    catch (Exception) when (attempt < maxRetries - 1)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt)), ct);
                    }
                }

                throw new InvalidOperationException("Unable to establish connection to server after multiple attempts.");
            }
            finally
            {
                _reconnectGate.Release();
            }
        }

        /// <summary>
        /// Logs in to the server with the specified username.
        /// </summary>
        /// <param name="username">The username to use for login.</param>
        /// <param name="ct">A cancellation token.</param>
        public async Task LoginAsync(string username, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("Username cannot be empty.", nameof(username));
            }

            await EnsureConnectedAsync(ct);

            Username = username.Trim();

            var loginData = new Dictionary<string, string> { ["username"] = Username };
            var envelope = MessageEnvelope.Create(loginData, typeName: "Login");
            await SendEnvelopeInternalAsync(envelope, ct).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
            _reconnectGate.Dispose();
        }

        /// <summary>
        /// Sends a text message to the server asynchronously.
        /// </summary>
        /// <param name="text">The text to send.</param>
        /// <param name="ct">A cancellation token.</param>
        public async Task SendTextAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                throw new InvalidOperationException("Must login before sending messages.");
            }

            await EnsureConnectedAsync(ct);

            var bytes = Encoding.UTF8.GetBytes(text);

            try
            {
                await Framing.WriteAsync(_tcp!.GetStream(), MessageKind.Text, bytes, ct).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // The connection was likely closed. Attempt to reconnect and resend once.
                await EnsureConnectedAsync(ct);
                await Framing.WriteAsync(_tcp!.GetStream(), MessageKind.Text, bytes, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends a JSON envelope to the server asynchronously.
        /// </summary>
        /// <param name="envelope">The envelope to send.</param>
        /// <param name="ct">A cancellation token.</param>
        public async Task SendEnvelopeAsync(MessageEnvelope envelope, CancellationToken ct = default)
        {
            await EnsureConnectedAsync(ct);
            await SendEnvelopeInternalAsync(envelope, ct);
        }

        private async Task SendEnvelopeInternalAsync(MessageEnvelope envelope, CancellationToken ct = default)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, MessageEnvelope.DefaultJsonOptions);
            await Framing.WriteAsync(_tcp!.GetStream(), MessageKind.JsonEnvelope, bytes, ct).ConfigureAwait(false);
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            var stream = _tcp!.GetStream();
            bool isIntentionalDisconnect = false;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var (kind, payload) = await Framing.ReadAsync(stream, ct).ConfigureAwait(false);
                    switch (kind)
                    {
                        case MessageKind.Text:
                            TextReceived?.Invoke(this, new TextMessageEventArgs
                            {
                                ClientId = Guid.Empty,
                                Text = Encoding.UTF8.GetString(payload),
                                Username = "Server"
                            });
                            break;

                        case MessageKind.JsonEnvelope:
                            var env = JsonSerializer.Deserialize<MessageEnvelope>(payload, MessageEnvelope.DefaultJsonOptions);
                            EnvelopeReceived?.Invoke(this, new EnvelopeEventArgs
                            {
                                ClientId = Guid.Empty,
                                Envelope = env,
                                Username = "Server"
                            });
                            break;

                        default:
                            throw new NotSupportedException($"Unknown message kind: {kind}");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                isIntentionalDisconnect = true;
            }
            catch (IOException) { }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs { Exception = ex });
            }
            finally
            {
                if (!isIntentionalDisconnect)
                {
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
                await DisconnectAsync();
            }
        }
    }
}