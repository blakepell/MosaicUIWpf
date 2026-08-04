/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Renci.SshNet;
using Renci.SshNet.Common;

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// SSH-backed terminal connection for <see cref="VT52Terminal"/>.
    /// </summary>
    public class SshConnection : ITerminalConnection
    {
        private SshClient? _client;
        private ShellStream? _shell;
        private volatile bool _isConnected;
        private volatile bool _intentionalDisconnect;
        private CancellationTokenSource? _readCts;

        /// <summary>
        /// Gets or sets the SSH host name or IP address.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets the SSH port.
        /// </summary>
        public int Port { get; set; } = 22;

        /// <summary>
        /// Gets or sets the SSH username.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the SSH password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the private key file path.
        /// </summary>
        public string? KeyFile { get; set; }

        /// <summary>
        /// Gets or sets the private key passphrase.
        /// </summary>
        public string? KeyPassphrase { get; set; }

        /// <inheritdoc />
        public int Columns { get; set; } = 120;

        /// <inheritdoc />
        public int Rows { get; set; } = 24;

        /// <inheritdoc />
        public int Width { get; set; } = 1980;

        /// <inheritdoc />
        public int Height { get; set; } = 1060;

        /// <summary>
        /// Gets or sets the shell stream buffer size.
        /// </summary>
        public int BufferSize { get; set; } = 1024 * 8;

        /// <summary>
        /// Gets or sets the terminal type reported to the remote host.
        /// </summary>
        public string TerminalName { get; set; } = "xterm-256color";

        /// <summary>
        /// Gets or sets the encoding used to decode remote output and encode text that is sent.
        /// </summary>
        /// <remarks>
        /// Set this before connecting. The default is <see cref="System.Text.Encoding.UTF8"/>.
        /// </remarks>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Raised whenever the remote host outputs data.
        /// </summary>
        /// <remarks>
        /// The payload is decoded with <see cref="Encoding"/>. Decoding is skipped entirely when
        /// no handler is attached, so a consumer that only wants bytes can subscribe to
        /// <see cref="RawDataReceived"/> alone.
        /// </remarks>
        public event EventHandler<string>? DataReceived;

        /// <summary>
        /// Raised with the undecoded bytes whenever the remote host outputs data.
        /// </summary>
        /// <remarks>
        /// Use this when the consumer needs an 8-bit clean stream, such as a terminal that
        /// applies its own decoder or a file transfer protocol that owns the byte stream.
        /// The array is not reused, so handlers may retain it.
        /// </remarks>
        public event EventHandler<byte[]>? RawDataReceived;

        /// <summary>
        /// Raised when the remote host closes the session or the underlying transport fails.
        /// </summary>
        /// <remarks>
        /// The argument carries the failure, or <see langword="null"/> when the peer closed the
        /// shell cleanly. This does not fire for a local <see cref="Disconnect"/>.
        /// </remarks>
        public event EventHandler<Exception?>? ConnectionLost;

        /// <inheritdoc />
        public bool IsConnected => _isConnected && _client?.IsConnected == true && _shell != null;

        /// <inheritdoc />
        public bool Connect()
        {
            if (IsConnected)
            {
                return true;
            }

            // A dropped session leaves the client and shell behind; tear them down so a
            // reconnect does not orphan the previous stream and its event handlers.
            Cleanup();

            if (string.IsNullOrWhiteSpace(Host))
            {
                throw new InvalidOperationException("Host is required.");
            }

            if (string.IsNullOrWhiteSpace(Username))
            {
                throw new InvalidOperationException("Username is required.");
            }

            var methods = new List<AuthenticationMethod>();

            if (!string.IsNullOrWhiteSpace(KeyFile))
            {
                var keyFiles = new[] { LoadKeyFile(KeyFile, KeyPassphrase) };
                var pk = new PrivateKeyAuthenticationMethod(Username, keyFiles);
                methods.Add(pk);
            }

            if (!string.IsNullOrEmpty(Password))
            {
                methods.Add(new PasswordAuthenticationMethod(Username, Password));
            }

            if (methods.Count == 0)
            {
                throw new InvalidOperationException("Provide either Password or KeyFile (with optional passphrase).");
            }

            var connInfo = new ConnectionInfo(Host, Port, Username, methods.ToArray());

            _intentionalDisconnect = false;
            _client = new SshClient(connInfo)
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            };

            try
            {
                _client.ErrorOccurred += OnClientError;
                _client.Connect();

                if (!_client.IsConnected)
                {
                    return false;
                }

                _shell = _client.CreateShellStream(
                    terminalName: string.IsNullOrWhiteSpace(TerminalName) ? "xterm-256color" : TerminalName,
                    columns: (uint)Columns,
                    rows: (uint)Rows,
                    width: (uint)Width,
                    height: (uint)Height,
                    bufferSize: BufferSize);

                StartReader();
                _isConnected = true;
                return true;
            }
            catch
            {
                Cleanup();
                throw;
            }
        }

        /// <inheritdoc />
        public bool Disconnect()
        {
            _isConnected = false;
            _intentionalDisconnect = true;

            try
            {
                StopReader();
                _shell?.Dispose();
                _shell = null;

                if (_client != null)
                {
                    _client.ErrorOccurred -= OnClientError;

                    if (_client.IsConnected)
                    {
                        _client.Disconnect();
                    }
                }

                _client?.Dispose();
                _client = null;

                return true;
            }
            catch
            {
                Cleanup();
                return false;
            }
        }

        /// <inheritdoc />
        public void Send(string text)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected.");
            }

            if (_shell == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            _shell.Write(Encoding.GetBytes(text));
            _shell.Flush();
        }

        /// <inheritdoc />
        public async Task SendAsync(string text)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected.");
            }

            if (_shell == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            var bytes = Encoding.GetBytes(text);
            await _shell.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            await _shell.FlushAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected.");
            }

            if (_shell == null || data.Length == 0)
            {
                return;
            }

            _shell.Write(data, 0, data.Length);
            _shell.Flush();
        }

        /// <inheritdoc />
        public async Task SendAsync(byte[] data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected.");
            }

            if (_shell == null || data.Length == 0)
            {
                return;
            }

            await _shell.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await _shell.FlushAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync() => Task.Run(Connect);

        /// <inheritdoc />
        public Task<bool> DisconnectAsync() => Task.Run(Disconnect);

        private static PrivateKeyFile LoadKeyFile(string path, string? passphrase)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Private key file not found.", path);
            }

            if (string.IsNullOrEmpty(passphrase))
            {
                return new PrivateKeyFile(path);
            }

            return new PrivateKeyFile(path, passphrase);
        }

        private void StartReader()
        {
            StopReader();

            if (_shell == null)
            {
                throw new InvalidOperationException("Shell stream is not initialized.");
            }

            _readCts = new CancellationTokenSource();
            _shell.DataReceived += OnShellData;
            _shell.ErrorOccurred += OnShellError;
            _shell.Closed += OnShellClosed;
        }

        private void StopReader()
        {
            if (_shell != null)
            {
                _shell.DataReceived -= OnShellData;
                _shell.ErrorOccurred -= OnShellError;
                _shell.Closed -= OnShellClosed;
            }

            if (_readCts != null)
            {
                try
                {
                    _readCts.Cancel();
                }
                catch
                {
                }

                _readCts.Dispose();
                _readCts = null;
            }
        }

        private void OnShellData(object? sender, ShellDataEventArgs e)
        {
            if (e.Data == null || e.Data.Length == 0)
            {
                return;
            }

            try
            {
                RawDataReceived?.Invoke(this, e.Data);

                EventHandler<string>? textHandler = DataReceived;

                if (textHandler != null)
                {
                    textHandler(this, Encoding.GetString(e.Data, 0, e.Data.Length));
                }
            }
            catch
            {
                // Do not let consumer exceptions kill the shell stream callback.
            }
        }

        private void OnShellError(object? sender, Renci.SshNet.Common.ExceptionEventArgs e)
        {
            RaiseConnectionLost(e.Exception);
        }

        private void OnShellClosed(object? sender, EventArgs e)
        {
            RaiseConnectionLost(null);
        }

        private void OnClientError(object? sender, Renci.SshNet.Common.ExceptionEventArgs e)
        {
            RaiseConnectionLost(e.Exception);
        }

        /// <summary>
        /// Reports a dropped session once, ignoring failures that follow a local disconnect.
        /// </summary>
        /// <param name="exception">The failure that ended the session, or <see langword="null"/> when the peer closed it cleanly.</param>
        private void RaiseConnectionLost(Exception? exception)
        {
            if (_intentionalDisconnect || !_isConnected)
            {
                return;
            }

            _isConnected = false;

            try
            {
                ConnectionLost?.Invoke(this, exception);
            }
            catch
            {
                // A consumer fault must not propagate into the SSH.NET callback.
            }
        }

        /// <inheritdoc />
        public void SendWindowChangeRequest(uint cols, uint rows, uint width, uint height)
        {
            if (_shell == null)
            {
                return;
            }

            Columns = (int)Math.Min(cols, int.MaxValue);
            Rows = (int)Math.Min(rows, int.MaxValue);
            Width = (int)Math.Min(width, int.MaxValue);
            Height = (int)Math.Min(height, int.MaxValue);

            try
            {
                _shell.ChangeWindowSize(cols, rows, width, height);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or SshException or IOException)
            {
                // The session is going away; the connection-lost path reports it.
            }
        }

        private void Cleanup()
        {
            try
            {
                StopReader();
            }
            catch
            {
            }

            try
            {
                _shell?.Dispose();
            }
            catch
            {
            }

            _shell = null;

            try
            {
                if (_client != null)
                {
                    _client.ErrorOccurred -= OnClientError;

                    if (_client.IsConnected)
                    {
                        _client.Disconnect();
                    }
                }
            }
            catch
            {
            }

            try
            {
                _client?.Dispose();
            }
            catch
            {
            }

            _client = null;
            _isConnected = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disconnect();
        }
    }
}
