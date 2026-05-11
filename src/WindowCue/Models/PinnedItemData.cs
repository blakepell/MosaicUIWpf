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
    }
}
