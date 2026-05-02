/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Net.Sockets;

namespace Mosaic.UI.Wpf.Controls.VT52Terminal
{
    /// <summary>
    /// Telnet-backed terminal connection for <see cref="VT52Terminal"/>.
    /// </summary>
    public class TelnetConnection : ITerminalConnection
    {
        private const byte Iac = 255;
        private const byte Dont = 254;
        private const byte Do = 253;
        private const byte Wont = 252;
        private const byte Will = 251;
        private const byte Sb = 250;
        private const byte Se = 240;

        private const byte OptionBinary = 0;
        private const byte OptionEcho = 1;
        private const byte OptionSuppressGoAhead = 3;
        private const byte OptionTerminalType = 24;
        private const byte OptionNaws = 31;

        private const byte TerminalTypeIs = 0;
        private const byte TerminalTypeSend = 1;

        private enum ParserState
        {
            Data,
            Command,
            Option,
            SubnegotiationOption,
            Subnegotiation,
            SubnegotiationIac
        }

        private readonly object _sendLock = new();
        private readonly List<byte> _subnegotiationBuffer = new();
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _readCts;
        private Task? _readerTask;
        private ParserState _parserState = ParserState.Data;
        private byte _pendingCommand;
        private byte _subnegotiationOption;
        private volatile bool _isConnected;
        private volatile bool _nawsEnabled;

        /// <summary>
        /// Gets or sets the telnet host name or IP address.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets the telnet port.
        /// </summary>
        public int Port { get; set; } = 23;

        /// <inheritdoc />
        public int Columns { get; set; } = 120;

        /// <inheritdoc />
        public int Rows { get; set; } = 24;

        /// <inheritdoc />
        public int Width { get; set; } = 1980;

        /// <inheritdoc />
        public int Height { get; set; } = 1060;

        /// <summary>
        /// Gets or sets the terminal type reported to the telnet host.
        /// </summary>
        public string TerminalType { get; set; } = "xterm-256color";

        /// <summary>
        /// Gets or sets the encoding used for terminal payload data.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets or sets the network read buffer size.
        /// </summary>
        public int BufferSize { get; set; } = 1024 * 8;

        /// <summary>
        /// Raised whenever the remote host outputs terminal data.
        /// </summary>
        public event EventHandler<string>? DataReceived;

        /// <inheritdoc />
        public bool IsConnected => _isConnected && _client?.Connected == true && _stream != null;

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

            _client = new TcpClient
            {
                NoDelay = true
            };

            try
            {
                _client.Connect(Host, Port);
                _stream = _client.GetStream();
                _isConnected = true;
                _readCts = new CancellationTokenSource();
                _readerTask = Task.Run(() => ReadLoop(_readCts.Token));
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
                _stream?.Dispose();
                _stream = null;
                _client?.Close();
                _client?.Dispose();
                _client = null;
                _nawsEnabled = false;
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
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Send(Encoding.GetBytes(text));
        }

        /// <inheritdoc />
        public Task SendAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return Task.CompletedTask;
            }

