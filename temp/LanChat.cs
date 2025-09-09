using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;

namespace LanChat
{
    /// <summary>
    /// A single class that acts as both chat server and client on a LAN.
    /// - Discovers peers via UDP multicast.
    /// - Exchanges data via TCP (framed protocol).
    /// - Sends/receives text messages and serialized .NET objects.
    /// </summary>
    public sealed class LanChatNode : IAsyncDisposable, IDisposable
    {
        // ----- Public surface -----

        public string NodeId { get; } = Guid.NewGuid().ToString("N");
        public string Name { get; }
        public int TcpPort { get; private set; }

        public event Action<Peer>? PeerDiscovered;
        public event Action<Peer>? PeerLost;
        public event Action<Peer, string>? TextReceived;
        public event Action<Peer, object, Type?>? ObjectReceived;
        public event Action<string>? Log;

        /// <summary>
        /// Construct a node. tcpPort=0 selects an ephemeral port. 
        /// Multicast group/port are defaults that work on most LANs.
        /// </summary>
        public LanChatNode(
            string name,
            int tcpPort = 0,
            string multicastAddress = "239.8.8.8",
            int discoveryPort = 45555)
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Node-{Environment.MachineName}" : name;
            _tcpPortRequested = tcpPort;
            _multicast = IPAddress.Parse(multicastAddress);
            _discoveryPort = discoveryPort;
        }

        /// <summary>Start discovery + TCP listener.</summary>
        public async Task StartAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var token = _cts.Token;

            // Start TCP listener (server side)
            _listener = new TcpListener(IPAddress.Any, _tcpPortRequested);
            _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Start();
            TcpPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(() => AcceptLoopAsync(token), token);

