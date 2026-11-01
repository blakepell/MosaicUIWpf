/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Themes;
using System.Windows.Interop;
using System.Windows.Data;
using System.Windows.Shell;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Attached behavior to apply and maintain <see cref="WindowChrome"/> settings on a <see cref="Window"/>.
    /// </summary>
    public static class WindowChromeBehavior
    {
        /// <summary>
        /// Enables or disables the behavior on a <see cref="Window"/>.
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(false, OnChromeSettingsChanged));

        /// <summary>
        /// The non-client caption height used by <see cref="WindowChrome"/>.
        /// </summary>
        public static readonly DependencyProperty CaptionHeightProperty = DependencyProperty.RegisterAttached(
            "CaptionHeight",
            typeof(double),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(0d, OnChromeSettingsChanged));

        /// <summary>
        /// The fallback corner radius used by <see cref="WindowChrome"/>. Native mode uses the OS radius.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
            "CornerRadius",
            typeof(CornerRadius),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(new CornerRadius(10), OnChromeSettingsChanged));

        /// <summary>
        /// The resize border thickness used by <see cref="WindowChrome"/>.
        /// </summary>
        public static readonly DependencyProperty ResizeBorderThicknessProperty = DependencyProperty.RegisterAttached(
            "ResizeBorderThickness",
            typeof(Thickness),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(new Thickness(5), OnChromeSettingsChanged));

        /// <summary>
        /// The fallback glass frame thickness used by <see cref="WindowChrome"/>. Native mode reserves a small DWM frame.
        /// </summary>
        public static readonly DependencyProperty GlassFrameThicknessProperty = DependencyProperty.RegisterAttached(
            "GlassFrameThickness",
            typeof(Thickness),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(new Thickness(0), OnChromeSettingsChanged));

        /// <summary>
        /// Indicates whether native aero caption buttons are used by <see cref="WindowChrome"/>.
        /// </summary>
        public static readonly DependencyProperty UseAeroCaptionButtonsProperty = DependencyProperty.RegisterAttached(
            "UseAeroCaptionButtons",
            typeof(bool),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(true, OnChromeSettingsChanged));

        /// <summary>
        /// Applies themed background and foreground brushes to the target <see cref="Window"/>.
        /// </summary>
        public static readonly DependencyProperty ApplyThemeWindowBrushesProperty = DependencyProperty.RegisterAttached(
            "ApplyThemeWindowBrushes",
            typeof(bool),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(false, OnChromeSettingsChanged));

        /// <summary>
        /// When enabled, the target <see cref="Window"/> is given a template whose border follows the rounded
        /// <see cref="CornerRadiusProperty"/> chrome corners. The window's own <see cref="Control.BorderBrush"/> and
        /// <see cref="Control.BorderThickness"/> are drawn as a rounded border that rounds with the window instead of a
        /// square border being clipped at the corners.
        /// </summary>
        public static readonly DependencyProperty RoundBorderProperty = DependencyProperty.RegisterAttached(
            "RoundBorder",
            typeof(bool),
            typeof(WindowChromeBehavior),
            new PropertyMetadata(false, OnChromeSettingsChanged));

        /// <summary>
        /// Enables system-rendered corners, borders, and shadows on Windows 11. Defaults to true.
        /// Earlier Windows versions continue to use WPF chrome rounding.
        /// </summary>
        public static readonly DependencyProperty UseNativeCornersProperty = DependencyProperty.RegisterAttached(
            "UseNativeCorners", typeof(bool), typeof(WindowChromeBehavior),
            new PropertyMetadata(true, OnChromeSettingsChanged));

        /// <summary>
        /// Controls visibility of the DWM border without affecting native rounding or shadows. Defaults to true.
        /// Native border thickness is controlled by Windows.
        /// </summary>
        public static readonly DependencyProperty ShowNativeBorderProperty = DependencyProperty.RegisterAttached(
            "ShowNativeBorder", typeof(bool), typeof(WindowChromeBehavior),
            new PropertyMetadata(true, OnBorderSettingsChanged));

        private static readonly DependencyProperty ThemeBorderBrushProperty = DependencyProperty.RegisterAttached(
            "ThemeBorderBrush", typeof(Brush), typeof(WindowChromeBehavior),
            new PropertyMetadata(null, OnBorderSettingsChanged));

        private static readonly DependencyProperty ObservedBorderBrushProperty = DependencyProperty.RegisterAttached(
            "ObservedBorderBrush", typeof(Brush), typeof(WindowChromeBehavior),
            new PropertyMetadata(null, OnBorderSettingsChanged));

        /// <summary>
        /// Gets whether the native window border is visible.
        /// </summary>
        public static bool GetShowNativeBorder(DependencyObject obj) => (bool)obj.GetValue(ShowNativeBorderProperty);

        /// <summary>
        /// Sets whether the native window border is visible.
        /// </summary>
        public static void SetShowNativeBorder(DependencyObject obj, bool value) => obj.SetValue(ShowNativeBorderProperty, value);

        private static void OnBorderSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window && GetIsEnabled(window))
            {
                ApplyDwmBorderColor(window);
            }
        }

        private static readonly DependencyPropertyKey IsNativeCornerModePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsNativeCornerMode", typeof(bool), typeof(WindowChromeBehavior), new PropertyMetadata(false));

        /// <summary>
        /// Identifies whether the window is currently using system-rendered corners.
        /// </summary>
        public static readonly DependencyProperty IsNativeCornerModeProperty = IsNativeCornerModePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether native Windows 11 corners are requested.
        /// </summary>
        public static bool GetUseNativeCorners(DependencyObject obj) => (bool)obj.GetValue(UseNativeCornersProperty);

        /// <summary>
        /// Sets whether native Windows 11 corners are requested. Custom radii and glass thickness apply only in fallback mode.
        /// </summary>
        public static void SetUseNativeCorners(DependencyObject obj, bool value) => obj.SetValue(UseNativeCornersProperty, value);

        /// <summary>
        /// Gets whether the window is currently using system-rendered corners.
        /// </summary>
        public static bool GetIsNativeCornerMode(DependencyObject obj) => (bool)obj.GetValue(IsNativeCornerModeProperty);

        /// <summary>
        /// Gets whether the behavior is enabled.
        /// </summary>
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets whether the behavior is enabled.
        /// </summary>
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        /// <summary>
        /// Gets the caption height.
        /// </summary>
        public static double GetCaptionHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(CaptionHeightProperty);
        }

        /// <summary>
        /// Sets the caption height.
        /// </summary>
        public static void SetCaptionHeight(DependencyObject obj, double value)
        {
            obj.SetValue(CaptionHeightProperty, value);
        }

        /// <summary>
        /// Gets the corner radius.
        /// </summary>
        public static CornerRadius GetCornerRadius(DependencyObject obj)
        {
            return (CornerRadius)obj.GetValue(CornerRadiusProperty);
        }

        /// <summary>
        /// Sets the corner radius.
        /// </summary>
        public static void SetCornerRadius(DependencyObject obj, CornerRadius value)
        {
            obj.SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets the resize border thickness.
        /// </summary>
        public static Thickness GetResizeBorderThickness(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(ResizeBorderThicknessProperty);
        }

        /// <summary>
        /// Sets the resize border thickness.
        /// </summary>
        public static void SetResizeBorderThickness(DependencyObject obj, Thickness value)
        {
            obj.SetValue(ResizeBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets the glass frame thickness.
        /// </summary>
        public static Thickness GetGlassFrameThickness(DependencyObject obj)
        {
            return (Thickness)obj.GetValue(GlassFrameThicknessProperty);
        }

        /// <summary>
        /// Sets the glass frame thickness.
        /// </summary>
        public static void SetGlassFrameThickness(DependencyObject obj, Thickness value)
        {
            obj.SetValue(GlassFrameThicknessProperty, value);
        }

        /// <summary>
        /// Gets whether aero caption buttons are used.
        /// </summary>
        public static bool GetUseAeroCaptionButtons(DependencyObject obj)
        {
            return (bool)obj.GetValue(UseAeroCaptionButtonsProperty);
        }

        /// <summary>
        /// Sets whether aero caption buttons are used.
        /// </summary>
        public static void SetUseAeroCaptionButtons(DependencyObject obj, bool value)
        {
            obj.SetValue(UseAeroCaptionButtonsProperty, value);
        }

        /// <summary>
        /// Gets whether theme brushes are applied to the target window.
        /// </summary>
        public static bool GetApplyThemeWindowBrushes(DependencyObject obj)
        {
            return (bool)obj.GetValue(ApplyThemeWindowBrushesProperty);
        }

        /// <summary>
        /// Sets whether theme brushes are applied to the target window.
        /// </summary>
        public static void SetApplyThemeWindowBrushes(DependencyObject obj, bool value)
        {
            obj.SetValue(ApplyThemeWindowBrushesProperty, value);
        }

        /// <summary>
        /// Gets whether the window's border follows the rounded chrome corners.
        /// </summary>
        public static bool GetRoundBorder(DependencyObject obj)
        {
            return (bool)obj.GetValue(RoundBorderProperty);
        }

        /// <summary>
        /// Sets whether the window's border follows the rounded chrome corners.
        /// </summary>
        public static void SetRoundBorder(DependencyObject obj, bool value)
        {
            obj.SetValue(RoundBorderProperty, value);
        }

        private static void OnChromeSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
            {
                return;
            }

            if (!GetIsEnabled(window))
            {
                window.SourceInitialized -= Window_SourceInitialized;
                window.Loaded -= Window_BorderStateChanged;
                window.Activated -= Window_BorderStateChanged;
                window.Deactivated -= Window_BorderStateChanged;
                DetachMaximizeHook(window);
                WindowChrome.SetWindowChrome(window, null);
                BindingOperations.ClearBinding(window, ObservedBorderBrushProperty);
                window.ClearValue(ThemeBorderBrushProperty);
                window.SetValue(IsNativeCornerModePropertyKey, false);
                SetDwmCornerPreference(new WindowInteropHelper(window).Handle, DwmWindowCornerPreference.Default);
                return;
            }

            window.SourceInitialized -= Window_SourceInitialized;
            window.SourceInitialized += Window_SourceInitialized;
            window.Loaded -= Window_BorderStateChanged;
            window.Loaded += Window_BorderStateChanged;
            window.Activated -= Window_BorderStateChanged;
            window.Activated += Window_BorderStateChanged;
            window.Deactivated -= Window_BorderStateChanged;
            window.Deactivated += Window_BorderStateChanged;
            Apply(window);
        }

        private static void Window_BorderStateChanged(object? sender, EventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                ApplyDwmBorderColor(window);
            }
        }

        private static void Window_SourceInitialized(object? sender, EventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                Apply(window);
            }
        }

        private static void Apply(Window window)
        {
            bool native = GetUseNativeCorners(window) && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
            window.SetValue(IsNativeCornerModePropertyKey, native);

            if (window.AllowsTransparency)
            {
                window.AllowsTransparency = false;
            }

            // Retain standard frame styles so DWM can supply its border and shadow.
            window.WindowStyle = native ? WindowStyle.SingleBorderWindow : WindowStyle.None;

            var chrome = WindowChrome.GetWindowChrome(window) ?? new WindowChrome();
            if (chrome.IsFrozen)
            {
                chrome = (WindowChrome)chrome.Clone();
            }
            chrome.CaptionHeight = GetCaptionHeight(window);
            chrome.CornerRadius = native ? new CornerRadius(0) : GetCornerRadius(window);
            chrome.ResizeBorderThickness = GetResizeBorderThickness(window);
            // A nonzero glass extension keeps WindowChromeWorker out of its SetWindowRgn path.
            // Use one DIP; WPF scales this to physical pixels on each monitor.
            chrome.GlassFrameThickness = native ? new Thickness(1) : GetGlassFrameThickness(window);
            chrome.UseAeroCaptionButtons = GetUseAeroCaptionButtons(window);
            WindowChrome.SetWindowChrome(window, chrome);

            if (GetApplyThemeWindowBrushes(window))
            {
                window.SetResourceReference(Control.BackgroundProperty, MosaicTheme.WindowBackgroundBrush);
                window.SetResourceReference(Control.ForegroundProperty, MosaicTheme.WindowForegroundBrush);
            }

            // Observe the caller brush and theme resource without overwriting either. DynamicResource
            // invalidation and brush subproperty changes refresh DWM without rebuilding window chrome.
            if (!BindingOperations.IsDataBound(window, ObservedBorderBrushProperty))
            {
                BindingOperations.SetBinding(window, ObservedBorderBrushProperty,
                    new Binding(nameof(Window.BorderBrush)) { RelativeSource = RelativeSource.Self });
            }
            window.SetResourceReference(ThemeBorderBrushProperty, MosaicTheme.WindowBorderBrush);

            AttachMaximizeHook(window);
            ApplyRoundBorder(window);
            SetDwmCornerPreference(new WindowInteropHelper(window).Handle,
                native ? DwmWindowCornerPreference.Round : DwmWindowCornerPreference.Default);
            ApplyDwmBorderColor(window);
        }

        /// <summary>
        /// Hooks <c>WM_GETMINMAXINFO</c> on the window so a maximized borderless window is constrained to the
        /// monitor's work area rather than covering the whole screen (and with it, the taskbar).
        /// </summary>
        /// <remarks>
        /// A <see cref="WindowStyle.None"/> window has no non-client frame, so the default maximized size Windows
        /// hands it is the full monitor rectangle instead of the work area. Supplying the work area here restores
        /// normal maximize behavior.
        /// </remarks>
        private static void AttachMaximizeHook(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero || HwndSource.FromHwnd(hwnd) is not { } source)
            {
                return;
            }

            // AddHook does not de-duplicate, so always remove first.
            source.RemoveHook(MaximizeHook);
            source.AddHook(MaximizeHook);
        }

        /// <summary>
        /// Removes the <c>WM_GETMINMAXINFO</c> hook applied by <see cref="AttachMaximizeHook"/>.
        /// </summary>
        private static void DetachMaximizeHook(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd != IntPtr.Zero && HwndSource.FromHwnd(hwnd) is { } source)
            {
                source.RemoveHook(MaximizeHook);
            }
        }

        private static IntPtr MaximizeHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WindowsMessageCodes.WM_GETMINMAXINFO)
            {
                ApplyWorkAreaToMinMaxInfo(hwnd, lParam);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Fills in the maximized position/size of a <c>MINMAXINFO</c> payload from the work area of the monitor
        /// the window currently lives on.
        /// </summary>
        private static void ApplyWorkAreaToMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            if (lParam == IntPtr.Zero)
            {
                return;
            }

            var monitor = Win32.MonitorFromWindow(hwnd, Win32.MONITOR_DEFAULTTONEAREST);

            if (monitor == IntPtr.Zero)
            {
                return;
            }

            var monitorInfo = new Win32.MONITORINFO { cbSize = Marshal.SizeOf<Win32.MONITORINFO>() };

            if (!Win32.GetMonitorInfo(monitor, ref monitorInfo))
            {
                return;
            }

            var work = monitorInfo.rcWork;
            var bounds = monitorInfo.rcMonitor;

            ReserveAutoHideTaskbarEdge(ref work, bounds);

            var minMax = Marshal.PtrToStructure<Win32.MINMAXINFO>(lParam);

            // The maximized position is expressed relative to the monitor's upper-left corner.
            minMax.ptMaxPosition.X = work.Left - bounds.Left;
            minMax.ptMaxPosition.Y = work.Top - bounds.Top;
            minMax.ptMaxSize.X = work.Right - work.Left;
            minMax.ptMaxSize.Y = work.Bottom - work.Top;
            minMax.ptMaxTrackSize.X = work.Right - work.Left;
            minMax.ptMaxTrackSize.Y = work.Bottom - work.Top;

            Marshal.StructureToPtr(minMax, lParam, true);
        }

        /// <summary>
        /// When the taskbar is set to auto-hide, its edge is not excluded from the monitor work area. If a maximized
        /// window covers that edge exactly, the taskbar can no longer be summoned, so a single pixel is reserved.
        /// </summary>
        private static void ReserveAutoHideTaskbarEdge(ref Win32.RECT work, Win32.RECT monitorBounds)
        {
            var state = new Win32.APPBARDATA { cbSize = Marshal.SizeOf<Win32.APPBARDATA>() };

            if ((Win32.SHAppBarMessage(AppBar.ABM_GETSTATE, ref state).ToInt32() & AppBar.ABS_AUTOHIDE) == 0)
            {
                return;
            }

            if (HasAutoHideBar(AppBar.ABE_BOTTOM, monitorBounds))
            {
                work.Bottom -= 1;
            }
            else if (HasAutoHideBar(AppBar.ABE_TOP, monitorBounds))
            {
                work.Top += 1;
            }
            else if (HasAutoHideBar(AppBar.ABE_LEFT, monitorBounds))
            {
                work.Left += 1;
            }
            else if (HasAutoHideBar(AppBar.ABE_RIGHT, monitorBounds))
            {
                work.Right -= 1;
            }
        }

        private static bool HasAutoHideBar(uint edge, Win32.RECT monitorBounds)
        {
            var data = new Win32.APPBARDATA
            {
                cbSize = Marshal.SizeOf<Win32.APPBARDATA>(),
                uEdge = edge,
                rc = monitorBounds
            };

            return Win32.SHAppBarMessage(AppBar.ABM_GETAUTOHIDEBAREX, ref data) != IntPtr.Zero;
        }

        /// <summary>
        /// Syncs the Windows 11 DWM border color to a solid window border brush. Native mode otherwise
        /// uses the system default border; fallback mode suppresses it when no WPF border is requested.
        /// </summary>
        private static void ApplyDwmBorderColor(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            int colorRef = ResolveDwmBorderColor(window);
            Win32.DwmSetWindowAttribute(hwnd, DwmWindowAttributes.BorderColor, ref colorRef, sizeof(int));
        }

        private static int ResolveDwmBorderColor(Window window)
        {
            bool native = GetIsNativeCornerMode(window);
            Brush? borderBrush = window.BorderBrush;
            if (native && (borderBrush == null ||
                DependencyPropertyHelper.GetValueSource(window, Control.BorderBrushProperty).BaseValueSource == BaseValueSource.Default))
            {
                borderBrush = (Brush?)window.GetValue(ThemeBorderBrushProperty);
            }

            int colorRef;
            if ((native && !GetShowNativeBorder(window)) ||
                (!native && window.BorderThickness == default) ||
                borderBrush is SolidColorBrush { Color.A: 0 } || borderBrush?.Opacity == 0)
            {
                colorRef = DwmColor.None;
            }
            else if (borderBrush is SolidColorBrush brush)
            {
                var c = brush.Color;
                // DWM accepts an opaque COLORREF (0x00BBGGRR), not an alpha channel.
                colorRef = c.R | (c.G << 8) | (c.B << 16);
            }
            else
            {
                colorRef = native ? DwmColor.Default : DwmColor.None;
            }

            return colorRef;
        }

        /// <summary>
        /// Applies (or clears) the rounded-border window template so a border set on the window follows the rounded
        /// chrome corners instead of being clipped.
        /// </summary>
        private static void ApplyRoundBorder(Window window)
        {
            if (GetRoundBorder(window) || GetIsNativeCornerMode(window))
            {
                if (window.TryFindResource(MosaicTheme.RoundedWindowTemplate) is ControlTemplate template)
                {
                    window.Template = template;
                }
            }
            else if (window.Template != null && ReferenceEquals(window.TryFindResource(MosaicTheme.RoundedWindowTemplate), window.Template))
            {
                // Only clear the template if we are the ones who set it.
                window.ClearValue(Control.TemplateProperty);
            }
        }

        private static void SetDwmCornerPreference(IntPtr hwnd, DwmWindowCornerPreference preference)
        {
            if (hwnd == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                return;
            }

            int value = (int)preference;
            Win32.DwmSetWindowAttribute(hwnd, DwmWindowAttributes.WindowCornerPreference, ref value, sizeof(int));
        }
    }
}
