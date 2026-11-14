namespace LanChat.Network.Commands;

/// <summary>Lists the registered slash commands.</summary>
public class HelpCommand : ISlashCommand
{
    public string Name => "help";
    public string Usage => "/help";
    public string Description => "Lists the available commands.";

    public Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct)
    {
        var lines = server.SlashCommands
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Select(c => $"{c.Usage} - {c.Description}");

        return Task.FromResult<string?>("Available commands:\n" + string.Join("\n", lines));
    }
}
