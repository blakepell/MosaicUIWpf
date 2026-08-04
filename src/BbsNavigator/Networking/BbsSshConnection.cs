/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls.VT52Terminal;
using System.Buffers;
using System.Text;

namespace BbsNavigator.Networking
{
    /// <summary>
    /// Provides a BBS session over SSH. The SSH plumbing (authentication, shell channel,
    /// window resizing) comes from <see cref="SshConnection"/>; this class adds the pieces a
    /// BBS session needs on top of it: incremental text decoding in the profile's encoding,
    /// traffic counters, and a binary mode for file transfers.
    /// </summary>
    public sealed class BbsSshConnection : IBbsConnection
    {
        private readonly SshConnection _ssh;
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private volatile SshBinaryChannel? _binaryChannel;
        private Decoder? _decoder;
        private long _bytesReceived;
        private long _bytesSent;
        private bool _disposed;

        /// <summary>
        /// Initializes a connection for the specified endpoint and login.
        /// </summary>
        /// <param name="host">The SSH host name or IP address.</param>
        /// <param name="port">The SSH port.</param>
        /// <param name="userName">The SSH username.</param>
        /// <param name="password">The SSH password.</param>
        public BbsSshConnection(string host, int port, string userName, string password)
        {
            Host = host;
            Port = port;

            _ssh = new SshConnection
            {
                Host = host,
                Port = port,
                Username = userName,
                Password = password
            };

            _ssh.RawDataReceived += Ssh_OnRawDataReceived;
            _ssh.ConnectionLost += Ssh_OnConnectionLost;
        }

        /// <inheritdoc />
        public string Host { get; }

        /// <inheritdoc />
        public int Port { get; }

        /// <inheritdoc />
        public bool IsConnected => _ssh.IsConnected;

        /// <inheritdoc />
        public int Columns
        {
            get => _ssh.Columns;
            set => _ssh.Columns = value;
        }

        /// <inheritdoc />
        public int Rows
        {
            get => _ssh.Rows;
            set => _ssh.Rows = value;
        }

        /// <inheritdoc />
        public int Width
        {
            get => _ssh.Width;
            set => _ssh.Width = value;
        }

        /// <inheritdoc />
        public int Height
        {
            get => _ssh.Height;
            set => _ssh.Height = value;
        }

        /// <summary>
        /// Gets or sets the encoding used for BBS text. Set this before connecting; the
        /// incremental decoder is created when the connection opens.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <inheritdoc />
        public long BytesReceived => Interlocked.Read(ref _bytesReceived);

        /// <inheritdoc />
        public long BytesSent => Interlocked.Read(ref _bytesSent);

        /// <inheritdoc />
        public DateTime? ConnectedAtUtc { get; private set; }

        /// <inheritdoc />
        public bool IsBinaryModeActive => _binaryChannel != null;

        /// <inheritdoc />
        public event EventHandler<string>? DataReceived;

        /// <inheritdoc />
        public event EventHandler<Exception?>? ConnectionLost;

        /// <inheritdoc />
        public bool Connect() => ConnectAsync(CancellationToken.None).GetAwaiter().GetResult();

        /// <inheritdoc />
        public Task<bool> ConnectAsync() => ConnectAsync(CancellationToken.None);

        /// <inheritdoc />
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (IsConnected)
                {
                    return true;
                }

                _decoder = Encoding.GetDecoder();
                _ssh.Encoding = Encoding;
                Interlocked.Exchange(ref _bytesReceived, 0);
                Interlocked.Exchange(ref _bytesSent, 0);

                // SSH.NET's handshake is synchronous, so it runs on the thread pool and the
                // caller's token abandons the wait; the orphaned attempt is torn down below.
                Task<bool> connectTask = Task.Run(_ssh.Connect, CancellationToken.None);
                bool connected;

                try
                {
                    connected = await connectTask.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _ = connectTask.ContinueWith(
                        static (_, state) => ((SshConnection)state!).Disconnect(),
                        _ssh,
                        CancellationToken.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);
                    throw;
                }

                if (connected)
                {
                    ConnectedAtUtc = DateTime.UtcNow;
                }

