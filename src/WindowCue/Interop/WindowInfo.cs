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

namespace WindowCue.Interop
{
    /// <summary>
    /// Snapshot of a visible, running desktop window used during the add-window picker flow.
    /// </summary>
    public class WindowInfo
    {
        /// <summary>Gets or sets the window handle (HWND).</summary>
        public IntPtr Handle { get; set; }

        /// <summary>Gets or sets the window title text.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the owning process ID.</summary>
        public int ProcessId { get; set; }

        /// <summary>Gets or sets the process name (no extension).</summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>Gets or sets the full path to the executable, if accessible.</summary>
        public string? ExecutablePath { get; set; }

        /// <summary>Gets or sets the icon extracted for this window.</summary>
        public ImageSource? Icon { get; set; }
    }
}
