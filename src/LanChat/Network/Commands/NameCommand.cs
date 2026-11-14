namespace LanChat.Network.Commands;

/// <summary>
/// Renames the user on the server.  The user's local setting is not changed.
/// </summary>
public class NameCommand : ISlashCommand
{
    private const int MaxLength = 32;

    public string Name => "name";
    public string Usage => "/name <new name>";
    public string Description => "Changes your name on this server (your local setting is unchanged).";

    public async Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct)
    {
        var newName = args.Trim();

        if (newName.Length == 0)
        {
            return $"Usage: {Usage}";
        }

        if (newName.Length > MaxLength)
        {
            return $"Names can be at most {MaxLength} characters.";
        }

        var oldName = user.DisplayName;

        if (string.Equals(oldName, newName, StringComparison.Ordinal))
        {
            return $"You are already known as {newName}.";
        }

        user.Username = newName;

        // Let everyone else know; the requester gets the returned reply.
        await server.BroadcastSystemMessageExceptAsync(user.Id, $"{oldName} is now known as {newName}.", ct).ConfigureAwait(false);

        return $"You are now known as {newName}.";
    }
}
