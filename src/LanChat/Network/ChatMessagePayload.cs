namespace LanChat.Network
{
    /// <summary>
    /// Helper payload shape matching server's ChatMessage envelope
    /// </summary>
    public class ChatMessagePayload
    {
        public string? Text { get; set; }
        public Guid SenderId { get; set; }
        public string? Sender { get; set; }
        public string? DisplayName { get; set; }
    }
}
