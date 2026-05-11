/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace WindowCue.Models
{
    /// <summary>
    /// Discriminates between a regular desktop window pin and a browser-tab pin.
    /// </summary>
    public enum PinnedTargetType
    {
        /// <summary>A standard desktop application window identified by HWND / PID.</summary>
        Window,

        /// <summary>A specific tab inside a Chromium-based browser (Edge, Chrome, etc.).</summary>
        BrowserTab
    }
}
