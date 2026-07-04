/*
 * ChromaSwap
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ChromaSwap.Common
{
    /// <summary>
    /// AppSettings that are persisted to storage and reloaded on application launch.
    /// </summary>
    public partial class AppSettings : ObservableObject, IAppSettings
    {
        /// <summary>
        /// The application data folder which will be populated by this program when it loads.
        /// </summary>
        [property: Category("File System")]
        [property: DisplayName("Application Data Folder")]
        [property: Description("The application data folder.")]
        [property: ReadOnly(true)]
        [property: Browsable(true)]
        [ObservableProperty]
        private string? _applicationDataFolder;

        [property: Category("File System")]
        [property: DisplayName("Client Settings")]
        [property: Description("Settings specific to this workstation.")]
        [property: ReadOnly(false)]
        [property: Browsable(true)]
        [JsonIgnore]
        [ObservableProperty]
        private LocalSettings? _clientSettings = new();

        /// <summary>
        /// The applications current theme.  ChromaSwap is designed dark first.
        /// </summary>
        [property: DisplayName("Theme")]
        [property: Category("UI")]
        [property: Browsable(false)]
        [ObservableProperty]
        private MosaicThemeMode _theme = MosaicThemeMode.Dark;

        /// <summary>
        /// The view state information for a given window.
        /// </summary>
        [property: Category("UI")]
        [property: DisplayName("Window View States")]
        [property: Description("The view state information for a given window.")]
        [property: Browsable(true)]
        [ObservableProperty]
        private ObservableCollection<WindowViewState> _windowViewStates = new();
    }
}
