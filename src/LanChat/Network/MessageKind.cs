namespace LanChat.Network
{
    /// <summary>
    /// Known message kinds for the chat transport. You may add custom type ids if desired.
    /// </summary>
    public enum MessageKind
    {
        /// <summary>
        /// Plain UTF-8 text chat message.
        /// </summary>
        Text = 1,

        /// <summary>
        /// JSON-serialized object payload inside an envelope.
        /// </summary>
        JsonEnvelope = 2
    }
}