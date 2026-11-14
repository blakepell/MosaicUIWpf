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

        /// <summary>True when the message was sent to a single user only.</summary>
        public bool IsPrivate { get; set; }

        /// <summary>On the sender's echo of a private message, the user it was sent to.</summary>
        public string? Recipient { get; set; }
    }
}
