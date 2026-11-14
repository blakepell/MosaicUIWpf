namespace LanChat.Network.Commands;

/// <summary>Gracefully closes the user's connection to the server.</summary>
public class QuitCommand : ISlashCommand
{
    public string Name => "quit";
    public string Usage => "/quit";
    public string Description => "Disconnects you from the chat.";

    public Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct)
    {
        // The server closes the connection after this reply has been sent.
        user.DisconnectRequested = true;
        return Task.FromResult<string?>("Goodbye!");
    }
}

/// <summary>Same as <see cref="QuitCommand"/>, under the name /logout.</summary>
public class LogoutCommand : ISlashCommand
{
    public string Name => "logout";
    public string Usage => "/logout";
    public string Description => "Disconnects you from the chat (same as /quit).";

    public Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct)
    {
        user.DisconnectRequested = true;
        return Task.FromResult<string?>("Goodbye!");
    }
}
