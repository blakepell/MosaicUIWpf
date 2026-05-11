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
using System.Windows.Interop;
using WindowCue.Interop;

namespace WindowCue.Services
{
    /// <summary>
    /// Registers the WindowCue toolbar as a Windows Shell AppBar so that the OS
    /// reserves screen work area on the docked edge.  Other windows will then
    /// maximize and snap around the toolbar strip instead of overlapping it.
    /// </summary>
    public sealed class AppBarService : IDisposable
    {
        // ── State ─────────────────────────────────────────────────────────────

        private WeakReference<Window>? _windowRef;
        private DockEdge _currentEdge = DockEdge.Left;
        private string? _currentDeviceName;

        private uint _callbackMessage;
        private bool _registered;
        private HwndSource? _hwndSource;

        private const int WM_DISPLAYCHANGE = 0x007E;

        // ── Construction ──────────────────────────────────────────────────────

        public AppBarService()
        {
            // Per-process message name avoids collisions when multiple instances run.
            _callbackMessage = NativeMethods.RegisterWindowMessage(
                $"WindowCue_AppBarCallback_{Environment.ProcessId}");
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Registers <paramref name="window"/> as a shell AppBar at the specified
        /// <paramref name="edge"/> of the target monitor (nearest monitor used as
        /// fallback when <paramref name="deviceName"/> is <see langword="null"/>).
        /// Also positions the window at the shell-confirmed reserved rectangle.
        /// </summary>
        public void Register(Window window, DockEdge edge, string? deviceName)
        {
            _windowRef = new WeakReference<Window>(window);
            _currentEdge = edge;
            _currentDeviceName = deviceName;

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            if (!_registered)
            {
                var data = MakeData(hwnd);
                NativeMethods.SHAppBarMessage(NativeMethods.ABM_NEW, ref data);
                _registered = true;

                _hwndSource = HwndSource.FromHwnd(hwnd);
                _hwndSource?.AddHook(WndProcHook);
            }

            ApplyPosition(window, hwnd, edge, deviceName);
        }

        /// <summary>
        /// Updates the reserved edge and monitor without deregistering and
        /// re-registering the AppBar.
        /// </summary>
        public void UpdateEdge(Window window, DockEdge edge, string? deviceName)
        {
            _currentEdge = edge;
            _currentDeviceName = deviceName;

            if (!_registered)
            {
                Register(window, edge, deviceName);
                return;
            }

            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            ApplyPosition(window, hwnd, edge, deviceName);
        }

        /// <summary>
        /// Removes the shell AppBar reservation, restoring the monitor's full work area.
        /// </summary>
        public void Unregister()
        {
            if (!_registered)
                return;

            var hwnd = _hwndSource?.Handle ?? IntPtr.Zero;
            if (hwnd != IntPtr.Zero)
            {
                var data = MakeData(hwnd);
                NativeMethods.SHAppBarMessage(NativeMethods.ABM_REMOVE, ref data);
            }

            _hwndSource?.RemoveHook(WndProcHook);
            _hwndSource = null;
            _registered = false;
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose() => Unregister();

        // ── Core positioning ──────────────────────────────────────────────────

        private void ApplyPosition(Window window, IntPtr hwnd, DockEdge edge, string? deviceName)
        {
            var monitorRect = GetMonitorRect(hwnd, deviceName);
            var dpi = GetDpiScale(window);
            var thickness = GetThicknessPx(window, edge, dpi);
            var abeEdge = ToAbeEdge(edge);

            var data = MakeData(hwnd);
            data.uEdge = abeEdge;
            data.rc = BuildProposedRect(monitorRect, edge, thickness);

            // Ask the shell to clip our proposed rect (e.g., around the taskbar).
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref data);

            // Re-enforce our desired thickness on whichever axis the shell clipped.
            EnforceThickness(ref data.rc, edge, thickness);

            // Confirm; the shell now updates the monitor work area for all windows.
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref data);

            // Position the WPF window at the confirmed physical rectangle.
            PositionWindow(window, data.rc, edge, dpi);

            // Inform the shell that our window has settled at its new position.
            var posData = MakeData(hwnd);
            NativeMethods.SHAppBarMessage(NativeMethods.ABM_WINDOWPOSCHANGED, ref posData);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private NativeMethods.APPBARDATA MakeData(IntPtr hwnd) => new()
        {
            cbSize = (uint)Marshal.SizeOf<NativeMethods.APPBARDATA>(),
            hWnd = hwnd,
            uCallbackMessage = _callbackMessage
        };

        private static NativeMethods.RECT GetMonitorRect(IntPtr hwnd, string? deviceName)
        {
            NativeMethods.RECT result = default;
            bool found = false;

            if (!string.IsNullOrEmpty(deviceName))
            {
                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    (hMon, _, ref _, _) =>
                    {
                        var mi = new NativeMethods.MONITORINFOEX
                        { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>() };
                        if (NativeMethods.GetMonitorInfo(hMon, ref mi) &&
                            string.Equals(mi.szDevice, deviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            result = mi.rcMonitor;
                            found = true;
                            return false; // stop enumeration
                        }
                        return true;
                    }, IntPtr.Zero);
            }

            if (!found)
            {
                var hMon = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
                var info = new NativeMethods.MONITORINFOEX
                { cbSize = (uint)Marshal.SizeOf<NativeMethods.MONITORINFOEX>() };
                NativeMethods.GetMonitorInfo(hMon, ref info);
                result = info.rcMonitor;
            }

            return result;
        }

        private static (double X, double Y) GetDpiScale(Window window)
        {
            var source = PresentationSource.FromVisual(window);
            if (source?.CompositionTarget == null)
                return (1.0, 1.0);
            var m = source.CompositionTarget.TransformToDevice;
            return (m.M11, m.M22);
        }

        private static int GetThicknessPx(Window window, DockEdge edge, (double X, double Y) dpi) =>
            edge is DockEdge.Left or DockEdge.Right
                ? (int)Math.Round(window.Width * dpi.X)
                : (int)Math.Round(window.Height * dpi.Y);

        private static NativeMethods.RECT BuildProposedRect(
            NativeMethods.RECT mon, DockEdge edge, int thickness) => edge switch
            {
                DockEdge.Left => new() { Left = mon.Left, Top = mon.Top, Right = mon.Left + thickness, Bottom = mon.Bottom },
                DockEdge.Right => new() { Left = mon.Right - thickness, Top = mon.Top, Right = mon.Right, Bottom = mon.Bottom },
                DockEdge.Top => new() { Left = mon.Left, Top = mon.Top, Right = mon.Right, Bottom = mon.Top + thickness },
                DockEdge.Bottom => new() { Left = mon.Left, Top = mon.Bottom - thickness, Right = mon.Right, Bottom = mon.Bottom },
                _ => default
            };

        private static void EnforceThickness(ref NativeMethods.RECT rc, DockEdge edge, int thickness)
        {
            switch (edge)
            {
                case DockEdge.Left: rc.Right = rc.Left + thickness; break;
                case DockEdge.Right: rc.Left = rc.Right - thickness; break;
                case DockEdge.Top: rc.Bottom = rc.Top + thickness; break;
                case DockEdge.Bottom: rc.Top = rc.Bottom - thickness; break;
            }
        }

        private static void PositionWindow(
            Window window, NativeMethods.RECT rc, DockEdge edge, (double X, double Y) dpi)
        {
            window.Left = rc.Left / dpi.X;
            window.Top = rc.Top / dpi.Y;

            switch (edge)
            {
                case DockEdge.Left:
                case DockEdge.Right:
                    window.Height = (rc.Bottom - rc.Top) / dpi.Y;
                    break;
                case DockEdge.Top:
                case DockEdge.Bottom:
                    window.Width = (rc.Right - rc.Left) / dpi.X;
                    break;
            }
        }

        private static uint ToAbeEdge(DockEdge edge) => edge switch
        {
            DockEdge.Left => NativeMethods.ABE_LEFT,
            DockEdge.Right => NativeMethods.ABE_RIGHT,
            DockEdge.Top => NativeMethods.ABE_TOP,
            DockEdge.Bottom => NativeMethods.ABE_BOTTOM,
            _ => NativeMethods.ABE_LEFT
        };

        // ── WndProc hook ──────────────────────────────────────────────────────

        private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Shell AppBar callback: re-assert position when work area layout changes.
            if ((uint)msg == _callbackMessage)
            {
                switch (wParam.ToInt32())
                {
                    case NativeMethods.ABN_POSCHANGED:
                        ReapplyPosition(hwnd);
                        break;
                }
                return IntPtr.Zero;
            }

            // Display topology changed (resolution, orientation, monitor connected/disconnected).
            if (msg == WM_DISPLAYCHANGE)
            {
                ReapplyPosition(hwnd);
            }

            return IntPtr.Zero;
        }

        private void ReapplyPosition(IntPtr hwnd)
        {
            if (_windowRef?.TryGetTarget(out var w) == true)
                ApplyPosition(w, hwnd, _currentEdge, _currentDeviceName);
        }
    }
}
