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
using Microsoft.Win32.SafeHandles;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The kind of notification the native tray layer forwards to <see cref="TrayIcon"/>.
    /// </summary>
    internal enum TrayNotificationKind
    {
        MouseMove,
        LeftButtonDown,
        LeftButtonUp,
        LeftButtonDoubleClick,
        RightButtonUp,
        ContextMenu,
        KeySelect,
        PopupOpen,
        PopupClose,
        TaskbarCreated,
        DisplaySettingsChanged
    }

    /// <summary>
    /// A decoded notification from the shell. Passed by value so the hot path does not allocate.
    /// </summary>
    /// <param name="Kind">What happened.</param>
    /// <param name="X">Anchor X in physical screen pixels (when applicable).</param>
    /// <param name="Y">Anchor Y in physical screen pixels (when applicable).</param>
    internal readonly record struct TrayNotification(TrayNotificationKind Kind, int X, int Y);

    /// <summary>
    /// Owns a native icon handle (<c>HICON</c>) and destroys it on release when owned.
    /// </summary>
    internal sealed class TrayIconHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Wraps <paramref name="handle"/>. Shared system icons must pass <paramref name="ownsHandle"/> = <see langword="false"/>.
        /// </summary>
        public TrayIconHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
        {
            this.SetHandle(handle);
        }

        /// <inheritdoc />
        protected override bool ReleaseHandle()
        {
            return TrayIconNative.NativeMethods.DestroyIcon(this.handle);
        }
    }

    /// <summary>
    /// Manages one notification-area registration: a hidden <see cref="HwndSource"/> message window,
    /// the <c>Shell_NotifyIcon</c> calls against it, and recovery when Explorer restarts.
    /// </summary>
    /// <remarks>
    /// Every instance owns its own message window, so multiple tray icons never share a callback target.
    /// All members must be called on the thread that created the instance. Icon handles passed to
    /// <see cref="SetIcon"/> become owned by this class; a handle the shell may still be displaying is never
    /// destroyed until the shell has accepted its replacement or the icon has been removed.
    /// </remarks>
    internal sealed partial class TrayIconNative : IDisposable
    {
        internal const uint IconId = 1;

        internal static readonly uint TaskbarCreatedMessage = NativeMethods.RegisterWindowMessageW("TaskbarCreated");
        internal static readonly uint CallbackMessage = NativeMethods.RegisterWindowMessageW("Mosaic.UI.Wpf.TrayIcon.Callback");

        private readonly Action<TrayNotification> _notify;
        private HwndSource? _source;
        private IntPtr _hwnd;
        private int _hookDepth;
        private bool _disposed;

        private string? _toolTip;

        /// <summary>The icon the caller wants displayed.</summary>
        private TrayIconHandle? _icon;

        /// <summary>The icon the shell last accepted (may differ from <see cref="_icon"/> after a failed update).</summary>
        private TrayIconHandle? _shellIcon;

        /// <summary>
        /// Creates the hidden message window.
        /// </summary>
        /// <param name="notify">Receives decoded notifications on the owning thread.</param>
        /// <exception cref="System.ComponentModel.Win32Exception">The message window could not be created.</exception>
        public TrayIconNative(Action<TrayNotification> notify)
        {
            _notify = notify;

            // A hidden top-level tool window rather than a message-only (HWND_MESSAGE) window: message-only
            // windows do not receive the broadcast TaskbarCreated message that signals an Explorer restart.
            var parameters = new HwndSourceParameters("MosaicTrayIcon")
            {
                WindowStyle = 0,
                ExtendedWindowStyle = NativeMethods.WS_EX_TOOLWINDOW,
                Width = 0,
                Height = 0,
                PositionX = 0,
                PositionY = 0
            };

            _source = new HwndSource(parameters);
            _hwnd = _source.Handle;
            _source.AddHook(this.WndProc);

            // Elevated processes would otherwise have both messages filtered out by UIPI.
            NativeMethods.ChangeWindowMessageFilterEx(_hwnd, TaskbarCreatedMessage, NativeMethods.MSGFLT_ALLOW, IntPtr.Zero);
            NativeMethods.ChangeWindowMessageFilterEx(_hwnd, CallbackMessage, NativeMethods.MSGFLT_ALLOW, IntPtr.Zero);
        }

        /// <summary>
        /// Gets whether the shell currently holds a registration for this icon.
        /// </summary>
        public bool IsAdded { get; private set; }

        /// <summary>
        /// Gets whether the shell accepted <c>NOTIFYICON_VERSION_4</c> semantics.
        /// </summary>
        public bool IsVersion4 { get; private set; }

        /// <summary>
        /// Gets the message window handle.
        /// </summary>
        public IntPtr Handle => _hwnd;

        /// <summary>
        /// Gets the icon handle the shell last accepted, if any. Exposed for diagnostics and tests.
        /// </summary>
        internal IntPtr ShellIconHandle => _shellIcon?.DangerousGetHandle() ?? IntPtr.Zero;

        /// <summary>
        /// Replaces the desired icon. Ownership of <paramref name="icon"/> transfers to this instance.
        /// Call <see cref="Apply"/> to push it to the shell.
        /// </summary>
        public void SetIcon(TrayIconHandle? icon)
        {
            if (ReferenceEquals(icon, _icon))
            {
                return;
            }

            var previous = _icon;
            _icon = icon;

            // Only destroy the previous handle now if the shell is not displaying it.
            if (previous != null && !ReferenceEquals(previous, _shellIcon))
            {
                previous.Dispose();
            }
        }

        /// <summary>
        /// Sets the tooltip text (truncated to the shell's 127-character limit). Call <see cref="Apply"/> to push it.
        /// </summary>
        public void SetToolTip(string? text)
        {
            _toolTip = text;
        }

        /// <summary>
        /// Adds the icon if it is not registered, otherwise modifies it with the current icon and tooltip.
        /// </summary>
        /// <returns><see langword="true"/> if the shell accepted the registration.</returns>
        public unsafe bool Apply()
        {
            if (_disposed)
            {
                return false;
            }

            var data = this.CreateData();

            if (this.IsAdded)
            {
                if (NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_MODIFY, &data))
                {
                    this.OnShellAccepted();
                    return true;
                }

                // The registration was lost (Explorer went away). Fall through and try to add it again.
                this.IsAdded = false;
            }

            if (!NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_ADD, &data))
            {
                // NIM_ADD fails when an icon with this identity already exists (e.g. a DPI-triggered
                // TaskbarCreated where the shell kept its registrations); a modify then succeeds.
                if (!NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_MODIFY, &data))
                {
                    Trace.WriteLine($"TrayIcon: Shell_NotifyIcon(NIM_ADD) failed (Win32 error {Marshal.GetLastPInvokeError()}). Registration will be retried when the taskbar is created.");
                    return false;
                }
            }

            this.IsAdded = true;
            this.OnShellAccepted();

            var versionData = this.CreateIdentityData();
            versionData.uVersion = NativeMethods.NOTIFYICON_VERSION_4;
            this.IsVersion4 = NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_SETVERSION, &versionData);

            return true;
        }

        /// <summary>
        /// Removes the icon from the notification area.
        /// </summary>
        public unsafe void Remove()
        {
            if (this.IsAdded)
            {
                var data = this.CreateIdentityData();
                NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_DELETE, &data);
                this.IsAdded = false;
            }

            this.ReleaseShellIcon();
        }

        /// <summary>
        /// Gets the bounding rectangle of the icon in physical screen pixels.
        /// </summary>
        /// <returns><see langword="true"/> if the shell reported a rectangle.</returns>
        public unsafe bool TryGetIconRect(out NativeMethods.RECT rect)
        {
            rect = default;

            if (!this.IsAdded)
            {
                return false;
            }

            var id = new NativeMethods.NOTIFYICONIDENTIFIER
            {
                cbSize = (uint)sizeof(NativeMethods.NOTIFYICONIDENTIFIER),
                hWnd = _hwnd,
                uID = IconId
            };

            NativeMethods.RECT r;
            if (NativeMethods.Shell_NotifyIconGetRect(&id, &r) != 0)
            {
                return false;
            }

            rect = r;
            return r.Right > r.Left && r.Bottom > r.Top;
        }

        /// <summary>
        /// Removes the icon, destroys the message window and releases all icon handles. Safe to call repeatedly.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            this.Remove();
            _disposed = true;

            _icon?.Dispose();
            _icon = null;

            var source = _source;
            _source = null;

            if (source != null)
            {
                source.RemoveHook(this.WndProc);

                // Destroying the window from inside its own window procedure is legal but leaves the
                // HwndSource mid-dispatch; defer it until the current message has unwound.
                if (_hookDepth > 0 && !source.Dispatcher.HasShutdownStarted)
                {
                    source.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(source.Dispose));
                }
                else
                {
                    source.Dispose();
                }
            }

            _hwnd = IntPtr.Zero;
        }

        private void OnShellAccepted()
        {
            if (!ReferenceEquals(_shellIcon, _icon))
            {
                this.ReleaseShellIcon();
                _shellIcon = _icon;
            }
        }

        private void ReleaseShellIcon()
        {
            var shell = _shellIcon;
            _shellIcon = null;

            if (shell != null && !ReferenceEquals(shell, _icon))
            {
                shell.Dispose();
            }
        }

        private unsafe NativeMethods.NOTIFYICONDATAW CreateIdentityData()
        {
            return new NativeMethods.NOTIFYICONDATAW
            {
                cbSize = (uint)sizeof(NativeMethods.NOTIFYICONDATAW),
                hWnd = _hwnd,
                uID = IconId
            };
        }

        private unsafe NativeMethods.NOTIFYICONDATAW CreateData()
        {
            var data = this.CreateIdentityData();
            data.uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP | NativeMethods.NIF_SHOWTIP;
            data.uCallbackMessage = CallbackMessage;
            data.hIcon = _icon?.DangerousGetHandle() ?? IntPtr.Zero;

            var tip = _toolTip.AsSpan();
            if (tip.Length > NativeMethods.TipLength - 1)
            {
                tip = tip[..(NativeMethods.TipLength - 1)];
            }

            var destination = new Span<char>(data.szTip, NativeMethods.TipLength);
            tip.CopyTo(destination);
            destination[tip.Length] = '\0';

            return data;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            _hookDepth++;

            try
            {
                uint message = (uint)msg;

                if (message == CallbackMessage)
                {
                    this.DecodeCallback(wParam, lParam);
                    handled = true;
                }
                else if (message == TaskbarCreatedMessage)
                {
                    // The shell has no record of us any more; the owner decides whether to re-register.
                    this.IsAdded = false;
                    this.ReleaseShellIcon();
                    _notify(new TrayNotification(TrayNotificationKind.TaskbarCreated, 0, 0));
                }
                else if (message is NativeMethods.WM_DISPLAYCHANGE or NativeMethods.WM_DPICHANGED or NativeMethods.WM_SETTINGCHANGE)
                {
                    _notify(new TrayNotification(TrayNotificationKind.DisplaySettingsChanged, 0, 0));
                }
            }
            finally
            {
                _hookDepth--;
            }

            return IntPtr.Zero;
        }

        private void DecodeCallback(IntPtr wParam, IntPtr lParam)
        {
            long l = lParam.ToInt64();
            int x;
            int y;
            uint eventCode;

            if (this.IsVersion4)
            {
                // NOTIFYICON_VERSION_4: LOWORD(lParam) is the event, HIWORD(lParam) the icon id, and
                // wParam carries the anchor coordinates (GET_X_LPARAM / GET_Y_LPARAM).
                eventCode = (uint)(l & 0xFFFF);

                if (((l >> 16) & 0xFFFF) != IconId)
                {
                    return;
                }

                long w = wParam.ToInt64();
                x = (short)(w & 0xFFFF);
                y = (short)((w >> 16) & 0xFFFF);
            }
            else
            {
                // Legacy protocol: lParam is the mouse message and there are no coordinates.
                eventCode = (uint)l;
                NativeMethods.GetCursorPos(out var pt);
                x = pt.X;
                y = pt.Y;
            }

            TrayNotificationKind kind;

            switch (eventCode)
            {
                case NativeMethods.WM_MOUSEMOVE:
                    kind = TrayNotificationKind.MouseMove;
                    break;
                case NativeMethods.WM_LBUTTONDOWN:
                    kind = TrayNotificationKind.LeftButtonDown;
                    break;
                case NativeMethods.WM_LBUTTONUP:
                    kind = TrayNotificationKind.LeftButtonUp;
                    break;
                case NativeMethods.WM_LBUTTONDBLCLK:
                    kind = TrayNotificationKind.LeftButtonDoubleClick;
                    break;
                case NativeMethods.WM_RBUTTONUP:
                    // Under the legacy protocol there is no WM_CONTEXTMENU; right-button-up is the request.
                    if (!this.IsVersion4)
                    {
                        _notify(new TrayNotification(TrayNotificationKind.RightButtonUp, x, y));
                        kind = TrayNotificationKind.ContextMenu;
                        break;
                    }

                    kind = TrayNotificationKind.RightButtonUp;
                    break;
                case NativeMethods.WM_CONTEXTMENU:
                    kind = TrayNotificationKind.ContextMenu;
                    break;
                case NativeMethods.NIN_KEYSELECT:
                    kind = TrayNotificationKind.KeySelect;
                    break;
                case NativeMethods.NIN_POPUPOPEN:
                    kind = TrayNotificationKind.PopupOpen;
                    break;
                case NativeMethods.NIN_POPUPCLOSE:
                    kind = TrayNotificationKind.PopupClose;
                    break;
                default:
                    // NIN_SELECT is intentionally ignored: clicks are derived from the button messages so that
                    // a double-click can be distinguished from a single click.
                    return;
            }

            _notify(new TrayNotification(kind, x, y));
        }

        /// <summary>
        /// Win32 declarations used by the tray icon. Internal to the library.
        /// </summary>
        internal static unsafe partial class NativeMethods
        {
            public const int TipLength = 128;

            public const uint NIM_ADD = 0x0;
            public const uint NIM_MODIFY = 0x1;
            public const uint NIM_DELETE = 0x2;
            public const uint NIM_SETVERSION = 0x4;

            public const uint NIF_MESSAGE = 0x1;
            public const uint NIF_ICON = 0x2;
            public const uint NIF_TIP = 0x4;
            public const uint NIF_SHOWTIP = 0x80;

            public const uint NOTIFYICON_VERSION_4 = 4;

            public const uint WM_MOUSEMOVE = 0x0200;
            public const uint WM_LBUTTONDOWN = 0x0201;
            public const uint WM_LBUTTONUP = 0x0202;
            public const uint WM_LBUTTONDBLCLK = 0x0203;
            public const uint WM_RBUTTONUP = 0x0205;
            public const uint WM_CONTEXTMENU = 0x007B;
            public const uint WM_SETTINGCHANGE = 0x001A;
            public const uint WM_DISPLAYCHANGE = 0x007E;
            public const uint WM_DPICHANGED = 0x02E0;

            public const uint NIN_SELECT = 0x0400;
            public const uint NIN_KEYSELECT = NIN_SELECT | 0x1;
            public const uint NIN_POPUPOPEN = 0x0406;
            public const uint NIN_POPUPCLOSE = 0x0407;

            public const int WS_EX_TOOLWINDOW = 0x00000080;
            public const uint MSGFLT_ALLOW = 1;
            public const uint MONITOR_DEFAULTTONEAREST = 2;
            public const uint MONITOR_DEFAULTTOPRIMARY = 1;
            public const int MDT_EFFECTIVE_DPI = 0;
            public const int SM_CXSMICON = 49;
            public const int IDI_APPLICATION = 32512;

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;

                public RECT(int left, int top, int right, int bottom)
                {
                    this.Left = left;
                    this.Top = top;
                    this.Right = right;
                    this.Bottom = bottom;
                }

                public readonly int Width => this.Right - this.Left;

                public readonly int Height => this.Bottom - this.Top;

                public readonly bool Contains(int x, int y) => x >= this.Left && x < this.Right && y >= this.Top && y < this.Bottom;
            }

            /// <summary>
            /// NOTIFYICONDATAW with fixed buffers so the struct is blittable (no marshaling allocations).
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct NOTIFYICONDATAW
            {
                public uint cbSize;
                public IntPtr hWnd;
                public uint uID;
                public uint uFlags;
                public uint uCallbackMessage;
                public IntPtr hIcon;
                public fixed char szTip[TipLength];
                public uint dwState;
                public uint dwStateMask;
                public fixed char szInfo[256];
                public uint uVersion; // union with uTimeout
                public fixed char szInfoTitle[64];
                public uint dwInfoFlags;
                public Guid guidItem;
                public IntPtr hBalloonIcon;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct NOTIFYICONIDENTIFIER
            {
                public uint cbSize;
                public IntPtr hWnd;
                public uint uID;
                public Guid guidItem;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MONITORINFO
            {
                public uint cbSize;
                public RECT rcMonitor;
                public RECT rcWork;
                public uint dwFlags;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct ICONINFO
            {
                public int fIcon;
                public uint xHotspot;
                public uint yHotspot;
                public IntPtr hbmMask;
                public IntPtr hbmColor;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct BITMAPINFOHEADER
            {
                public uint biSize;
                public int biWidth;
                public int biHeight;
                public ushort biPlanes;
                public ushort biBitCount;
                public uint biCompression;
                public uint biSizeImage;
                public int biXPelsPerMeter;
                public int biYPelsPerMeter;
                public uint biClrUsed;
                public uint biClrImportant;
            }

            [LibraryImport(Common.Win32.Shell32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool Shell_NotifyIconW(uint dwMessage, NOTIFYICONDATAW* lpData);

            [LibraryImport(Common.Win32.Shell32)]
            public static partial int Shell_NotifyIconGetRect(NOTIFYICONIDENTIFIER* identifier, RECT* iconLocation);

            [LibraryImport(Common.Win32.Shell32, StringMarshalling = StringMarshalling.Utf16)]
            public static partial int SHDefExtractIconW(string pszIconFile, int iIndex, uint uFlags, IntPtr* phiconLarge, IntPtr* phiconSmall, uint nIconSize);

            [LibraryImport(Common.Win32.User32, StringMarshalling = StringMarshalling.Utf16)]
            public static partial uint RegisterWindowMessageW(string lpString);

            [LibraryImport(Common.Win32.User32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool ChangeWindowMessageFilterEx(IntPtr hwnd, uint message, uint action, IntPtr pChangeFilterStruct);

            [LibraryImport(Common.Win32.User32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool SetForegroundWindow(IntPtr hWnd);

            [LibraryImport(Common.Win32.User32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GetCursorPos(out POINT lpPoint);

            [LibraryImport(Common.Win32.User32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool DestroyIcon(IntPtr hIcon);

            /// <summary>
            /// Creates an icon by copying the supplied color and mask bitmaps. Straight BGRA color input produces
            /// correct DrawIconEx AC_SRC_OVER blending; supplying premultiplied input dims it in the native tests.
            /// </summary>
            [LibraryImport(Common.Win32.User32, SetLastError = true)]
            public static partial IntPtr CreateIconIndirect(ICONINFO* piconinfo);

            [LibraryImport(Common.Win32.User32)]
            public static partial IntPtr LoadIconW(IntPtr hInstance, IntPtr lpIconName);

            [LibraryImport(Common.Win32.User32)]
            public static partial uint GetDoubleClickTime();

            [LibraryImport(Common.Win32.User32)]
            public static partial IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

            [LibraryImport(Common.Win32.User32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GetMonitorInfoW(IntPtr hMonitor, MONITORINFO* lpmi);

            [LibraryImport(Common.Win32.User32)]
            public static partial int GetSystemMetricsForDpi(int nIndex, uint dpi);

            [LibraryImport(Common.Win32.User32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [LibraryImport(Common.Win32.Shcore)]
            public static partial int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

            [LibraryImport(Common.Win32.Gdi32, SetLastError = true)]
            public static partial IntPtr CreateDIBSection(IntPtr hdc, BITMAPINFOHEADER* pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);

            [LibraryImport(Common.Win32.Gdi32, SetLastError = true)]
            public static partial IntPtr CreateBitmap(int nWidth, int nHeight, uint nPlanes, uint nBitCount, void* lpBits);

            [LibraryImport(Common.Win32.Gdi32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static partial bool DeleteObject(IntPtr hObject);

            /// <summary>
            /// Returns the effective DPI of the monitor containing <paramref name="pt"/> as seen by the calling
            /// thread's DPI awareness (system-aware threads get the system DPI, per-monitor threads the real one).
            /// </summary>
            public static uint GetDpiForPoint(POINT pt, uint flags = MONITOR_DEFAULTTONEAREST)
            {
                var monitor = MonitorFromPoint(pt, flags);

                if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, MDT_EFFECTIVE_DPI, out uint dpiX, out _) == 0 && dpiX > 0)
                {
                    return dpiX;
                }

                return 96;
            }

            /// <summary>
            /// Gets the work area (physical pixels) of the monitor nearest <paramref name="pt"/>.
            /// </summary>
            public static bool TryGetWorkArea(POINT pt, out RECT workArea)
            {
                workArea = default;
                var monitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);

                if (monitor == IntPtr.Zero)
                {
                    return false;
                }

                var info = new MONITORINFO { cbSize = (uint)sizeof(MONITORINFO) };

                if (!GetMonitorInfoW(monitor, &info))
                {
                    return false;
                }

                workArea = info.rcWork;
                return true;
            }
        }
    }
}
