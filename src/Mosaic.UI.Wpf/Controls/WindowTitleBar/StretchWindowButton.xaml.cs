/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Interop;
using Mosaic.UI.Wpf.Common;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A title bar button that stretches the window it lives in to a fixed fraction of the work area
    /// on the monitor that window currently occupies, then centers it there.
    /// </summary>
    public partial class StretchWindowButton : UserControl
    {
        /// <summary>
        /// Fraction of the screen work area width the window fills when it is stretched.
        /// </summary>
        private const double StretchFillWidth = 0.90;

        /// <summary>
        /// Fraction of the screen work area height the window fills when it is stretched.
        /// </summary>
        private const double StretchFillHeight = 0.90;

        /// <summary>
        /// Constructor.
        /// </summary>
        public StretchWindowButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Stretches the owning window to <see cref="StretchFillWidth"/> of the width and
        /// <see cref="StretchFillHeight"/> of the height of the work area on the monitor it currently
        /// occupies, then centers it horizontally and vertically within that work area. The work area
        /// excludes the taskbar and any other docked app bars.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ButtonStretchWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);

            if (window == null)
            {
                return;
            }

            // Maximized or minimized windows ignore Width/Height/Left/Top, so return to the
            // normal state before resizing.
            if (window.WindowState != WindowState.Normal)
            {
                window.WindowState = WindowState.Normal;
            }

            var workArea = GetWorkArea(window);

            if (workArea.Width <= 0 || workArea.Height <= 0)
            {
                return;
            }

            var width = Math.Floor(workArea.Width * StretchFillWidth);
            var height = Math.Floor(workArea.Height * StretchFillHeight);

            // Respect any minimum size the window declares, but never grow past the usable area.
            if (!double.IsNaN(window.MinWidth))
            {
                width = Math.Max(width, window.MinWidth);
            }

            if (!double.IsNaN(window.MinHeight))
            {
                height = Math.Max(height, window.MinHeight);
            }

            width = Math.Min(width, workArea.Width);
            height = Math.Min(height, workArea.Height);

            window.Width = width;
            window.Height = height;

            window.Left = workArea.Left + (workArea.Width - width) / 2;
            window.Top = workArea.Top + (workArea.Height - height) / 2;
        }

        /// <summary>
        /// Returns the work area, in device-independent units, of the screen the specified window
        /// currently occupies. Falls back to the primary screen's work area before the window has a
        /// handle or when the monitor cannot be queried.
        /// </summary>
        /// <param name="window">The window whose screen work area should be returned.</param>
        private static Rect GetWorkArea(Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;

            if (handle == IntPtr.Zero)
            {
                return SystemParameters.WorkArea;
            }

            var monitor = Win32.MonitorFromWindow(handle, Win32.MONITOR_DEFAULTTONEAREST);

            if (monitor == IntPtr.Zero)
            {
                return SystemParameters.WorkArea;
            }

            var info = new Win32.MONITORINFO { cbSize = Marshal.SizeOf<Win32.MONITORINFO>() };

            if (!Win32.GetMonitorInfo(monitor, ref info))
            {
                return SystemParameters.WorkArea;
            }

            var dpi = VisualTreeHelper.GetDpi(window);
            var work = info.rcWork;

            return new Rect(
                work.Left / dpi.DpiScaleX,
                work.Top / dpi.DpiScaleY,
                (work.Right - work.Left) / dpi.DpiScaleX,
                (work.Bottom - work.Top) / dpi.DpiScaleY);
        }
    }
}
