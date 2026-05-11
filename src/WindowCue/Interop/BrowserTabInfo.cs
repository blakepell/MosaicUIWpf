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
    /// Snapshot of an open browser tab discovered via UI Automation.
    /// </summary>
    public class BrowserTabInfo
    {
        /// <summary>Gets or sets the tab page title.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL of the tab. Only populated for the active tab in each
        /// browser window; background tabs return <see langword="null"/> because the
        /// address bar only exposes the currently-visible URL.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the HWND of the top-level browser window that contains this tab.
        /// Used to bring the window to the foreground before activating the tab.
        /// </summary>
        public IntPtr WindowHandle { get; set; }

        /// <summary>Gets or sets the owning process ID of the browser window.</summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the process name used to find the browser (e.g., <c>msedge</c>,
        /// <c>chrome</c>). Defaults to <c>msedge</c>.
        /// </summary>
        public string BrowserProcessName { get; set; } = "msedge";

        /// <summary>Gets or sets the full path to the browser executable, if accessible.</summary>
        public string? ExecutablePath { get; set; }

        /// <summary>Gets or sets the icon representing this tab (browser app icon).</summary>
        public ImageSource? Icon { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this tab is the currently active tab in
        /// its browser window.
        /// </summary>
        public bool IsActiveTab { get; set; }
    }
}
