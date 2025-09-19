using System.Drawing.Text;
using System.Net;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LanChat.Network
{
    /// <summary>
    /// Represents a connected chat user with core properties and I/O capabilities.
    /// </summary>
    public partial class ChatUser : ObservableObject, IDisposable
    {
        private readonly SemaphoreSlim _sendGate = new(1, 1);

        public ChatUser(Guid id, TcpClient tcp)
        {
            Id = id;
            Tcp = tcp;
            Tcp.NoDelay = true;
            ConnectedAt = DateTime.Now;
            LastActivity = DateTime.Now;

            try
            {
                if (tcp?.Client?.RemoteEndPoint is IPEndPoint remote)
                {
                    IpAddress = remote.Address.ToString();
                }
                else
                {
                    IpAddress = tcp?.Client?.RemoteEndPoint?.ToString() ?? "n/a";
                }
            }
            catch
            {
                IpAddress = "n/a";
            }
        }

        /// <summary>
        /// Unique identifier for this user.
        /// </summary>
        [ObservableProperty]
        private Guid _id;

        /// <summary>
        /// The TCP connection for this user.
        /// </summary>
        public TcpClient Tcp { get; }

        /// <summary>
        /// Username provided during login.
        /// </summary>
        [ObservableProperty]
        private string _username = string.Empty;

        /// <summary>
        /// When this user connected to the server.
        /// </summary>
        [ObservableProperty]
        private DateTime _connectedAt;

        /// <summary>
        /// Last time this user sent a message or was active.
        /// </summary>
        [ObservableProperty]
        private DateTime _lastActivity;

        /// <summary>
        /// The number of messages this user has sent.
        /// </summary>
        [ObservableProperty] 
        private int _messagesSent = 0;

        /// <summary>
        /// Gets or sets the IP address associated with the object.
        /// </summary>
        [ObservableProperty]
        public string _ipAddress = "n/a";

        /// <summary>
        /// Get display name for this user (username or fallback).
        /// </summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(Username) ? Username : $"User {Id.ToString()[..8]}";

        public void Dispose()
        {
            try
            {
                Tcp.Close();
            }
            catch
            {
                /* ignore */
            }

            Tcp.Dispose();
            _sendGate.Dispose();
        }

        /// <summary>
        /// Send a message to this user.
        /// </summary>
        public async Task SendAsync(MessageKind kind, ReadOnlyMemory<byte> payload, CancellationToken ct)
        {
            await _sendGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var stream = Tcp.GetStream();
                await Framing.WriteAsync(stream, kind, payload, ct).ConfigureAwait(false);
            }
            finally
            {
                _sendGate.Release();
            }
        }
    }
}
