/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Runtime.InteropServices;
using System.Windows;
using WindowCue.Interop;

namespace WindowCue.Services
{
    /// <summary>Represents one of the four edges a toolbar can be docked to.</summary>
    public enum DockEdge { Left, Right, Top, Bottom }

    /// <summary>Describes a physical monitor and its usable work area.</summary>
    public class MonitorInfo
    {
        public string DeviceName { get; set; } = string.Empty;
        public Rect   WorkArea   { get; set; }
        public bool   IsPrimary  { get; set; }
    }

    /// <summary>
    /// Positions and sizes a <see cref="Window"/> at a chosen edge of a chosen monitor,
    /// implementing the always-on-top docking behavior.
    /// </summary>
    public class ScreenDockingService
    {
        /// <summary>Returns information about all connected monitors.</summary>
        public List<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo>();

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (hMonitor, _, ref _, _) =>
                {
                    var mi = new NativeMethods.MONITORINFOEX();
                    mi.cbSize = (uint)Marshal.SizeOf(mi);
                    if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
                    {
                        monitors.Add(new MonitorInfo
                        {
                            DeviceName = mi.szDevice,
                            WorkArea   = new Rect(
                                mi.rcWork.Left, mi.rcWork.Top,
                                mi.rcWork.Right - mi.rcWork.Left,
                                mi.rcWork.Bottom - mi.rcWork.Top),
                            IsPrimary = (mi.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0
                        });
                    }
                    return true;
                }, IntPtr.Zero);

            return monitors;
        }

        /// <summary>
        /// Positions and sizes <paramref name="window"/> at the specified <paramref name="edge"/>
        /// of the specified monitor (matched by device name; falls back to primary monitor).
        /// </summary>
        public void Dock(Window window, DockEdge edge, string? deviceName = null)
        {
            var monitors = GetMonitors();

            var monitor = monitors.FirstOrDefault(m =>
                              string.Equals(m.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                          ?? monitors.FirstOrDefault(m => m.IsPrimary)
                          ?? monitors.FirstOrDefault();

            if (monitor == null)
            {
                return;
            }

            var work = monitor.WorkArea;

            switch (edge)
            {
                case DockEdge.Left:
                    window.Left   = work.Left;
                    window.Top    = work.Top;
                    window.Height = work.Height;
                    break;

                case DockEdge.Right:
                    window.Left   = work.Right - window.Width;
                    window.Top    = work.Top;
                    window.Height = work.Height;
                    break;

                case DockEdge.Top:
                    window.Left  = work.Left;
                    window.Top   = work.Top;
                    window.Width = work.Width;
                    break;

                case DockEdge.Bottom:
                    window.Left  = work.Left;
                    window.Top   = work.Bottom - window.Height;
                    window.Width = work.Width;
                    break;
            }
        }
    }
}
