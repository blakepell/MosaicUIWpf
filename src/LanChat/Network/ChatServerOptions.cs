namespace LanChat.Network
{
    public sealed class ChatServerOptions
    {
        /// <summary>
        /// TCP port to listen on.
        /// </summary>
        public required int Port { get; init; }

        /// <summary>
        /// Human-friendly server name.
        /// </summary>
        public string Name { get; init; } = $"{Environment.MachineName}";

        /// <summary>
        /// Optional message processor. If provided and returns a non-null envelope, that envelope is broadcast instead of the original.
        /// Use this to validate, transform, or enrich payloads server-side.
        /// </summary>
        public Func<MessageEnvelope, MessageEnvelope?>? EnvelopeProcessor { get; init; }
    }
}