# WindowChromeBehavior

Attached behavior to apply and maintain WindowChrome settings on a Window.

![WindowChromeBehavior](./images/WindowChromeBehavior.png)

## Native Windows 11 chrome

`UseNativeCorners` defaults to `True`. On Windows 11 (build 22000 or newer),
Windows renders the rounded corners, outer border, and shadow. Existing windows
with `IsEnabled="True"` automatically use this mode, including windows without
`RoundBorder` enabled.

```xml
<Window
    xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
    mosaic:WindowChromeBehavior.IsEnabled="True"
    mosaic:WindowChromeBehavior.UseNativeCorners="True">
    <!-- Window content, including WindowTitleBar -->
</Window>
```

Native mode retains standard window frame styles and uses a one-DIP glass frame
extension so WPF does not install a window region. It requests `DWMWCP_ROUND`;
Windows determines the radius and suppresses rounding when maximized or snapped.
The Mosaic template leaves the outside border to DWM instead of drawing and
clipping a second border. By default the border uses `MosaicTheme.WindowBorderBrush`,
with subtle colors for Light, Dark, Black, and Blue and the theme window-text
color for High Contrast. The color updates when the theme changes.

Set the window's standard `BorderBrush` to override the theme color. Changes at
runtime, including dynamic resources and solid brush color changes, update DWM.
Native borders accept solid, opaque colors; a fully transparent brush hides the
border. Other alpha values are ignored by DWM; non-solid brushes use the OS default.

```xml
<!-- Override the native border color. -->
<Window BorderBrush="#303848"
        mosaic:WindowChromeBehavior.IsEnabled="True" />

<!-- Hide the border while retaining native corners and shadow. -->
<Window mosaic:WindowChromeBehavior.IsEnabled="True"
        mosaic:WindowChromeBehavior.ShowNativeBorder="False" />
```

Override `MosaicTheme.WindowBorderBrush` in window resources to customize the default
for one window, or in the host resource scope to customize the theme default.
Call `ClearValue(Control.BorderBrushProperty)` to return a caller override to the
theme default. Without a theme dictionary, Windows supplies its default border.

Native border thickness is controlled by Windows and varies with DPI;
`BorderThickness` does not change it. For a custom WPF border thickness, use
`UseNativeCorners="False"`, `RoundBorder="True"`, and the standard window
`BorderBrush` / `BorderThickness` properties.

`CornerRadius` and `GlassFrameThickness` apply to fallback mode only. To retain
custom corner radii or the prior WPF appearance, set `UseNativeCorners="False"`.
On earlier Windows versions this fallback is automatic. WPF WindowChrome alone
owns the fallback HWND region; Mosaic no longer installs a competing region.
`RoundBorder="True"` enables the rounded WPF border template in fallback mode.
`IsNativeCornerMode` is a read-only attached property indicating the active mode.

Use opaque windows (`AllowsTransparency="False"`). System rounding follows Windows
policy and is not guaranteed in remote or virtualized sessions. See
[Microsoft rounding guidance](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners).

For validation, check initial display, resize, maximize/restore, minimize/restore,
snap/unsnap, and moving between monitors with different DPI settings. Native
windows should have no custom HWND region in the normal state. Check both
`RoundBorder` settings and switch `UseNativeCorners` at runtime to exercise fallback.