                return connected;
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        /// <inheritdoc />
        public bool Disconnect() => DisconnectAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        public async Task<bool> DisconnectAsync()
        {
            await _lifecycleGate.WaitAsync().ConfigureAwait(false);

            try
            {
                ConnectedAtUtc = null;
                _binaryChannel?.Complete();
                return await Task.Run(_ssh.Disconnect).ConfigureAwait(false);
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        /// <inheritdoc />
        public void Send(string text) => SendAsync(text).GetAwaiter().GetResult();

        /// <inheritdoc />
        public Task SendAsync(string text)
        {
            if (string.IsNullOrEmpty(text) || _binaryChannel != null)
            {
                // Keyboard input is discarded while a file transfer owns the stream;
                // a stray keypress must not corrupt protocol framing.
                return Task.CompletedTask;
            }

            return SendAsync(Encoding.GetBytes(text));
        }

        /// <inheritdoc />
        public void Send(byte[] data) => SendAsync(data).GetAwaiter().GetResult();

        /// <inheritdoc />
        public async Task SendAsync(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data.Length == 0)
            {
                return;
            }

            await _ssh.SendAsync(data).ConfigureAwait(false);
            Interlocked.Add(ref _bytesSent, data.Length);
        }

        /// <inheritdoc />
        public void SendWindowChangeRequest(uint cols, uint rows, uint width, uint height)
        {
            if (!IsConnected)
            {
                return;
            }

            _ssh.SendWindowChangeRequest(cols, rows, width, height);
        }

        /// <inheritdoc />
        public IBbsBinaryChannel EnterBinaryMode()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!IsConnected)
            {
                throw new InvalidOperationException("The BBS is not connected.");
            }

            var channel = new SshBinaryChannel(this);

            if (Interlocked.CompareExchange(ref _binaryChannel, channel, null) != null)
            {
                throw new InvalidOperationException("A file transfer is already in progress.");
            }

            return channel;
        }

        /// <summary>
        /// Restores terminal text delivery when a transfer channel is disposed.
        /// </summary>
        /// <param name="channel">The channel being released.</param>
        internal void ExitBinaryMode(SshBinaryChannel channel)
        {
            Interlocked.CompareExchange(ref _binaryChannel, null, channel);
        }

        /// <summary>
        /// Sends transfer payload bytes. The SSH channel is 8-bit clean, so no escaping applies.
        /// </summary>
        internal async ValueTask SendBinaryAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendAsync(data.ToArray()).ConfigureAwait(false);
        }

        /// <summary>
        /// Routes shell output to the active transfer channel, or decodes it for the terminal.
        /// Runs on an SSH.NET callback thread.
        /// </summary>
        private void Ssh_OnRawDataReceived(object? sender, byte[] payload)
        {
            if (payload.Length == 0)
            {
                return;
            }

            Interlocked.Add(ref _bytesReceived, payload.Length);
            SshBinaryChannel? channel = _binaryChannel;

            if (channel != null)
            {
                channel.Post(payload);
                return;
            }

            string text = DecodeText(payload);

            if (text.Length > 0)
            {
                DataReceived?.Invoke(this, text);
            }
        }

        private void Ssh_OnConnectionLost(object? sender, Exception? exception)
        {
            ConnectedAtUtc = null;
            _binaryChannel?.Complete();
            ConnectionLost?.Invoke(this, exception);
        }

        /// <summary>
        /// Decodes payload bytes using the incremental decoder so multi-byte sequences that
        /// split across reads survive intact.
        /// </summary>
        private string DecodeText(byte[] payload)
        {
            Decoder decoder = _decoder ??= Encoding.GetDecoder();
            char[] chars = ArrayPool<char>.Shared.Rent(Encoding.GetMaxCharCount(payload.Length));

            try
            {
                int charCount = decoder.GetChars(payload, 0, payload.Length, chars, 0);
                return new string(chars, 0, charCount);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(chars);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _ssh.RawDataReceived -= Ssh_OnRawDataReceived;
            _ssh.ConnectionLost -= Ssh_OnConnectionLost;
            _ssh.Dispose();
            ConnectedAtUtc = null;
            _lifecycleGate.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _ssh.RawDataReceived -= Ssh_OnRawDataReceived;
            _ssh.ConnectionLost -= Ssh_OnConnectionLost;
            await DisconnectAsync().ConfigureAwait(false);
            _disposed = true;
            _ssh.Dispose();
            _lifecycleGate.Dispose();
        }
    }
}
