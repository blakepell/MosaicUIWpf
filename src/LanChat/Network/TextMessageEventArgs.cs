namespace LanChat.Network
{
    public sealed class TextMessageEventArgs : EventArgs
    {
        public required Guid ClientId { get; init; }
        public required string Text { get; init; }
        public required string Username { get; init; }
    }
}