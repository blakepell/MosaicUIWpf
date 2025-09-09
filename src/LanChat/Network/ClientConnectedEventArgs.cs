using System.Net;

namespace LanChat.Network
{
    public sealed class ClientConnectionEventArgs : EventArgs
    {
        public required Guid ClientId { get; init; }
        public required IPEndPoint RemoteEndPoint { get; init; }
    }
}