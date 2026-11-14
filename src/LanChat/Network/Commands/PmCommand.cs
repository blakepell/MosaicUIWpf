namespace LanChat.Network.Commands;

/// <summary>
/// Sends a private message to one user that is not shown to the rest of the room.
/// </summary>
public class PmCommand : ISlashCommand
{
    public string Name => "pm";
    public string Usage => "/pm <username> <message>";
    public string Description => "Sends a private message that only that user sees.";

    public async Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct)
    {
        var split = args.IndexOfAny([' ', '\t']);

        if (split <= 0 || string.IsNullOrWhiteSpace(args[(split + 1)..]))
        {
            return $"Usage: {Usage}";
        }

        var targetName = args[..split];
        var message = args[(split + 1)..].Trim();

        var matches = server.Clients
            .Where(c => string.Equals(c.DisplayName, targetName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return $"No user named '{targetName}' is connected.";
        }

        if (matches.Count > 1)
        {
            return $"More than one user is named '{targetName}', so the message was not sent.";
        }

        var target = matches[0];

        if (target.Id == user.Id)
        {
            return "You can't send a private message to yourself.";
        }

        await server.SendPrivateMessageAsync(user, target, message, ct).ConfigureAwait(false);
        return null;
    }
}
