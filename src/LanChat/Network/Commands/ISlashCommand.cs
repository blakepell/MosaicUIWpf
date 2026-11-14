namespace LanChat.Network.Commands;

/// <summary>
/// A slash command (e.g. "/name Blake") that is processed by the server instead of being
/// broadcast as chat.  Implementations are discovered via reflection and registered with the
/// <see cref="ChatServer"/>; they must have a public parameterless constructor.
/// </summary>
public interface ISlashCommand
{
    /// <summary>The command name without the leading slash (e.g. "name").</summary>
    string Name { get; }

    /// <summary>Short usage text shown by /help (e.g. "/name &lt;new name&gt;").</summary>
    string Usage { get; }

    /// <summary>One line description shown by /help.</summary>
    string Description { get; }

    /// <summary>
    /// When true only admins can run the command and see it in /help.
    /// </summary>
    bool AdminOnly => false;

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="server">The server processing the command.</param>
    /// <param name="user">The user who issued the command.</param>
    /// <param name="args">Everything after the command name, trimmed (may be empty).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Text sent back to only the issuing user as a server message, or null for none.</returns>
    Task<string?> ExecuteAsync(ChatServer server, ChatUser user, string args, CancellationToken ct);
}
