using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Themes;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
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
        /// The corner radius used by <see cref="WindowChrome"/>.
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
        /// The glass frame thickness used by <see cref="WindowChrome"/>.
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
                window.SizeChanged -= Window_SizeChanged;
                window.StateChanged -= Window_StateChanged;
                DetachMaximizeHook(window);
                ClearRoundWindowRegion(window);
                return;
            }

            window.SourceInitialized -= Window_SourceInitialized;
            window.SourceInitialized += Window_SourceInitialized;
            window.SizeChanged -= Window_SizeChanged;
            window.SizeChanged += Window_SizeChanged;
            window.StateChanged -= Window_StateChanged;
            window.StateChanged += Window_StateChanged;
            Apply(window);
        }

        private static void Window_SourceInitialized(object? sender, EventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                Apply(window);
            }
        }

        private static void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                ApplyRoundWindowRegion(window);
            }
        }

        private static void Window_StateChanged(object? sender, EventArgs e)
        {
            if (sender is Window window && GetIsEnabled(window))
            {
                ApplyRoundWindowRegion(window);
            }
        }

        private static void Apply(Window window)
        {
            if (window.WindowStyle != WindowStyle.None)
            {
                window.WindowStyle = WindowStyle.None;
            }

            if (window.AllowsTransparency)
            {
                window.AllowsTransparency = false;
            }

            var chrome = WindowChrome.GetWindowChrome(window) ?? new WindowChrome();
            chrome.CaptionHeight = GetCaptionHeight(window);
            chrome.CornerRadius = GetCornerRadius(window);
            chrome.ResizeBorderThickness = GetResizeBorderThickness(window);
            chrome.GlassFrameThickness = GetGlassFrameThickness(window);
            chrome.UseAeroCaptionButtons = GetUseAeroCaptionButtons(window);
            WindowChrome.SetWindowChrome(window, chrome);

            if (GetApplyThemeWindowBrushes(window))
            {
                window.SetResourceReference(Control.BackgroundProperty, MosaicTheme.WindowBackgroundBrush);
                window.SetResourceReference(Control.ForegroundProperty, MosaicTheme.WindowForegroundBrush);
            }

            AttachMaximizeHook(window);
            ApplyRoundBorder(window);
            ApplyRoundWindowRegion(window);
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
        /// Syncs the Windows 11 DWM window border color (<c>DWMWA_BORDER_COLOR</c>) to the window's
        /// <see cref="Control.BorderBrush"/>. Without this, DWM paints its own default-colored frame around the
        /// window, which overpaints any border set on the window along the straight edges (the rounded corners are
        /// clipped away by the window region, revealing the templated border underneath — producing a two-tone
        /// border). Requires Windows 11 (build 22000+); the call is a harmless no-op on earlier systems.
        /// </summary>
        private static void ApplyDwmBorderColor(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            int colorRef;

            // A zero-thickness border means "no border"; suppress the DWM frame so nothing is drawn.
            if (window.BorderThickness == default || window.BorderBrush is not SolidColorBrush brush)
            {
                colorRef = DwmColor.None;
            }
            else
            {
                var c = brush.Color;
                // DWMWA_BORDER_COLOR expects a COLORREF (0x00BBGGRR); alpha is ignored by DWM.
                colorRef = c.R | (c.G << 8) | (c.B << 16);
            }

            Win32.DwmSetWindowAttribute(hwnd, DwmWindowAttributes.BorderColor, ref colorRef, sizeof(int));
        }

        /// <summary>
        /// Applies (or clears) the rounded-border window template so a border set on the window follows the rounded
        /// chrome corners instead of being clipped.
        /// </summary>
        private static void ApplyRoundBorder(Window window)
        {
            if (GetRoundBorder(window))
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

        /// <summary>
        /// Applies a native rounded region to the HWND so pixels outside the rounded border are not rendered.
        /// </summary>
        private static void ApplyRoundWindowRegion(Window window)
        {
            if (!GetRoundBorder(window) || window.WindowState == WindowState.Maximized)
            {
                ClearRoundWindowRegion(window);
                return;
            }

            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd == IntPtr.Zero || !Win32.GetWindowRect(hwnd, out var rect))
            {
                return;
            }

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            double radius = GetMaxCornerRadius(GetCornerRadius(window));

            if (radius <= 0)
            {
                ClearRoundWindowRegion(window);
                return;
            }

            int dpi = Win32.GetDpiForWindow(hwnd);
            double dpiScale = dpi > 0 ? dpi / 96d : 1d;
            int diameter = Math.Max(1, (int)Math.Round(radius * 2d * dpiScale));

            SetDwmCornerPreference(hwnd, DwmWindowCornerPreference.Round);

            IntPtr region = Win32.CreateRoundRectRgn(0, 0, width + 1, height + 1, diameter, diameter);

            if (region == IntPtr.Zero)
            {
                return;
            }

            if (Win32.SetWindowRgn(hwnd, region, true) == 0)
            {
                Win32.DeleteObject(region);
            }
        }

        /// <summary>
        /// Clears any native rounded region applied by <see cref="ApplyRoundWindowRegion"/>.
        /// </summary>
        private static void ClearRoundWindowRegion(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            if (hwnd != IntPtr.Zero)
            {
                Win32.SetWindowRgn(hwnd, IntPtr.Zero, true);
                SetDwmCornerPreference(hwnd, DwmWindowCornerPreference.Default);
            }
        }

        private static double GetMaxCornerRadius(CornerRadius cornerRadius)
        {
            return Math.Max(
                Math.Max(cornerRadius.TopLeft, cornerRadius.TopRight),
                Math.Max(cornerRadius.BottomRight, cornerRadius.BottomLeft));
        }

        private static void SetDwmCornerPreference(IntPtr hwnd, DwmWindowCornerPreference preference)
        {
            int value = (int)preference;
            Win32.DwmSetWindowAttribute(hwnd, DwmWindowAttributes.WindowCornerPreference, ref value, sizeof(int));
        }
    }
}
