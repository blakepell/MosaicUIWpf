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
        /// Raised whenever the remote host outputs data.
        /// </summary>
        public event EventHandler<string>? DataReceived;

        /// <inheritdoc />
        public bool IsConnected => _isConnected && _client?.IsConnected == true && _shell != null;

        /// <inheritdoc />
        public bool Connect()
        {
            if (IsConnected)
            {
                return true;
            }

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

            _client = new SshClient(connInfo)
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            };

            try
            {
                _client.Connect();

                if (!_client.IsConnected)
                {
                    return false;
                }

                _shell = _client.CreateShellStream(
                    terminalName: "xterm-256color",
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

            try
            {
                StopReader();
                _shell?.Dispose();
                _shell = null;

                if (_client?.IsConnected == true)
                {
                    _client.Disconnect();
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

            _shell.Write(text);
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

            var bytes = Encoding.UTF8.GetBytes(text);
            await _shell.WriteAsync(bytes, 0, bytes.Length);
            await _shell.FlushAsync();
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

            await _shell.WriteAsync(data, 0, data.Length);
            await _shell.FlushAsync();
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
        }

        private void StopReader()
        {
            if (_shell != null)
            {
                _shell.DataReceived -= OnShellData;
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

            string text = Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);

            try
            {
                DataReceived?.Invoke(this, text);
            }
            catch
            {
                // Do not let consumer exceptions kill the shell stream callback.
            }
        }

        /// <inheritdoc />
        public void SendWindowChangeRequest(uint cols, uint rows, uint width, uint height)
        {
            if (_shell == null)
            {
                return;
            }

            var channel = _shell.GetType().GetField("_channel", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_shell);
            var method = channel?.GetType().GetMethod("SendWindowChangeRequest", BindingFlags.Public | BindingFlags.Instance);
            method?.Invoke(channel, [cols, rows, width, height]);
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
                if (_client?.IsConnected == true)
                {
                    _client.Disconnect();
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
