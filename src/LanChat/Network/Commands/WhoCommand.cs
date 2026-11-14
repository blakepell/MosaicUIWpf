namespace LanChat.Network.Commands;

/// <summary>Lists the users currently logged in to the server.</summary>
public class WhoCommand : ISlashCommand
{
    public string Name => "who";
    public string Usage => "/who";
    public string Description => "Lists the users currently in the chat.";

    public Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct)
    {
        // Only users that have logged in; connections that haven't sent a username yet aren't "in the chat".
        var names = server.Clients
            .Where(c => !string.IsNullOrWhiteSpace(c.Username))
            .OrderBy(c => c.Username, StringComparer.OrdinalIgnoreCase)
            .Select(c => c.Id == user.Id ? $"{c.Username} (you)" : c.Username)
            .ToList();

        var reply = $"Users online ({names.Count}):\n" + string.Join("\n", names);
        return Task.FromResult<string?>(reply);
    }
}
