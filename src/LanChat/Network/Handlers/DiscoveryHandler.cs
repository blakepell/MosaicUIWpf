using System.Text.Json;
using Argus.Memory;
using LanChat.Common;

namespace LanChat.Network.Handlers
{
    /// <summary>
    /// Handler to respond to discovery requests from clients looking for chat servers.
    /// </summary>
    public class DiscoveryHandler : IEnvelopeHandler
    {
        /// <inheritdoc cref="TypeName"/>
        public string TypeName { get; } = "DiscoveryRequest";

        /// <inheritdoc cref="HandleAsync"/>
        public async Task HandleAsync(ChatUser user, object envelope, CancellationToken ct = default)
        {
            var appSettings = AppServices.GetRequiredService<AppSettings>();
            var respPayload = new { IsChatServer = true, ServerName = appSettings.ServerName, Name = appSettings.ServerName };
            var respEnv = MessageEnvelope.Create(respPayload, typeName: "DiscoveryResponse");
            var bytes = JsonSerializer.SerializeToUtf8Bytes(respEnv, MessageEnvelope.DefaultJsonOptions);
            // Send only to the requesting client
            await user.SendAsync(MessageKind.JsonEnvelope, bytes, ct).ConfigureAwait(false);
        }
    }
}