            return SendAsync(Encoding.GetBytes(text));
        }

        /// <inheritdoc />
        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected.");
            }

            if (data.Length == 0)
            {
                return;
            }

            WriteRaw(EscapeIac(data));
        }

        /// <inheritdoc />
        public async Task SendAsync(byte[] data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected.");
            }

            if (data.Length == 0)
            {
                return;
            }

            await WriteRawAsync(EscapeIac(data));
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync() => Task.Run(Connect);

        /// <inheritdoc />
        public Task<bool> DisconnectAsync() => Task.Run(Disconnect);

        /// <inheritdoc />
        public void SendWindowChangeRequest(uint cols, uint rows, uint width, uint height)
        {
            Columns = (int)Math.Min(cols, int.MaxValue);
            Rows = (int)Math.Min(rows, int.MaxValue);
            Width = (int)Math.Min(width, int.MaxValue);
            Height = (int)Math.Min(height, int.MaxValue);

            if (!IsConnected || !_nawsEnabled)
            {
                return;
            }

            SendNaws();
        }

        private async Task ReadLoop(CancellationToken cancellationToken)
        {
            var buffer = new byte[Math.Max(256, BufferSize)];
            var payload = new List<byte>(buffer.Length);

            try
            {
                while (!cancellationToken.IsCancellationRequested && _stream != null)
                {
                    int read = await _stream.ReadAsync(buffer, cancellationToken);

                    if (read == 0)
                    {
                        break;
                    }

                    payload.Clear();
                    ParseIncoming(buffer, read, payload);

                    if (payload.Count > 0)
                    {
                        RaiseDataReceived(Encoding.GetString(payload.ToArray()));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _isConnected = false;
            }
        }

        private void ParseIncoming(byte[] buffer, int count, List<byte> payload)
        {
            for (int i = 0; i < count; i++)
            {
                byte value = buffer[i];

                switch (_parserState)
                {
                    case ParserState.Data:
                        if (value == Iac)
                        {
                            _parserState = ParserState.Command;
                        }
                        else
                        {
                            payload.Add(value);
                        }
                        break;
                    case ParserState.Command:
                        HandleCommand(value, payload);
                        break;
                    case ParserState.Option:
                        HandleNegotiation(_pendingCommand, value);
                        _parserState = ParserState.Data;
                        break;
                    case ParserState.SubnegotiationOption:
                        _subnegotiationOption = value;
                        _subnegotiationBuffer.Clear();
                        _parserState = ParserState.Subnegotiation;
                        break;
                    case ParserState.Subnegotiation:
                        if (value == Iac)
                        {
                            _parserState = ParserState.SubnegotiationIac;
                        }
                        else
                        {
                            _subnegotiationBuffer.Add(value);
                        }
                        break;
                    case ParserState.SubnegotiationIac:
                        if (value == Iac)
                        {
                            _subnegotiationBuffer.Add(Iac);
                            _parserState = ParserState.Subnegotiation;
                        }
                        else if (value == Se)
                        {
                            HandleSubnegotiation(_subnegotiationOption, _subnegotiationBuffer);
                            _subnegotiationBuffer.Clear();
                            _parserState = ParserState.Data;
                        }
                        else
                        {
                            _subnegotiationBuffer.Clear();
                            _parserState = ParserState.Data;
                        }
                        break;
                }
            }
        }

        private void HandleSubnegotiation(byte option, List<byte> data)
        {
            if (option == OptionTerminalType && data.Count > 0 && data[0] == TerminalTypeSend)
            {
                SendTerminalType();
            }
        }

        private void HandleCommand(byte command, List<byte> payload)
        {
            if (command == Iac)
            {
                payload.Add(Iac);
                _parserState = ParserState.Data;
                return;
            }

            if (command is Do or Dont or Will or Wont)
            {
                _pendingCommand = command;
                _parserState = ParserState.Option;
                return;
            }

            if (command == Sb)
            {
                _subnegotiationBuffer.Clear();
                _parserState = ParserState.SubnegotiationOption;
                return;
            }

            _parserState = ParserState.Data;
        }

        private void HandleNegotiation(byte command, byte option)
        {
            switch (command)
            {
                case Do:
                    if (option is OptionTerminalType or OptionNaws or OptionSuppressGoAhead or OptionBinary)
                    {
                        WriteCommand(Will, option);

                        if (option == OptionNaws)
                        {
                            _nawsEnabled = true;
                            SendNaws();
                        }
                    }
                    else
                    {
                        WriteCommand(Wont, option);
                    }
                    break;
                case Dont:
                    if (option == OptionNaws)
                    {
                        _nawsEnabled = false;
                    }

                    WriteCommand(Wont, option);
                    break;
                case Will:
                    if (option is OptionEcho or OptionSuppressGoAhead or OptionBinary)
                    {
                        WriteCommand(Do, option);
                    }
                    else
                    {
                        WriteCommand(Dont, option);
                    }
                    break;
                case Wont:
                    WriteCommand(Dont, option);
                    break;
            }
        }

        private void SendTerminalType()
        {
            var terminalType = Encoding.ASCII.GetBytes(string.IsNullOrWhiteSpace(TerminalType) ? "xterm-256color" : TerminalType);
            var response = new List<byte>(terminalType.Length + 6)
            {
                Iac,
                Sb,
                OptionTerminalType,
                TerminalTypeIs
            };

            response.AddRange(EscapeIac(terminalType));
            response.Add(Iac);
            response.Add(Se);
            WriteRaw(response.ToArray());
        }

        private void SendNaws()
        {
            ushort cols = (ushort)Math.Clamp(Columns, 1, ushort.MaxValue);
            ushort rows = (ushort)Math.Clamp(Rows, 1, ushort.MaxValue);
            byte[] data =
            [
                Iac,
                Sb,
                OptionNaws,
                (byte)(cols >> 8),
                (byte)(cols & 0xFF),
                (byte)(rows >> 8),
                (byte)(rows & 0xFF),
                Iac,
                Se
            ];

            WriteRaw(data);
        }

        private void WriteCommand(byte command, byte option)
        {
            WriteRaw([Iac, command, option]);
        }

        private void WriteRaw(byte[] data)
        {
            if (_stream == null)
            {
                return;
            }

            lock (_sendLock)
            {
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
        }

        private async Task WriteRawAsync(byte[] data)
        {
            if (_stream == null)
            {
                return;
            }

            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }

        private static byte[] EscapeIac(byte[] data)
        {
            if (!data.Contains(Iac))
            {
                return data;
            }

            var escaped = new List<byte>(data.Length + 4);

            foreach (byte value in data)
            {
                escaped.Add(value);

                if (value == Iac)
                {
                    escaped.Add(Iac);
                }
            }

            return escaped.ToArray();
        }

        private void RaiseDataReceived(string text)
        {
            try
            {
                DataReceived?.Invoke(this, text);
            }
            catch
            {
                // Do not let consumer exceptions kill the read loop.
            }
        }

        private void StopReader()
        {
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
                _stream?.Dispose();
            }
            catch
            {
            }

            _stream = null;

            try
            {
                _client?.Close();
                _client?.Dispose();
            }
            catch
            {
            }

            _client = null;
            _readerTask = null;
            _nawsEnabled = false;
            _isConnected = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disconnect();
        }
    }
}
