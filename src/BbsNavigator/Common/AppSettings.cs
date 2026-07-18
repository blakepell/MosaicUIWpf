/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Contains settings persisted by the Mosaic application host.
    /// </summary>
    public partial class AppSettings : ObservableObject, IAppSettings
    {
        [property: Category("File System")]
        [property: DisplayName("Application Data Folder")]
        [property: ReadOnly(true)]
        [ObservableProperty]
        private string? _applicationDataFolder;

        [property: Browsable(false)]
        [JsonIgnore]
        [ObservableProperty]
        private LocalSettings? _clientSettings = new();

        [property: Browsable(false)]
        [ObservableProperty]
        private MosaicThemeMode _theme = MosaicThemeMode.Blue;

        private string? _credentialEncryptionVerifier;

        /// <summary>
        /// Gets or sets the encrypted verifier for the application-wide credential passphrase.
        /// </summary>
        /// <value>
        /// A protected verifier used to confirm the passphrase without persisting the passphrase itself.
        /// </value>
        [Browsable(false)]
        public string? CredentialEncryptionVerifier
        {
            get => _credentialEncryptionVerifier;
            set => SetProperty(ref _credentialEncryptionVerifier, value);
        }

        [property: Category("Terminal")]
        [property: DisplayName("Terminal Font Size")]
        [property: Description("The default terminal font size.")]
        [ObservableProperty]
        private double _fontSize = 15.0;

        private double _userGuideFontSize = 12.0;

        /// <summary>
        /// Gets or sets the persisted base font size used by the Markdown user guide.
        /// </summary>
        [Category("Appearance")]
        [DisplayName("User Guide Font Size")]
        [Description("The Markdown user guide font size. Hold Ctrl and use the mouse wheel over the guide to change it.")]
        public double UserGuideFontSize
        {
            get => _userGuideFontSize;
            set => SetProperty(ref _userGuideFontSize, Math.Clamp(value, 8.0, 32.0));
        }

        [property: Browsable(false)]
        [ObservableProperty]
        private ObservableCollection<WindowViewState> _windowViewStates = new();

        [property: Category("Connections")]
        [property: DisplayName("Reconnect Delay (seconds)")]
        [property: Description("Delay before an automatic reconnection attempt.")]
        [ObservableProperty]
        private int _reconnectDelaySeconds = 5;

        [property: Category("Connections")]
        [property: DisplayName("Connect Timeout (seconds)")]
        [property: Description("How long a connection attempt may take before it is abandoned.")]
        [ObservableProperty]
        private int _connectTimeoutSeconds = 15;

        [property: Category("Connections")]
        [property: DisplayName("Keepalive Interval (seconds)")]
        [property: Description("Sends a Telnet NOP after this much idle time so routers do not drop quiet sessions. Zero disables keepalives.")]
        [ObservableProperty]
        private int _keepAliveSeconds = 60;

        [property: Category("File Transfers")]
        [property: DisplayName("Download Folder")]
        [property: Description("Where downloaded files are saved.")]
        [property: Mosaic.UI.Wpf.Controls.PropertyGrid(EditorType = typeof(Mosaic.UI.Wpf.Controls.FolderPropertyEditor), IsReadOnly = true)]
        [ObservableProperty]
        private string? _downloadFolder;

        [property: Category("File Transfers")]
        [property: DisplayName("Default Protocol")]
        [property: Description("The transfer protocol offered first when uploading or downloading.")]
        [ObservableProperty]
        private Transfers.TransferProtocol _defaultTransferProtocol = Transfers.TransferProtocol.Zmodem;

        [property: Category("File Transfers")]
        [property: DisplayName("Auto-start ZMODEM Downloads")]
        [property: Description("Starts receiving automatically when the remote system begins a ZMODEM send.")]
        [ObservableProperty]
        private bool _autoStartZmodemDownloads = true;

        /// <summary>
        /// Returns the folder downloads are saved to, creating the default location
        /// (Downloads\BBS Navigator) when no folder has been chosen.
        /// </summary>
        public string ResolveDownloadFolder()
        {
            string folder = string.IsNullOrWhiteSpace(DownloadFolder)
                ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "BBS Navigator")
                : DownloadFolder;

            System.IO.Directory.CreateDirectory(folder);
            return folder;
        }

        [property: Browsable(false)]
        [ObservableProperty]
        private ObservableCollection<BbsProfile> _bbsProfiles = new();
    }
}
