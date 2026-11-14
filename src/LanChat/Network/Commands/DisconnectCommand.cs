namespace LanChat.Network.Commands;

/// <summary>Forcibly disconnects a user from the chat.</summary>
public class DisconnectCommand : ISlashCommand
{
    public string Name => "disconnect";
    public string Usage => "/disconnect <username>";
    public string Description => "Disconnects a user from the chat.";
    public bool AdminOnly => true;

    public async Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return $"Usage: {Usage}";
        }

        var matches = server.Clients
            .Where(c => string.Equals(c.DisplayName, args, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return $"No user named '{args}' is connected.";
        }

        if (matches.Count > 1)
        {
            return $"More than one user is named '{args}', so nobody was disconnected.";
        }

        var target = matches[0];

        if (target.Id == user.Id)
        {
            return "You can't disconnect yourself, use /quit instead.";
        }

        var message = $"{target.DisplayName} on {target.IpAddress} was disconnected";
        await server.DisconnectUserAsync(target, "You were disconnected by an admin.", ct).ConfigureAwait(false);

        return message;
    }
}
