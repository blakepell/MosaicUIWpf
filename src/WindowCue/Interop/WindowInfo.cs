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

namespace WindowCue.Interop
{
    /// <summary>
    /// Snapshot of a visible, running desktop window used during the add-window picker flow.
    /// </summary>
    public partial class WindowInfo : ObservableObject
    {
        /// <summary>
        /// Gets or sets the window handle (HWND).
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// Gets or sets the window title text.
        /// </summary>
        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the owning process ID.
        /// </summary>
        [ObservableProperty]
        public partial int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the process name (no extension).
        /// </summary>
        [ObservableProperty]
        public partial string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full path to the executable, if accessible.
        /// </summary>
        [ObservableProperty]
        public partial string? ExecutablePath { get; set; }

        /// <summary>
        /// The command line used to launch the process, if accessible.
        /// </summary>
        [ObservableProperty]
        public partial string? CommandLine { get; set; }

        /// <summary>
        /// Gets or sets the icon extracted for this window.
        /// </summary>
        [ObservableProperty]
        public partial ImageSource? Icon { get; set; }
    }
}
