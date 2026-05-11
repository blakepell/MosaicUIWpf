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
using Mosaic.UI.Wpf;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using WindowCue.Models;

namespace WindowCue.Common
{
    public partial class AppSettings : ObservableObject, IAppSettings
    {
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

        [property: Category("UI")]
        [property: DisplayName("Font Size")]
        [property: Description("The default font size for UI elements.")]
        [property: Browsable(true)]
        [ObservableProperty]
        private double _fontSize = 12.0;

        [property: DisplayName("Theme")]
        [property: Category("UI")]
        [property: Browsable(false)]
        [ObservableProperty]
        private MosaicThemeMode _theme = MosaicThemeMode.Light;

        [property: Category("UI")]
        [property: DisplayName("Window View States")]
        [property: Description("The view state information for a given window.")]
        [property: Browsable(true)]
        [ObservableProperty]
        private ObservableCollection<WindowViewState> _windowViewStates = new();

        // ── WindowCue-specific settings ───────────────────────────────────────

        [property: Category("Toolbar")]
        [property: DisplayName("Dock Edge")]
        [property: Description("The screen edge the toolbar is docked to (Left, Right, Top, Bottom).")]
        [property: Browsable(true)]
        [ObservableProperty]
        private string _dockEdge = "Left";

        [property: Category("Toolbar")]
        [property: DisplayName("Monitor Device Name")]
        [property: Description("The device name of the monitor the toolbar is docked to.")]
        [property: Browsable(true)]
        [ObservableProperty]
        private string? _monitorDeviceName;

        [property: Category("Toolbar")]
        [property: DisplayName("Pinned Items")]
        [property: Description("The windows pinned to the toolbar.")]
        [property: Browsable(false)]
        [ObservableProperty]
        private List<PinnedItemData> _pinnedItems = new();
    }
}
