namespace LanChat.Network
{
    public sealed class ErrorEventArgs : EventArgs
    {
        public required Exception Exception { get; init; }
        public Guid? ClientId { get; init; }
    }
}