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
        [Category("File System")]
        [DisplayName("Application Data Folder")]
        [Description("The application data folder.")]
        [ReadOnly(true)]
        [Browsable(true)]
        [ObservableProperty]
        public partial string? ApplicationDataFolder { get; set; }

        [Category("File System")]
        [DisplayName("Client Settings")]
        [Description("Settings specific to this workstation.")]
        [ReadOnly(false)]
        [Browsable(true)]
        [JsonIgnore]
        [ObservableProperty]
        public partial LocalSettings? ClientSettings { get; set; } = new();

        [Category("UI")]
        [DisplayName("Font Size")]
        [Description("The default font size for UI elements.")]
        [Browsable(true)]
        [ObservableProperty]
        public partial double FontSize { get; set; } = 12.0;

        [DisplayName("Theme")]
        [Category("UI")]
        [Browsable(false)]
        [ObservableProperty]
        public partial MosaicThemeMode Theme { get; set; } = MosaicThemeMode.Light;

        [Category("UI")]
        [DisplayName("Window View States")]
        [Description("The view state information for a given window.")]
        [Browsable(true)]
        [ObservableProperty]
        public partial ObservableCollection<WindowViewState> WindowViewStates { get; set; } = new();

        // ── WindowCue-specific settings ───────────────────────────────────────

        [Category("Toolbar")]
        [DisplayName("Dock Edge")]
        [Description("The screen edge the toolbar is docked to (Left, Right, Top, Bottom).")]
        [Browsable(true)]
        [ObservableProperty]
        public partial string DockEdge { get; set; } = "Left";

        [Category("Toolbar")]
        [DisplayName("Monitor Device Name")]
        [Description("The device name of the monitor the toolbar is docked to.")]
        [Browsable(true)]
        [ObservableProperty]
        public partial string? MonitorDeviceName { get; set; }

        [Category("Toolbar")]
        [DisplayName("Pinned Items")]
        [Description("The windows pinned to the toolbar.")]
        [Browsable(false)]
        [ObservableProperty]
        public partial List<PinnedItemData> PinnedItems { get; set; } = new();
    }
}
