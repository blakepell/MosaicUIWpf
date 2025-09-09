namespace LanChat.Network
{
    public sealed class EnvelopeEventArgs : EventArgs
    {
        public required Guid ClientId { get; init; }
        public required MessageEnvelope Envelope { get; init; }
        public required string Username { get; init; }
    }
}