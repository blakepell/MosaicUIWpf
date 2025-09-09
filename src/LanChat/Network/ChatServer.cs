using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Collections;

namespace LanChat.Network;

public partial class ChatServer : ObservableObject, IAsyncDisposable
{
    [ObservableProperty]
    private bool _isListening = false;

    [ObservableProperty]
    private ConcurrentObservableCollection<ChatUser> _clients = new();

    private readonly SemaphoreSlim _broadcastGate = new(1, 1);
    private CancellationTokenSource _cts = new();
    private TcpListener? _listener;
    private readonly ChatServerOptions _options;
    private Task? _acceptLoop;

    public ChatServer(ChatServerOptions options)
    {
        _options = options;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<TextMessageEventArgs>? TextReceived;
    public event EventHandler<EnvelopeEventArgs>? EnvelopeReceived;
    public event EventHandler<ErrorEventArgs>? Error;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Reset the cancellation token source for restarts
            if (_cts.IsCancellationRequested)
            {
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }

            // Create a new listener if it's null (first start) or not active (after a stop)
            if (_listener is null || !_listener.Server.IsBound)
            {
                _listener = new TcpListener(IPAddress.Any, _options.Port);
            }

            _listener.Start(128);
            IsListening = true;

            _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));

            // Link external cancellation
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => _cts.Cancel());
            }

            await Task.CompletedTask;
        }
        catch
        {
            // Set the IsListening, then propagate the exception up.
            IsListening = false;
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (!IsListening)
        {
            return;
        }

        try
        {
            try
            {
                _cts.Cancel();
            }
            catch
            {
                /* ignore */
            }

            _listener?.Stop();

            if (_acceptLoop is not null)
            {
                await _acceptLoop.ConfigureAwait(false);
            }

            foreach (var kvp in _clients)
            {
                kvp.Dispose();
            }

            _clients.Clear();
        }
        finally
        {
            IsListening = false;
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
            try
            {
                var tcp = await _listener!.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                _ = HandleClientAsync(tcp, ct); // fire & forget
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs { Exception = ex });
            }
    }

    private async Task HandleClientAsync(TcpClient tcp, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var user = new ChatUser(id, tcp);
        _clients.Add(user);

        var remote = (IPEndPoint)tcp.Client.RemoteEndPoint!;
        ClientConnected?.Invoke(this, new ClientConnectionEventArgs { ClientId = id, RemoteEndPoint = remote });

        try
        {
            var stream = tcp.GetStream();
            while (!ct.IsCancellationRequested)
            {
                var (kind, payload) = await Framing.ReadAsync(stream, ct).ConfigureAwait(false);
                switch (kind)
                {
                    case MessageKind.Text:
                        {
                            var text = Encoding.UTF8.GetString(payload);
                            user.LastActivity = DateTime.Now;
                            user.MessagesSent++;

                            TextReceived?.Invoke(this, new TextMessageEventArgs 
                            { 
                                ClientId = id, 
                                Text = text, 
                                Username = user.Username 
                            });

                            // Broadcast as enriched envelope so recipients know the sender identity.
                            await BroadcastClientTextAsync(id, text, ct).ConfigureAwait(false);
                            break;
                        }
                    case MessageKind.JsonEnvelope:
                        {
                            var env = JsonSerializer.Deserialize<MessageEnvelope>(payload, MessageEnvelope.DefaultJsonOptions);
                            
                            // Handle login/announce messages
                            if (!string.IsNullOrEmpty(env.TypeName) && string.Equals(env.TypeName, "Login", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var loginData = JsonSerializer.Deserialize<Dictionary<string, string>>(env.Json, MessageEnvelope.DefaultJsonOptions);

                                    if (loginData?.TryGetValue("username", out var username) == true && !string.IsNullOrWhiteSpace(username))
                                    {
                                        user.Username = username.Trim();
                                        user.LastActivity = DateTime.Now;
                                        user.MessagesSent++;

                                        // Send system announcement about the newly logged in user
                                        var announcement = $"{user.DisplayName} has joined the chat.";
                                        await BroadcastSystemMessageAsync(announcement, ct).ConfigureAwait(false);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Error?.Invoke(this, new ErrorEventArgs { Exception = ex, ClientId = id });
                                }

                                break; // Don't broadcast login messages
                            }

                            // Optional server-side processing
                            if (_options.EnvelopeProcessor is { } proc)
                            {
                                var processed = proc(env);
                                if (processed is null)
                                {
                                    break; // drop
                                }
                                env = processed.Value;
                            }

                            EnvelopeReceived?.Invoke(this, new EnvelopeEventArgs 
                            { 
                                ClientId = id, 
                                Envelope = env, 
                                Username = user.Username 
                            });
                            await BroadcastEnvelopeAsync(env, ct).ConfigureAwait(false);
                            break;
                        }
                    default:
                        throw new NotSupportedException($"Unknown message kind: {kind}");
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (IOException) // remote closed
        {
            // ignore
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new ErrorEventArgs { Exception = ex, ClientId = id });
        }
        finally
        {
            var removed = _clients.FirstOrDefault(x => x.Id.Equals(id));

            if (removed != null)
            {
                _clients.Remove(removed);

                // Send disconnection announcement if user was logged in
                if (!string.IsNullOrWhiteSpace(removed.Username))
                {
                    try
                    {
                        var announcement = $"{removed.DisplayName} has left the chat.";
                        await BroadcastSystemMessageAsync(announcement, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke(this, new ErrorEventArgs { Exception = ex, ClientId = id });
                    }
                }

                removed.Dispose();
            }

            ClientDisconnected?.Invoke(this, new ClientConnectionEventArgs { ClientId = id, RemoteEndPoint = remote });
        }
    }

    /// <summary>Broadcast a UTF-8 text message to all connected clients.</summary>
    public async Task BroadcastTextAsync(string text, CancellationToken ct = default)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await BroadcastAsync(MessageKind.Text, bytes, ct).ConfigureAwait(false);
    }

    /// <summary>Broadcast a JSON envelope to all connected clients.</summary>
    public async Task BroadcastEnvelopeAsync(MessageEnvelope envelope, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(envelope, MessageEnvelope.DefaultJsonOptions);
        await BroadcastAsync(MessageKind.JsonEnvelope, bytes, ct).ConfigureAwait(false);
    }

    /// <summary>Broadcast a client's text as an enriched envelope ("ChatMessage").</summary>
    public async Task BroadcastClientTextAsync(Guid senderId, string text, CancellationToken ct = default)
    {
        var sender = _clients.FirstOrDefault(x => x.Id.Equals(senderId));

        if (sender == null)
        {
            // fallback to plain text if sender missing
            await BroadcastTextAsync(text, ct).ConfigureAwait(false);
            return;
        }

        var payload = new
        {
            Text = text,
            SenderId = senderId,
            Sender = sender.Username,
            DisplayName = sender.DisplayName
        };

        var env = MessageEnvelope.Create(payload, typeName: "ChatMessage");
        await BroadcastEnvelopeAsync(env, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Create a system envelope from the supplied message and broadcast it to all clients.
    /// </summary>
    private async Task BroadcastSystemMessageAsync(string message, CancellationToken ct = default)
    {
        var env = MessageEnvelope.Create(new { Text = message, Sender = "System" }, typeName: "SystemMessage");
        await BroadcastEnvelopeAsync(env, ct).ConfigureAwait(false);
    }

    private async Task BroadcastAsync(MessageKind kind, ReadOnlyMemory<byte> payload, CancellationToken ct)
    {
        // Serialize writes across clients to avoid interleaving on slow connections.
        await _broadcastGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var tasks = new List<Task>(_clients.Count);
            foreach (var kvp in _clients)
            {
                tasks.Add(kvp.SendAsync(kind, payload, ct));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            _broadcastGate.Release();
        }
    }
}