using Mosaic.UI.Wpf.Themes;
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

        private static void OnChromeSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
            {
                return;
            }

            if (!GetIsEnabled(window))
            {
                window.SourceInitialized -= Window_SourceInitialized;
                return;
            }

            window.SourceInitialized -= Window_SourceInitialized;
            window.SourceInitialized += Window_SourceInitialized;
            Apply(window);
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
        }
    }
}
