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
    }
}