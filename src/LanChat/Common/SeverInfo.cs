using CommunityToolkit.Mvvm.ComponentModel;

namespace LanChat.Common
{
    /// <summary>
    /// Represents information required to identify and connect to a LAN chat server.
    /// </summary>
    public partial class SeverInfo : ObservableObject
    {
        /// <summary>
        /// The friendly name of the server.
        /// </summary>
        [ObservableProperty]
        private string _serverName = string.Empty;

        /// <summary>
        /// The network IP address of the server.
        /// </summary>
        [ObservableProperty]
        private string _ipAddress = string.Empty;

        /// <summary>
        /// The network port on which the server is listening.
        /// </summary>
        [ObservableProperty]
        private int _port = 4000;

        /// <summary>
        /// Returns a human-readable representation of the server information.
        /// Format: "{ServerName} ({IpAddress}:{Port})".
        /// </summary>
        /// <returns>A formatted string containing the server name, IP address, and port.</returns>
        public override string ToString()
        {
            return $"{this.ServerName} ({this.IpAddress}:{this.Port})";
        }
    }
}
