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

namespace WindowCue.Models
{
    /// <summary>
    /// Serialization-friendly representation of a pinned toolbar item, persisted in AppSettings.
    /// Window handles are not stored because they are not stable across reboots.
    /// </summary>
    public partial class PinnedItemData : ObservableObject
    {
        /// <summary>
        /// Indicates whether this item is a regular desktop window or a browser tab.
        /// Defaults to <see cref="PinnedTargetType.Window"/> for backward compatibility.
        /// </summary>
        [ObservableProperty]
        public partial PinnedTargetType TargetType { get; set; } = PinnedTargetType.Window;

        /// <summary>
        /// The process ID associated with the pinned window to find a specific instance.
        /// </summary>
        [ObservableProperty]
        public partial int ProcessId { get; set; }

        /// <summary>
        /// User-editable display label shown beneath the icon.
        /// </summary>
        [ObservableProperty]
        public partial string Label { get; set; } = string.Empty;

        /// <summary>
        /// The process name (no .exe extension) used for window rebinding on startup.
        /// </summary>
        [ObservableProperty]
        public partial string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Full path to the executable, used as the primary rebinding key.
        /// </summary>
        [ObservableProperty]
        public partial string? ExecutablePath { get; set; }

        /// <summary>
        /// Last known window title, used as a fallback rebinding hint.
        /// </summary>
        [ObservableProperty]
        public partial string WindowTitle { get; set; } = string.Empty;

        // ── Browser-tab–specific fields (ignored for TargetType = Window) ─────

        /// <summary>
        /// The tab page title saved when the tab was pinned. Used as the primary
        /// rebinding key for browser-tab items.
        /// </summary>
        [ObservableProperty]
        public partial string TabTitle { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the tab at the time it was pinned. Used as a disambiguation hint
        /// when multiple tabs share the same title. Null for background tabs.
        /// </summary>
        [ObservableProperty]
        public partial string? TabUrl { get; set; }

        /// <summary>
        /// The browser process name used to locate the tab (e.g., <c>msedge</c>,
        /// <c>chrome</c>). Defaults to <c>msedge</c>.
        /// </summary>
        [ObservableProperty]
        public partial string BrowserProcessName { get; set; } = "msedge";
    }
}
