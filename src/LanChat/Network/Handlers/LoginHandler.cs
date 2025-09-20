using Argus.Memory;
using System.Text.Json;

namespace LanChat.Network.Handlers
{
    /// <summary>
    /// Handles login-related messages by processing the provided envelope and updating the user's state.
    /// </summary>
    /// <remarks>
    /// Because this is intended for local area networks we don't require passwords, the user simply
    /// announces who they are.
    /// </remarks>
    public class LoginHandler : IEnvelopeHandler
    {
        /// <inheritdoc cref="TypeName"/>
        public string TypeName { get; } = "Login";

        /// <inheritdoc cref="HandleAsync"/>
        public async Task HandleAsync(ChatUser user, object envelope, CancellationToken ct = default)
        {
            MessageEnvelope? env = envelope as MessageEnvelope?;

            if (env == null)
            {
                return;
            }

            var loginData = JsonSerializer.Deserialize<Dictionary<string, string>>(env.Value.Json, MessageEnvelope.DefaultJsonOptions);

            if (loginData?.TryGetValue("username", out var username) == true && !string.IsNullOrWhiteSpace(username))
            {
                var server = AppServices.GetRequiredService<ChatServer>();
                user.Username = username.Trim();
                user.LastActivity = DateTime.Now;
                user.MessagesSent++;

                // Send system announcement about the newly logged in user
                var announcement = $"{user.DisplayName} has joined the chat.";
                await server.BroadcastSystemMessageAsync(announcement, ct).ConfigureAwait(false);
            }
        }
    }
}
