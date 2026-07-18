/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;
using Cysharp.Text;

namespace BbsNavigator.Models
{
    /// <summary>
    /// Defines a saved bulletin board system endpoint.
    /// </summary>
    public partial class BbsProfile : ObservableObject
    {
        /// <summary>
        /// Gets or sets the unique profile identifier.
        /// </summary>
        /// <value>The identifier used to associate the profile with an open session.</value>
        [ObservableProperty]
        private Guid _id = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the BBS display name.
        /// </summary>
        /// <value>The name shown in the BBS directory.</value>
        [property: Category("BBS")]
        [ObservableProperty]
        private string _name = "New BBS";

        /// <summary>
        /// Gets or sets the BBS host name or IP address.
        /// </summary>
        /// <value>The network host used to establish the Telnet connection.</value>
        [property: Category("Connection")]
        [ObservableProperty]
        private string _host = "localhost";

        /// <summary>
        /// Gets or sets the BBS Telnet port.
        /// </summary>
        /// <value>The network port used to establish the Telnet connection.</value>
        [property: Category("Connection")]
        [ObservableProperty]
        private int _port = 23;

        /// <summary>
        /// Gets or sets the date and time of the most recent successful connection.
        /// </summary>
        /// <value>The local connection time, or <see langword="null"/> when the profile has not connected.</value>
        [property: Category("Connection")]
        [ObservableProperty]
        private DateTime? _lastConnected;

        /// <summary>
        /// Gets or sets the number of successful connections made to this BBS.
        /// </summary>
        /// <value>The number of successful connections.</value>
        [property: Category("Connection")]
        [property: DisplayName("Successful Connections")]
        [ObservableProperty]
        private int _connectionCount;

        /// <summary>
        /// Gets or sets the BBS description.
        /// </summary>
        /// <value>The descriptive text shown in the BBS directory.</value>
        [property: Category("BBS")]
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>
        /// Gets or sets a value that indicates whether this BBS is a favorite.
        /// </summary>
        /// <value><see langword="true"/> when the BBS is a favorite; otherwise, <see langword="false"/>.</value>
        [property: Category("BBS")]
        [property: DisplayName("Favorite")]
        [ObservableProperty]
        private bool _favorite;

        /// <summary>
        /// Gets or sets a value that indicates whether the session reconnects automatically.
        /// </summary>
        /// <value><see langword="true"/> to reconnect after an unexpected disconnect; otherwise, <see langword="false"/>.</value>
        [property: Category("Connection")]
        [property: DisplayName("Reconnect automatically")]
        [ObservableProperty]
        private bool _autoReconnect = true;

        /// <summary>
        /// Gets or sets a value that indicates whether typed characters are echoed locally.
        /// </summary>
        /// <value><see langword="true"/> to display keyboard input immediately; otherwise, <see langword="false"/>.</value>
        [property: Category("Terminal")]
        [property: DisplayName("Show typed characters locally")]
        [property: Description("Displays keyboard input immediately for systems that do not negotiate or provide remote echo.")]
        [ObservableProperty]
        private bool _localEcho;

        /// <summary>
        /// Gets or sets a value that indicates whether Backspace sends the DEL character.
        /// </summary>
        /// <value><see langword="true"/> to send DEL; otherwise, <see langword="false"/> to send BS.</value>
        [property: Category("Terminal")]
        [property: DisplayName("Send DEL for Backspace")]
        [property: Description("Sends DEL (0x7F) instead of Ctrl-H/BS (0x08) for systems that require it.")]
        [ObservableProperty]
        private bool _backspaceSendsDelete;

        /// <summary>
        /// Gets or sets the encoding used to interpret terminal text.
        /// </summary>
        /// <value>One of the enumeration values that specifies the terminal encoding.</value>
        [property: Category("Terminal")]
        [property: DisplayName("Text encoding")]
        [property: Description("How incoming bytes are interpreted. CP437 renders classic DOS box-drawing and ANSI art correctly.")]
        [ObservableProperty]
        private BbsEncoding _terminalEncoding = BbsEncoding.Cp437;

        /// <summary>
        /// Gets or sets the current connection state.
        /// </summary>
        /// <value>One of the enumeration values that describes the BBS session state.</value>
        [JsonIgnore]
        [ObservableProperty]
        private BbsConnectionState _connectionState = BbsConnectionState.Disconnected;

        /// <summary>
        /// Gets or sets the protected credential envelope associated with the profile.
        /// </summary>
        /// <value>The encrypted credentials, or <see langword="null"/> when none are stored.</value>
        [property: Browsable(false)]
        [ObservableProperty]
        private string? _encryptedCredentials;

        /// <summary>
        /// Gets a value that indicates whether this profile contains an encrypted credential record.
        /// </summary>
        [JsonIgnore]
        public bool HasCredentials => !string.IsNullOrWhiteSpace(EncryptedCredentials);

        /// <summary>
        /// Gets the host and port in display form.
        /// </summary>
        [JsonIgnore]
        public string Endpoint => $"{Host}:{Port}";

        partial void OnHostChanged(string value)
        {
            OnPropertyChanged(nameof(Endpoint));
        }

        partial void OnPortChanged(int value)
        {
            OnPropertyChanged(nameof(Endpoint));
        }

        partial void OnEncryptedCredentialsChanged(string? value)
        {
            OnPropertyChanged(nameof(HasCredentials));
        }

        /// <summary>
        /// Overridden ToString() returns a key used in searching on the UI.
        /// </summary>
        public override string ToString()
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.Append($"{Name},{Host}:{Port},{Description}");
                return sb.ToString();
            }
        }
    }
}
