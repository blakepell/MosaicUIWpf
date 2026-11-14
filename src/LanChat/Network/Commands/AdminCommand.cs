namespace LanChat.Network.Commands;

/// <summary>Toggles whether another user is an admin.</summary>
public class AdminCommand : ISlashCommand
{
    public string Name => "admin";
    public string Usage => "/admin <username>";
    public string Description => "Toggles whether a user is an admin.";
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
            return $"More than one user is named '{args}', so nothing was changed.";
        }

        var target = matches[0];

        if (target.Id == user.Id)
        {
            return "You can't change your own admin status.";
        }

        target.IsAdmin = !target.IsAdmin;

        var notice = target.IsAdmin ? "You are now an admin." : "You are no longer an admin.";
        await server.SendSystemMessageToAsync(target, notice, ct).ConfigureAwait(false);

        return $"{target.DisplayName} is {(target.IsAdmin ? "now" : "no longer")} an admin.";
    }
}
