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
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace BbsNavigator.Networking
{
    /// <summary>
    /// Provides an asynchronous, allocation-conscious Telnet connection for a BBS terminal.
    /// </summary>
    public sealed class BbsTelnetConnection : ITerminalConnection, IAsyncDisposable
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

        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private readonly SemaphoreSlim _sendGate = new(1, 1);
        private readonly byte[] _subnegotiation = new byte[128];
        private Socket? _socket;
        private NetworkStream? _stream;
        private CancellationTokenSource? _readCancellation;
        private Task? _readTask;
        private ParserState _parserState;
        private byte _pendingCommand;
        private byte _subnegotiationOption;
        private int _subnegotiationLength;
        private bool _nawsEnabled;
        private bool _intentionalDisconnect;
        private bool _disposed;
        private volatile bool _isConnected;

        private enum ParserState
        {
            Data,
            Command,
            Option,
            SubnegotiationOption,
            Subnegotiation,
            SubnegotiationIac
        }

        /// <summary>
        /// Initializes a connection for the specified endpoint.
        /// </summary>
        public BbsTelnetConnection(string host, int port)
        {
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Gets the remote host name.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the remote port.
        /// </summary>
        public int Port { get; }

        /// <inheritdoc />
        public bool IsConnected => _isConnected;

        /// <inheritdoc />
        public int Columns { get; set; } = 120;

        /// <inheritdoc />
        public int Rows { get; set; } = 40;

        /// <inheritdoc />
        public int Width { get; set; } = 1200;

        /// <inheritdoc />
        public int Height { get; set; } = 800;

        /// <summary>
        /// Gets or sets the encoding used for BBS text.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <inheritdoc />
        public event EventHandler<string>? DataReceived;

        /// <summary>
        /// Occurs when the peer closes the connection or network I/O fails.
        /// </summary>
        public event EventHandler<Exception?>? ConnectionLost;

        /// <inheritdoc />
        public bool Connect() => ConnectAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        public Task<bool> ConnectAsync() => ConnectAsync(CancellationToken.None);

        /// <summary>
        /// Connects asynchronously with cancellation support.
        /// </summary>
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

                _intentionalDisconnect = false;
                _parserState = ParserState.Data;
                _subnegotiationLength = 0;
                _nawsEnabled = false;

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };

                try
                {
                    await socket.ConnectAsync(Host, Port, cancellationToken).ConfigureAwait(false);
                    var stream = new NetworkStream(socket, ownsSocket: true);
                    var readCancellation = new CancellationTokenSource();
                    _socket = socket;
                    _stream = stream;
                    _readCancellation = readCancellation;
                    _isConnected = true;
                    _readTask = ReadLoopAsync(stream, readCancellation.Token);
                    return true;
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
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
            Task? readTask;

            try
            {
                _intentionalDisconnect = true;
                _isConnected = false;
                _readCancellation?.Cancel();
                _stream?.Dispose();
                _socket?.Dispose();
                readTask = _readTask;
                _stream = null;
                _socket = null;
                _readTask = null;
                _readCancellation?.Dispose();
                _readCancellation = null;
                _nawsEnabled = false;
            }
            finally
            {
                _lifecycleGate.Release();
            }

            if (readTask != null)
            {
                try
                {
                    await readTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            return true;
        }

        /// <inheritdoc />
        public void Send(string text) => SendAsync(text).GetAwaiter().GetResult();

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
        public void Send(byte[] data) => SendAsync(data).GetAwaiter().GetResult();

        /// <inheritdoc />
        public Task SendAsync(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return SendPayloadAsync(data);
        }

        /// <inheritdoc />
        public void SendWindowChangeRequest(uint cols, uint rows, uint width, uint height)
        {
            Columns = (int)Math.Min(cols, int.MaxValue);
            Rows = (int)Math.Min(rows, int.MaxValue);
            Width = (int)Math.Min(width, int.MaxValue);
            Height = (int)Math.Min(height, int.MaxValue);

            if (_nawsEnabled && IsConnected)
            {
                _ = SendNawsAsync();
            }
        }

        private async Task ReadLoopAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] input = ArrayPool<byte>.Shared.Rent(16 * 1024);
            byte[] payload = ArrayPool<byte>.Shared.Rent(16 * 1024);
            byte[] responses = ArrayPool<byte>.Shared.Rent(1024);
            Exception? failure = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int count = await stream.ReadAsync(input.AsMemory(0, input.Length), cancellationToken).ConfigureAwait(false);
                    if (count == 0)
                    {
                        break;
                    }

                    (int payloadLength, int responseLength) = ParseIncoming(
                        input.AsSpan(0, count), payload, responses);

                    if (responseLength > 0)
                    {
                        await SendRawAsync(responses.AsMemory(0, responseLength), cancellationToken).ConfigureAwait(false);
                    }

                    if (payloadLength > 0)
                    {
                        DataReceived?.Invoke(this, Encoding.GetString(payload, 0, payloadLength));
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
            {
                failure = ex;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(input);
                ArrayPool<byte>.Shared.Return(payload);
                ArrayPool<byte>.Shared.Return(responses);
                _isConnected = false;
                stream.Dispose();
                _socket?.Dispose();
                _stream = null;
                _socket = null;
                _readCancellation?.Dispose();
                _readCancellation = null;
                _readTask = null;

                if (!_intentionalDisconnect)
                {
                    ConnectionLost?.Invoke(this, failure);
                }
            }
        }

        private (int PayloadLength, int ResponseLength) ParseIncoming(
            ReadOnlySpan<byte> input, Span<byte> payload, Span<byte> responses)
        {
            int payloadLength = 0;
            int responseLength = 0;

            foreach (byte value in input)
            {
                switch (_parserState)
                {
                    case ParserState.Data:
                        if (value == Iac)
                        {
                            _parserState = ParserState.Command;
                        }
                        else
                        {
                            payload[payloadLength++] = value;
                        }
                        break;
                    case ParserState.Command:
                        if (value == Iac)
                        {
                            payload[payloadLength++] = Iac;
                            _parserState = ParserState.Data;
                        }
                        else if (value is Do or Dont or Will or Wont)
                        {
                            _pendingCommand = value;
                            _parserState = ParserState.Option;
                        }
                        else if (value == Sb)
                        {
                            _parserState = ParserState.SubnegotiationOption;
                        }
                        else
                        {
                            _parserState = ParserState.Data;
                        }
                        break;
                    case ParserState.Option:
                        responseLength += AppendNegotiationResponse(
                            _pendingCommand, value, responses[responseLength..]);
                        _parserState = ParserState.Data;
                        break;
                    case ParserState.SubnegotiationOption:
                        _subnegotiationOption = value;
                        _subnegotiationLength = 0;
                        _parserState = ParserState.Subnegotiation;
                        break;
                    case ParserState.Subnegotiation:
                        if (value == Iac)
                        {
                            _parserState = ParserState.SubnegotiationIac;
                        }
                        else if (_subnegotiationLength < _subnegotiation.Length)
                        {
                            _subnegotiation[_subnegotiationLength++] = value;
                        }
                        break;
                    case ParserState.SubnegotiationIac:
                        if (value == Iac && _subnegotiationLength < _subnegotiation.Length)
                        {
                            _subnegotiation[_subnegotiationLength++] = Iac;
                            _parserState = ParserState.Subnegotiation;
                        }
                        else
                        {
                            if (value == Se)
                            {
                                responseLength += AppendSubnegotiationResponse(responses[responseLength..]);
                            }

                            _parserState = ParserState.Data;
                        }
                        break;
                }
            }

            return (payloadLength, responseLength);
        }

        private int AppendNegotiationResponse(byte command, byte option, Span<byte> destination)
        {
            byte response;

            if (command == Do)
            {
                bool accepted = option is OptionBinary or OptionSuppressGoAhead or OptionTerminalType or OptionNaws;
                response = accepted ? Will : Wont;
                if (option == OptionNaws && accepted)
                {
                    _nawsEnabled = true;
                }
            }
            else if (command == Will)
            {
                response = option is OptionBinary or OptionEcho or OptionSuppressGoAhead ? Do : Dont;
            }
            else
            {
                response = command == Dont ? Wont : Dont;
                if (option == OptionNaws)
                {
                    _nawsEnabled = false;
                }
            }

            destination[0] = Iac;
            destination[1] = response;
            destination[2] = option;
            return 3;
        }

        private int AppendSubnegotiationResponse(Span<byte> destination)
        {
            if (_subnegotiationOption != OptionTerminalType ||
                _subnegotiationLength == 0 ||
                _subnegotiation[0] != 1)
            {
                return 0;
            }

            ReadOnlySpan<byte> terminalType = "xterm-256color"u8;
            destination[0] = Iac;
            destination[1] = Sb;
            destination[2] = OptionTerminalType;
            destination[3] = 0;
            terminalType.CopyTo(destination[4..]);
            int offset = 4 + terminalType.Length;
            destination[offset++] = Iac;
            destination[offset++] = Se;
            return offset;
        }

        private async Task SendPayloadAsync(ReadOnlyMemory<byte> data)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("The BBS is not connected.");
            }

            int iacCount = 0;
            foreach (byte value in data.Span)
            {
                if (value == Iac)
                {
                    iacCount++;
                }
            }

            if (iacCount == 0)
            {
                await SendRawAsync(data, CancellationToken.None).ConfigureAwait(false);
                return;
            }

            byte[] escaped = ArrayPool<byte>.Shared.Rent(data.Length + iacCount);
            int offset = 0;
            try
            {
                foreach (byte value in data.Span)
                {
                    escaped[offset++] = value;
                    if (value == Iac)
                    {
                        escaped[offset++] = Iac;
                    }
                }

                await SendRawAsync(escaped.AsMemory(0, offset), CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(escaped);
            }
        }

        private async Task SendNawsAsync()
        {
            byte[] response =
            [
                Iac, Sb, OptionNaws,
                (byte)(Math.Clamp(Columns, 1, ushort.MaxValue) >> 8),
                (byte)Math.Clamp(Columns, 1, ushort.MaxValue),
                (byte)(Math.Clamp(Rows, 1, ushort.MaxValue) >> 8),
                (byte)Math.Clamp(Rows, 1, ushort.MaxValue),
                Iac, Se
            ];
            await SendRawAsync(response, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task SendRawAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            NetworkStream stream = _stream ?? throw new InvalidOperationException("The BBS is not connected.");
            await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Disconnect();
            _disposed = true;
            _lifecycleGate.Dispose();
            _sendGate.Dispose();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await DisconnectAsync().ConfigureAwait(false);
            _disposed = true;
            _lifecycleGate.Dispose();
            _sendGate.Dispose();
        }
    }
}
