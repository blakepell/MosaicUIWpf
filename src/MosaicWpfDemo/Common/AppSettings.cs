/*
 * Mosaic UI for WPF
 * 
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Interfaces;

namespace MosaicWpfDemo.Common
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
        /// The applications current theme.
        /// </summary>
        [property: DisplayName("Theme")]
        [property: Category("UI")]
        [property: Browsable(false)]
        [ObservableProperty]
        private string _theme = "Light";

        /// <summary>
        /// Default font size for UI elements.
        /// </summary>
        [property: Category("UI")]
        [property: DisplayName("Font Size")]
        [property: Description("The default font size for UI elements.")]
        [ObservableProperty]
        [property: Browsable(true)]
        private double _fontSize = 12.0;

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
