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
        private MosaicThemeMode _theme = MosaicThemeMode.Dark;

        [property: Category("Terminal")]
        [property: DisplayName("Terminal Font Size")]
        [property: Description("The default terminal font size.")]
        [ObservableProperty]
        private double _fontSize = 15.0;

        [property: Browsable(false)]
        [ObservableProperty]
        private ObservableCollection<WindowViewState> _windowViewStates = new();

        [property: Category("Connections")]
        [property: DisplayName("Reconnect Delay (seconds)")]
        [property: Description("Delay before an automatic reconnection attempt.")]
        [ObservableProperty]
        private int _reconnectDelaySeconds = 5;

        [property: Browsable(false)]
        [ObservableProperty]
        private ObservableCollection<BbsProfile> _bbsProfiles = new();
    }
}
