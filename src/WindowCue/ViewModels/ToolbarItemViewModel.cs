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
using WindowCue.Interop;

namespace WindowCue.ViewModels
{
    /// <summary>
    /// Represents one pinned window item displayed in the WindowCue toolbar.
    /// </summary>
    public partial class ToolbarItemViewModel : ObservableObject
    {
        /// <summary>User-editable display label shown beneath the icon.</summary>
        [ObservableProperty]
        private string _label = string.Empty;

        /// <summary>Icon extracted from the target window or its executable.</summary>
        [ObservableProperty]
        private ImageSource? _icon;

        /// <summary>Process ID of the pinned window's owning process.</summary>
        [ObservableProperty]
        private int _processId;

        /// <summary>Last known window handle. May become stale if the window is closed and re-opened.</summary>
        [ObservableProperty]
        private IntPtr _windowHandle;

        /// <summary>Last known title of the target window.</summary>
        [ObservableProperty]
        private string _windowTitle = string.Empty;

        /// <summary>Process name (without extension) of the owning process.</summary>
        [ObservableProperty]
        private string _processName = string.Empty;

        /// <summary>Full path to the executable, if accessible.</summary>
        [ObservableProperty]
        private string? _executablePath;

        /// <summary>
        /// <see langword="true"/> while the target window is reachable; <see langword="false"/>
        /// when the process has exited or the handle is stale.
        /// </summary>
        [ObservableProperty]
        private bool _isAvailable = true;

        /// <summary>Human-readable explanation shown in the tooltip when the item is unavailable.</summary>
        [ObservableProperty]
        private string? _unavailableReason;

        /// <summary>Creates a ViewModel from a live <see cref="WindowInfo"/> snapshot.</summary>
        public static ToolbarItemViewModel FromWindowInfo(WindowInfo info, string? overrideLabel = null)
        {
            string label = overrideLabel
                ?? (info.ProcessName.Length > 0 ? info.ProcessName : info.Title);

            // Truncate long labels for compact display
            if (label.Length > 10)
                label = label[..10];

            return new ToolbarItemViewModel
            {
                Label          = label,
                Icon           = info.Icon,
                ProcessId      = info.ProcessId,
                WindowHandle   = info.Handle,
                WindowTitle    = info.Title,
                ProcessName    = info.ProcessName,
                ExecutablePath = info.ExecutablePath,
                IsAvailable    = true
            };
        }
    }
}
