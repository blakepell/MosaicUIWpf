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
        [ObservableProperty]
        private Guid _id = Guid.NewGuid();

        [property: Category("BBS")]
        [ObservableProperty]
        private string _name = "New BBS";

        [property: Category("Connection")]
        [ObservableProperty]
        private string _host = "localhost";

        [property: Category("Connection")]
        [ObservableProperty]
        private int _port = 23;

        [property: Category("Connection")]
        [ObservableProperty]
        private DateTime? _lastConnected;

        [property: Category("BBS")]
        [ObservableProperty]
        private string _description = string.Empty;

        [property: Category("Connection")]
        [property: DisplayName("Reconnect automatically")]
        [ObservableProperty]
        private bool _autoReconnect = true;

        [property: Category("Terminal")]
        [property: DisplayName("Show typed characters locally")]
        [property: Description("Displays keyboard input immediately for systems that do not negotiate or provide remote echo.")]
        [ObservableProperty]
        private bool _localEcho;

        [property: Category("Terminal")]
        [property: DisplayName("Send DEL for Backspace")]
        [property: Description("Sends DEL (0x7F) instead of Ctrl-H/BS (0x08) for systems that require it.")]
        [ObservableProperty]
        private bool _backspaceSendsDelete;

        [property: Category("Terminal")]
        [property: DisplayName("Text encoding")]
        [property: Description("How incoming bytes are interpreted. CP437 renders classic DOS box-drawing and ANSI art correctly.")]
        [ObservableProperty]
        private BbsEncoding _terminalEncoding = BbsEncoding.Cp437;

        [JsonIgnore]
        [ObservableProperty]
        private BbsConnectionState _connectionState = BbsConnectionState.Disconnected;

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