            // Start discovery
            _udp = new UdpClient(AddressFamily.InterNetwork);
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, _discoveryPort));
            _udp.JoinMulticastGroup(_multicast);
            _udp.MulticastLoopback = true;
            _udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);

            _ = Task.Run(() => DiscoveryReceiveLoopAsync(token), token);
            _ = Task.Run(() => AnnounceLoopAsync(token), token);
            _ = Task.Run(() => PeerAgingLoopAsync(token), token);

            // Proactively ping for peers on start
            await SendDiscoveryAsync(token);
            Log?.Invoke($"LanChatNode '{Name}' started on TCP:{TcpPort}");
        }

        /// <summary>Stop all networking and clear peers.</summary>
        public async Task StopAsync()
        {
            if (_disposed) return;

            try { _cts?.Cancel(); } catch { /* ignore */ }

            try { _listener?.Stop(); } catch { /* ignore */ }
            try
            {
                foreach (var kv in _connections)
                {
                    try { kv.Value.Client.Close(); } catch { /* ignore */ }
                }
            }
            catch { /* ignore */ }

            try { _udp?.DropMulticastGroup(_multicast); } catch { /* ignore */ }
            try { _udp?.Close(); } catch { /* ignore */ }

            await Task.Delay(50); // give loops a moment to unwind

            _peers.Clear();
            _connections.Clear();
        }

        /// <summary>Get a snapshot of known peers.</summary>
        public Peer[] GetPeers()
        {
            return _peers.Values.ToArray();
        }

        /// <summary>Connect to a peer over TCP (if not connected yet).</summary>
        public async Task ConnectAsync(Peer peer, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            if (peer is null) throw new ArgumentNullException(nameof(peer));
            var key = peer.Key;

            if (_connections.ContainsKey(key)) return;

            var client = new TcpClient(AddressFamily.InterNetworkV6);
            client.Client.DualMode = true;
            client.NoDelay = true;

            await client.ConnectAsync(peer.Address, peer.Port, ct).ConfigureAwait(false);
            RegisterConnection(peer, client);
        }

        /// <summary>Send a text message to a specific peer or all connected peers (peer == null).</summary>
        public Task SendTextAsync(string text, Peer? peer = null, CancellationToken ct = default)
            => SendPayloadAsync(MessageType.Text, Encoding.UTF8.GetBytes(text ?? string.Empty), peer, ct);

        /// <summary>
        /// Send a .NET object to a peer (or all peers). 
        /// Uses System.Text.Json + an envelope with the assembly-qualified type name to deserialize on the other side.
        /// </summary>
        public Task SendObjectAsync<T>(T obj, Peer? peer = null, CancellationToken ct = default)
        {
            var typeName = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name;
            var envelope = new ObjectEnvelope
            {
                Type = typeName,
                Data = obj
            };
            var json = JsonSerializer.Serialize(envelope, _jsonWriteOpts);
            var bytes = Encoding.UTF8.GetBytes(json);
            return SendPayloadAsync(MessageType.Object, bytes, peer, ct);
        }

        // ----- Internals -----

        private readonly int _tcpPortRequested;
        private readonly IPAddress _multicast;
        private readonly int _discoveryPort;

        private readonly JsonSerializerOptions _jsonReadOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly JsonSerializerOptions _jsonWriteOpts = new(JsonSerializerDefaults.Web);

        private TcpListener? _listener;
        private UdpClient? _udp;
        private CancellationTokenSource? _cts;

        private readonly ConcurrentDictionary<string, Peer> _peers = new();               // key => Peer
        private readonly ConcurrentDictionary<string, Connection> _connections = new();   // key => connection

        private bool _disposed;

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(LanChatNode));
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var client = await _listener!.AcceptTcpClientAsync(token).ConfigureAwait(false);
                    client.NoDelay = true;

                    var remote = (IPEndPoint)client.Client.RemoteEndPoint!;
                    // We don't yet know their NodeId from TCP; we’ll associate by address/port match with a known peer if possible.
                    var key = Peer.KeyFrom(remote.Address, remote.Port);
                    var peer = FindClosestPeer(remote.Address) ?? new Peer
                    {
                        Address = remote.Address,
                        Port = remote.Port,
                        Name = "Unknown",
                        NodeId = "Unknown"
                    };

                    RegisterConnection(peer, client);
                }
            }
            catch (OperationCanceledException) { /* expected on shutdown */ }
            catch (Exception ex)
            {
                Log?.Invoke($"AcceptLoop error: {ex.Message}");
            }
        }

        private void RegisterConnection(Peer peer, TcpClient client)
        {
            var key = peer.Key;
            var conn = new Connection(peer, client);
            if (_connections.TryAdd(key, conn))
            {
                Log?.Invoke($"Connected to {peer}");
                _ = Task.Run(() => ReadLoopAsync(conn, _cts!.Token), _cts!.Token);
            }
            else
            {
                // Already connected; close extraneous
                try { client.Close(); } catch { /* ignore */ }
            }
        }

        private async Task ReadLoopAsync(Connection conn, CancellationToken token)
        {
            using var client = conn.Client; // ensures disposal on exit
            var stream = client.GetStream();

            var header = new byte[5]; // 1 byte type + 4 bytes length (little-endian)
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read header
                    if (!await ReadExactlyAsync(stream, header, token).ConfigureAwait(false)) break;
                    var msgType = (MessageType)header[0];
                    var length = BitConverter.ToInt32(header, 1);
                    if (length < 0 || length > 64 * 1024 * 1024) // 64 MB guard
                        throw new IOException("Invalid frame length.");

                    // Read payload
                    var payload = ArrayPool<byte>.Shared.Rent(length);
                    try
                    {
                        if (!await ReadExactlyAsync(stream, new ArraySegment<byte>(payload, 0, length), token).ConfigureAwait(false))
                            break;

                        switch (msgType)
                        {
                            case MessageType.Text:
                            {
                                var text = Encoding.UTF8.GetString(payload, 0, length);
                                TextReceived?.Invoke(conn.Peer, text);
                                break;
                            }
                            case MessageType.Object:
                            {
                                var json = Encoding.UTF8.GetString(payload, 0, length);
                                var env = JsonSerializer.Deserialize<ObjectEnvelope>(json, _jsonReadOpts);
                                if (env is not null)
                                {
                                    Type? declaredType = null;
                                    object? obj = env.Data;

                                    if (!string.IsNullOrWhiteSpace(env.Type))
                                    {
                                        declaredType = Type.GetType(env.Type!, throwOnError: false);
                                        if (declaredType != null && env.Data is not null)
                                        {
                                            // Re-serialize then deserialize to target type for strongly-typed object
                                            var reJson = JsonSerializer.Serialize(env.Data, _jsonWriteOpts);
                                            obj = JsonSerializer.Deserialize(reJson, declaredType, _jsonReadOpts);
                                        }
                                    }
                                    ObjectReceived?.Invoke(conn.Peer, obj!, declaredType);
                                }
                                break;
                            }
                            default:
                                // Unknown message: ignore
                                break;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(payload);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (IOException) { break; }
                catch (Exception ex)
                {
                    Log?.Invoke($"ReadLoop error from {conn.Peer}: {ex.Message}");
                    break;
                }
            }

            // Connection ended
            _connections.TryRemove(conn.Peer.Key, out _);
            Log?.Invoke($"Disconnected from {conn.Peer}");
        }

        private static async Task<bool> ReadExactlyAsync(NetworkStream stream, ArraySegment<byte> buffer, CancellationToken token)
        {
            var total = 0;
            while (total < buffer.Count)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(total, buffer.Count - total), token).ConfigureAwait(false);
                if (n == 0) return false;
                total += n;
            }
            return true;
        }

        private static Task<bool> ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken token)
            => ReadExactlyAsync(stream, new ArraySegment<byte>(buffer), token);

        private async Task SendPayloadAsync(MessageType type, byte[] payload, Peer? target, CancellationToken ct)
        {
            ThrowIfDisposed();

            async Task SendTo(Connection c)
            {
                try
                {
                    var stream = c.Client.GetStream();
                    Span<byte> header = stackalloc byte[5];
                    header[0] = (byte)type;
                    var lenBytes = BitConverter.GetBytes(payload.Length); // little-endian
                    header[1] = lenBytes[0];
                    header[2] = lenBytes[1];
                    header[3] = lenBytes[2];
                    header[4] = lenBytes[3];

                    await stream.WriteAsync(header, ct).ConfigureAwait(false);
                    await stream.WriteAsync(payload.AsMemory(), ct).ConfigureAwait(false);
                    await stream.FlushAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Send error to {c.Peer}: {ex.Message}");
                }
            }

            if (target is null)
            {
                // broadcast to all active TCP connections
                foreach (var kv in _connections)
                {
                    await SendTo(kv.Value).ConfigureAwait(false);
                }
            }
            else
            {
                var key = target.Key;
                if (_connections.TryGetValue(key, out var conn))
                {
                    await SendTo(conn).ConfigureAwait(false);
                }
                else
                {
                    // Not connected yet: attempt connect then send
                    await ConnectAsync(target, ct).ConfigureAwait(false);
                    if (_connections.TryGetValue(key, out conn))
                        await SendTo(conn).ConfigureAwait(false);
                    else
                        Log?.Invoke($"No connection to {target} to send message.");
                }
            }
        }

        // --------- Discovery (UDP multicast) ---------

        private async Task DiscoveryReceiveLoopAsync(CancellationToken token)
        {
            var udp = _udp!;
            var multicastEp = new IPEndPoint(IPAddress.Any, _discoveryPort);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var result = await udp.ReceiveAsync(token).ConfigureAwait(false);
                    var msg = Encoding.UTF8.GetString(result.Buffer);
                    var remote = result.RemoteEndPoint;

                    if (IsSelf(remote)) continue;

                    if (msg.StartsWith("DISCOVER|", StringComparison.Ordinal))
                    {
                        // Unicast reply to the sender with our info
                        await SendHereUnicastAsync(udp, remote, token).ConfigureAwait(false);
                    }
                    else if (msg.StartsWith("HERE|", StringComparison.Ordinal))
                    {
                        // Format: HERE|<NodeId>|<Name>|<TcpPort>
                        var parts = msg.Split('|');
                        if (parts.Length >= 4 &&
                            int.TryParse(parts[3], out var port))
                        {
                            var peer = new Peer
                            {
                                NodeId = parts[1],
                                Name = parts[2],
                                Address = remote.Address,
                                Port = port,
                                LastSeenUtc = DateTime.UtcNow
                            };

                            // Ignore ourselves (if broadcast loopback)
                            if (peer.NodeId == NodeId) continue;

                            var added = _peers.AddOrUpdate(peer.Key, peer, (_, existing) =>
                            {
                                existing.LastSeenUtc = DateTime.UtcNow;
                                existing.Address = peer.Address; // in case of DHCP changes
                                existing.Port = peer.Port;
                                existing.Name = peer.Name;
                                existing.NodeId = peer.NodeId;
                                return existing;
                            });

                            PeerDiscovered?.Invoke(added);

                            // Optionally autoconnnect:
                            // await ConnectAsync(peer, token).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { /* normal */ }
            catch (ObjectDisposedException) { /* normal on shutdown */ }
            catch (Exception ex)
            {
                Log?.Invoke($"Discovery loop error: {ex.Message}");
            }
        }

        private async Task AnnounceLoopAsync(CancellationToken token)
        {
            // Burst a few quick announcements on start, then periodic
            for (int i = 0; i < 3 && !token.IsCancellationRequested; i++)
            {
                await SendHereMulticastAsync(_udp!, token).ConfigureAwait(false);
                await Task.Delay(500, token);
            }

            while (!token.IsCancellationRequested)
            {
                await SendHereMulticastAsync(_udp!, token).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }

        private async Task PeerAgingLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    foreach (var kv in _peers)
                    {
                        var peer = kv.Value;
                        if ((now - peer.LastSeenUtc) > TimeSpan.FromSeconds(30))
                        {
                            if (_peers.TryRemove(kv.Key, out var removed))
                            {
                                PeerLost?.Invoke(removed);
                            }
                        }
                    }
                }
                catch { /* ignore */ }

                await Task.Delay(5_000, token);
            }
        }

        private async Task SendDiscoveryAsync(CancellationToken token)
        {
            var payload = $"DISCOVER|{NodeId}|{Name}";
            var data = Encoding.UTF8.GetBytes(payload);
            await _udp!.SendAsync(data, data.Length, new IPEndPoint(_multicast, _discoveryPort), token).ConfigureAwait(false);
        }

        private async Task SendHereMulticastAsync(UdpClient udp, CancellationToken token)
        {
            var payload = $"HERE|{NodeId}|{Name}|{TcpPort}";
            var data = Encoding.UTF8.GetBytes(payload);
            await udp.SendAsync(data, data.Length, new IPEndPoint(_multicast, _discoveryPort), token).ConfigureAwait(false);
        }

        private async Task SendHereUnicastAsync(UdpClient udp, IPEndPoint target, CancellationToken token)
        {
            var payload = $"HERE|{NodeId}|{Name}|{TcpPort}";
            var data = Encoding.UTF8.GetBytes(payload);
            await udp.SendAsync(data, data.Length, target, token).ConfigureAwait(false);
        }

        private bool IsSelf(IPEndPoint ep)
        {
            try
            {
                if (ep.Port == TcpPort) return true;
            }
            catch { }
            return false;
        }

        private Peer? FindClosestPeer(IPAddress address)
        {
            foreach (var kv in _peers)
            {
                if (Equals(kv.Value.Address, address))
                    return kv.Value;
            }
            return null;
        }

        // ----- Types & helpers -----

        private enum MessageType : byte
        {
            Text = 1,
            Object = 2
        }

        private sealed class Connection
        {
            public Peer Peer { get; }
            public TcpClient Client { get; }

            public Connection(Peer peer, TcpClient client)
            {
                Peer = peer;
                Client = client;
            }
        }

        private sealed class ObjectEnvelope
        {
            public string? Type { get; set; }
            public object? Data { get; set; }
        }

        public sealed class Peer
        {
            public string NodeId { get; set; } = "";
            public string Name { get; set; } = "";
            public IPAddress Address { get; set; } = IPAddress.Loopback;
            public int Port { get; set; }
            public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

            public string Key => KeyFrom(Address, Port);
            public static string KeyFrom(IPAddress ip, int port) => $"{ip}-{port}";

            public override string ToString() => $"{Name}@{Address}:{Port} ({NodeId[..Math.Min(8, NodeId.Length)]})";
        }

        // ----- Disposal -----

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
            _udp?.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // --------- Minimal usage example (for reference) ----------
    // var node = new LanChat.LanChatNode("BlakePC");
    // node.Log += Console.WriteLine;
    // node.PeerDiscovered += p => Console.WriteLine($"Discovered: {p}");
    // node.TextReceived += (p, s) => Console.WriteLine($"[{p.Name}] {s}");
    // node.ObjectReceived += (p, obj, t) => Console.WriteLine($"[{p.Name}] OBJ type={t?.Name ?? obj?.GetType().Name}, value={obj}");
    // await node.StartAsync();
    //
    // // Later, to send:
    // foreach (var peer in node.GetPeers()) await node.ConnectAsync(peer);
    // await node.SendTextAsync("Hello, LAN!");
    // await node.SendObjectAsync(new { Kind = "Ping", At = DateTimeOffset.Now });
    //
    // // Shutdown:
    // await node.StopAsync();
}
