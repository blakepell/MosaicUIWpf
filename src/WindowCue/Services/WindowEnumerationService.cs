/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Diagnostics;
using System.Text;
using WindowCue.Interop;

namespace WindowCue.Services
{
    /// <summary>
    /// Enumerates visible, taskbar-style application windows on the desktop, excluding
    /// WindowCue itself and system/shell windows.
    /// </summary>
    public class WindowEnumerationService
    {
        /// <summary>
        /// Returns a sorted list of visible, non-tool windows suitable for presentation use.
        /// WindowCue's own process windows are automatically filtered out.
        /// </summary>
        public List<WindowInfo> GetVisibleWindows()
        {
            var result     = new List<WindowInfo>();
            var shellHwnd  = NativeMethods.GetShellWindow();
            var desktopHwnd = NativeMethods.GetDesktopWindow();
            int ownPid     = Environment.ProcessId;

            NativeMethods.EnumWindows((hWnd, _) =>
            {
                try
                {
                    // Skip shell and desktop
                    if (hWnd == shellHwnd || hWnd == desktopHwnd)
                    {
                        return true;
                    }

                    // Only visible windows
                    if (!NativeMethods.IsWindowVisible(hWnd))
                    {
                        return true;
                    }

                    // Skip child windows (not top-level)
                    if (NativeMethods.GetParent(hWnd) != IntPtr.Zero)
                    {
                        return true;
                    }

                    // Skip tool windows (not in Alt+Tab / taskbar)
                    long exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
                    if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0)
                    {
                        return true;
                    }

                    // Skip windows with no title
                    int len = NativeMethods.GetWindowTextLength(hWnd);
                    if (len == 0)
                    {
                        return true;
                    }

                    var sb = new StringBuilder(len + 1);
                    NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        return true;
                    }

                    // Get process info
                    NativeMethods.GetWindowThreadProcessId(hWnd, out int pid);
                    if (pid == 0)
                    {
                        return true;
                    }

                    // Skip our own process
                    if (pid == ownPid)
                    {
                        return true;
                    }

                    string processName     = string.Empty;
                    string? executablePath = null;

                    try
                    {
                        using var proc = Process.GetProcessById(pid);
                        processName = proc.ProcessName;
                        try { executablePath = proc.MainModule?.FileName; } catch { /* access denied */ }
                    }
                    catch { return true; /* process already exited */ }

                    result.Add(new WindowInfo
                    {
                        Handle        = hWnd,
                        Title         = title,
                        ProcessId     = pid,
                        ProcessName   = processName,
                        ExecutablePath = executablePath
                    });
                }
                catch { /* ignore misbehaving windows */ }

                return true;
            }, IntPtr.Zero);

            return result
                .OrderBy(w => w.ProcessName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(w => w.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Attempts to find a currently-running window that matches the given saved item data.
        /// Tries in priority order: executable path → process name → window title substring.
        /// </summary>
        public WindowInfo? TryRebind(string? executablePath, string processName, string windowTitle)
        {
            var windows = GetVisibleWindows();

            // Priority 1: exact executable path
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                var match = windows.FirstOrDefault(w =>
                    string.Equals(w.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            // Priority 2: process name
            if (!string.IsNullOrWhiteSpace(processName))
            {
                var match = windows.FirstOrDefault(w =>
                    string.Equals(w.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            // Priority 3: window title substring (bidirectional)
            if (!string.IsNullOrWhiteSpace(windowTitle))
            {
                var match = windows.FirstOrDefault(w =>
                    w.Title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase) ||
                    windowTitle.Contains(w.Title, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
