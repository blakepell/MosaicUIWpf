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
using WindowCue.Interop;

namespace WindowCue.Services
{
    /// <summary>
    /// Provides Win32-backed operations to focus, restore, and validate desktop windows.
    /// </summary>
    public class WindowFocusService
    {
        /// <summary>
        /// Brings the target window to the foreground, restoring it first if minimized.
        /// </summary>
        /// <returns><see langword="true"/> if the focus operation was attempted on a valid window.</returns>
        public bool FocusWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero || !NativeMethods.IsWindow(handle))
                return false;

            if (NativeMethods.IsIconic(handle))
                NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);

            NativeMethods.BringWindowToTop(handle);
            NativeMethods.SetForegroundWindow(handle);
            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the window handle still refers to a valid, visible window.
        /// </summary>
        public bool IsWindowAlive(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return false;
            return NativeMethods.IsWindow(handle) && NativeMethods.IsWindowVisible(handle);
        }

        /// <summary>
        /// Attempts to resolve a fresh window handle for the given process ID by querying
        /// the process's current main window handle.
        /// </summary>
        public IntPtr FindWindowForProcess(int processId)
        {
            if (processId <= 0) return IntPtr.Zero;
            try
            {
                using var proc = Process.GetProcessById(processId);
                return proc.MainWindowHandle;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
    }
}
