using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using LanChat.Network.Commands;
using LanChat.Network.Handlers;
using Mosaic.UI.Wpf.Collections;
using System.Reflection;

namespace LanChat.Network;

public partial class ChatServer : ObservableObject, IAsyncDisposable
{
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<TextMessageEventArgs>? TextReceived;
    public event EventHandler<EnvelopeEventArgs>? EnvelopeReceived;
    public event EventHandler<ErrorEventArgs>? Error;

    [ObservableProperty]
    private bool _isListening = false;

    [ObservableProperty]
    private ConcurrentObservableCollection<ChatUser> _clients = new();

    private readonly SemaphoreSlim _broadcastGate = new(1, 1);
    private CancellationTokenSource _cts = new();
    private TcpListener? _listener;
    private readonly ChatServerOptions _options;
    private Task? _acceptLoop;
    private List<IEnvelopeHandler> EnvelopeHandlers { get; }

    /// <summary>
    /// The registered slash commands (listed by /help).
    /// </summary>
    public IReadOnlyList<ISlashCommand> SlashCommands { get; }

    /// <summary>
    /// The TCP port the server listens on.
    /// </summary>
    public int Port => _options.Port;

    public ChatServer(ChatServerOptions options)
    {
        _options = options;

        // Initialize the envelope handlers list and load implementations via reflection.
        EnvelopeHandlers = new List<IEnvelopeHandler>();
        SlashCommands = LoadSlashCommands();

        try
        {
            // Use the assembly that contains this type to find handler implementations.
            var asm = typeof(ChatServer).Assembly;

            var handlerTypes = asm.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IEnvelopeHandler).IsAssignableFrom(t));

            foreach (var ht in handlerTypes)
            {
                try
                {
                    if (Activator.CreateInstance(ht) is IEnvelopeHandler handler)
                    {
                        EnvelopeHandlers.Add(handler);
                    }
                }
                catch (Exception ex)
                {
                    // Surface loader errors through the Error event if someone is listening.
                    Error?.Invoke(this, new ErrorEventArgs { Exception = ex });
                }
            }
        }
        catch (ReflectionTypeLoadException rtlex)
        {
            // Aggregate loader exceptions
            foreach (var le in rtlex.LoaderExceptions ?? [])
            {
                Error?.Invoke(this, new ErrorEventArgs { Exception = le });
            }
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new ErrorEventArgs { Exception = ex });
        }



    }

    private List<ISlashCommand> LoadSlashCommands()
    {
        var commands = new List<ISlashCommand>();

        foreach (var type in typeof(ChatServer).Assembly.GetTypes()
                     .Where(t => !t.IsAbstract && !t.IsInterface && typeof(ISlashCommand).IsAssignableFrom(t)))
        {
            try
            {
                if (Activator.CreateInstance(type) is ISlashCommand command)
                {
                    commands.Add(command);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new ErrorEventArgs { Exception = ex });
            }
        }

        return commands;
    }

    /// <summary>
    /// Runs a slash command for the user and returns the reply to only that user.
    /// </summary>
    private async Task ProcessSlashCommandAsync(ChatUser user, string text, CancellationToken ct)
    {
        var body = text.TrimStart()[1..];
        var split = body.IndexOfAny([' ', '	']);
        var name = split < 0 ? body : body[..split];
        var args = split < 0 ? string.Empty : body[(split + 1)..].Trim();

        var command = SlashCommands.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        string? reply;

        if (command == null)
        {
            reply = $"Unknown command '/{name}'. Type /help for a list of commands.";
        }
        else
        {
            try
            {
                reply = await command.ExecuteAsync(this, user, args, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Error?.Invoke(this, new ErrorEventArgs { Exception = ex, ClientId = user.Id });
                reply = $"The command '/{name}' failed.";
            }
        }

        if (!string.IsNullOrEmpty(reply))
        {
            await SendSystemMessageToAsync(user, reply, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Send a server (system) message to a single user.</summary>
    public async Task SendSystemMessageToAsync(ChatUser user, string message, CancellationToken ct = default)
    {
        var env = MessageEnvelope.Create(new { Text = message, Sender = "System", IsPrivate = true }, typeName: "SystemMessage");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(env, MessageEnvelope.DefaultJsonOptions);
        await user.SendAsync(MessageKind.JsonEnvelope, bytes, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a private chat message to one user, plus an echo to the sender, without involving the rest of the room.
    /// </summary>
    public async Task SendPrivateMessageAsync(ChatUser sender, ChatUser recipient, string text, CancellationToken ct = default)
    {
        var toRecipient = MessageEnvelope.Create(new
        {
            Text = text,
            SenderId = sender.Id,
            Sender = sender.Username,
            DisplayName = sender.DisplayName,
            IsPrivate = true
        }, typeName: "ChatMessage");

        var echo = MessageEnvelope.Create(new
        {
            Text = text,
            SenderId = sender.Id,
            Sender = sender.Username,
            DisplayName = sender.DisplayName,
            IsPrivate = true,
            Recipient = recipient.DisplayName
        }, typeName: "ChatMessage");

        await recipient.SendAsync(MessageKind.JsonEnvelope, JsonSerializer.SerializeToUtf8Bytes(toRecipient, MessageEnvelope.DefaultJsonOptions), ct).ConfigureAwait(false);
        await sender.SendAsync(MessageKind.JsonEnvelope, JsonSerializer.SerializeToUtf8Bytes(echo, MessageEnvelope.DefaultJsonOptions), ct).ConfigureAwait(false);
    }

    /// <summary>Broadcast a server (system) message to everyone except one user.</summary>
    public async Task BroadcastSystemMessageExceptAsync(Guid excludedId, string message, CancellationToken ct = default)
    {
        var env = MessageEnvelope.Create(new { Text = message, Sender = "System" }, typeName: "SystemMessage");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(env, MessageEnvelope.DefaultJsonOptions);

        await _broadcastGate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            await Task.WhenAll(_clients.Where(c => c.Id != excludedId)
                .Select(c => c.SendAsync(MessageKind.JsonEnvelope, bytes, ct))).ConfigureAwait(false);
        }
        finally
        {
            _broadcastGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }

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

                            // Slash commands are handled by the server and are not broadcast.
                            if (text.TrimStart().StartsWith('/'))
                            {
                                await ProcessSlashCommandAsync(user, text, ct).ConfigureAwait(false);
                                break;
                            }

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

                            foreach (var handler in EnvelopeHandlers)
                            {
                                if (!string.IsNullOrEmpty(env.TypeName) && string.Equals(env.TypeName, handler.TypeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    await handler.HandleAsync(user, env, ct);
                                }
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
            // ignore
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
    public async Task BroadcastSystemMessageAsync(string message, CancellationToken ct = default)
    {
        var env = MessageEnvelope.Create(new { Text = message, Sender = "System" }, typeName: "SystemMessage");
        await BroadcastEnvelopeAsync(env, ct).ConfigureAwait(false);
    }

    public async Task BroadcastAsync(MessageKind kind, ReadOnlyMemory<byte> payload, CancellationToken ct)
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