namespace LanChat.Network
{
    /// <summary>
    /// Helper payload shape for system messages
    /// </summary>
    public class SystemMessagePayload
    {
        public string? Text { get; set; }
        public string? Sender { get; set; }
    }
}
