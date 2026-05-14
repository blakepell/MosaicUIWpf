/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Controls;
using WindowCue.Interop;
using WindowCue.Models;

namespace WindowCue.ViewModels
{
    /// <summary>
    /// Represents one pinned item displayed in the WindowCue toolbar.
    /// The item is either a desktop window (<see cref="PinnedTargetType.Window"/>) or a
    /// specific browser tab (<see cref="PinnedTargetType.BrowserTab"/>).
    /// </summary>
    public partial class ToolbarItemViewModel : ObservableObject
    {
        /// <summary>User-editable display label shown beneath the icon.</summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Appearance", DisplayName = "Label")]
        private string _label = string.Empty;

        /// <summary>Icon extracted from the target window or its executable.</summary>
        [ObservableProperty]
        [property: PropertyGrid(Ignore = true)]
        private ImageSource? _icon;

        /// <summary>Process ID of the pinned window's owning process.</summary>
        [ObservableProperty]
        [PropertyGrid(Category = "Process", DisplayName = "Process ID", IsReadOnly = true)]
        public partial int ProcessId { get; set; }

        /// <summary>Last known window handle. May become stale if the window is closed and re-opened.</summary>
        [ObservableProperty]
        [property: PropertyGrid(Ignore = true)]
        private IntPtr _windowHandle;

        /// <summary>Last known title of the target window.</summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Window", DisplayName = "Window Title", IsReadOnly = true)]
        private string _windowTitle = string.Empty;

        /// <summary>Process name (without extension) of the owning process.</summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Process", DisplayName = "Process Name", IsReadOnly = true)]
        private string _processName = string.Empty;

        /// <summary>Full path to the executable, if accessible.</summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Process", DisplayName = "Executable Path", IsReadOnly = true)]
        private string? _executablePath;

        /// <summary>
        /// <see langword="true"/> while the target window is reachable; <see langword="false"/>
        /// when the process has exited or the handle is stale.
        /// </summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Status", DisplayName = "Available", IsReadOnly = true)]
        private bool _isAvailable = true;

        /// <summary>Human-readable explanation shown in the tooltip when the item is unavailable.</summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Status", DisplayName = "Unavailable Reason", IsReadOnly = true)]
        private string? _unavailableReason;

        // ── Browser-tab fields ────────────────────────────────────────────────

        /// <summary>
        /// Whether this item targets a desktop window or a specific browser tab.
        /// Defaults to <see cref="PinnedTargetType.Window"/>.
        /// </summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "General", DisplayName = "Target Type", IsReadOnly = true)]
        private PinnedTargetType _targetType = PinnedTargetType.Window;

        /// <summary>
        /// The tab title at the time of pinning; used as the primary rebinding key
        /// for browser-tab items. Empty for regular window items.
        /// </summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Browser Tab", DisplayName = "Tab Title", IsReadOnly = true)]
        private string _tabTitle = string.Empty;

        /// <summary>
        /// The URL of the tab at the time of pinning; used as a disambiguation hint
        /// alongside <see cref="TabTitle"/>. Null for background tabs and non-tab items.
        /// </summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Browser Tab", DisplayName = "URL", IsReadOnly = true)]
        private string? _tabUrl;

        /// <summary>
        /// The browser process name used to locate the tab (e.g., <c>msedge</c>,
        /// <c>chrome</c>). Empty for non-tab items.
        /// </summary>
        [ObservableProperty]
        [property: PropertyGrid(Category = "Browser Tab", DisplayName = "Browser", IsReadOnly = true)]
        private string _browserProcessName = string.Empty;

        // ── Factory methods ───────────────────────────────────────────────────

        /// <summary>Creates a ViewModel from a live <see cref="WindowInfo"/> snapshot.</summary>
        public static ToolbarItemViewModel FromWindowInfo(WindowInfo info, string? overrideLabel = null)
        {
            string label = overrideLabel
                ?? (info.ProcessName.Length > 0 ? info.ProcessName : info.Title);

            // Truncate long labels for compact display
            if (label.Length > 10)
            {
                label = label[..10];
            }

            return new ToolbarItemViewModel
            {
                Label = label,
                Icon = info.Icon,
                ProcessId = info.ProcessId,
                WindowHandle = info.Handle,
                WindowTitle = info.Title,
                ProcessName = info.ProcessName,
                ExecutablePath = info.ExecutablePath,
                TargetType = PinnedTargetType.Window,
                IsAvailable = true
            };
        }

        /// <summary>Creates a ViewModel from a live <see cref="BrowserTabInfo"/> snapshot.</summary>
        public static ToolbarItemViewModel FromBrowserTab(BrowserTabInfo tab, string? overrideLabel = null)
        {
            string label = overrideLabel ?? tab.Title;

            // Truncate long labels for compact display
            if (label.Length > 10)
            {
                label = label[..10];
            }

            return new ToolbarItemViewModel
            {
                Label = label,
                Icon = tab.Icon,
                ProcessId = tab.ProcessId,
                WindowHandle = tab.WindowHandle,
                WindowTitle = tab.Title,
                ProcessName = tab.BrowserProcessName,
                ExecutablePath = tab.ExecutablePath,
                TargetType = PinnedTargetType.BrowserTab,
                TabTitle = tab.Title,
                TabUrl = tab.Url,
                BrowserProcessName = tab.BrowserProcessName,
                IsAvailable = true
            };
        }
    }
}
