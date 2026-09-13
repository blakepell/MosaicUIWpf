# WindowTitleBar

**Base class:** `UserControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/WindowTitleBar/WindowTitleBar.xaml.cs`  
**Example:** `src/MosaicWpfDemo/MainWindow.xaml`

## Description

A custom window title bar `UserControl` providing Minimize, Maximize/Restore, and Close buttons alongside a configurable icon, title text, and arbitrary left/right content slots. Works together with `WindowChromeBehavior` (or `WindowChrome`) to replace the OS title bar.

`WindowTitleBar` is a `UserControl` and cannot be re-templated.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `TitleText` | `string` | `null` | The application title shown in the title bar. |
| `TitleAlignment` | `HorizontalAlignment` | `Left` | Horizontal alignment of the title text. |
| `IconSource` | `ImageSource` | `null` | The window icon image. |
| `ShowIcon` | `bool` | `true` | Whether the icon is displayed. |
| `IsStretchButtonVisible` | `bool` | `false` | Shows the stretch (auto resize) button to the left of minimize. See `StretchWindowButton` below. |
| `ShowMinimizeButton` | `bool` | `true` | Show/hide the minimize (−) button. |
| `ShowMaxRestoreButton` | `bool` | `true` | Show/hide the maximize/restore (⬜) button. |
| `ShowCloseButton` | `bool` | `true` | Show/hide the close (✕) button. |
| `LeftContent` | `object` | `null` | Arbitrary content placed to the left of the title (e.g., a logo). |
| `RightContent` | `object` | `null` | Arbitrary content placed to the right of the title and before the window buttons (e.g., notification icons). |
| `MaximizedGlyph` | `string` | `"\xE923"` (Restore) | Segoe MDL2 glyph on the max/restore button while the window is maximized. |
| `RestoredGlyph` | `string` | `"\xE922"` (Maximize) | Segoe MDL2 glyph on the max/restore button while the window is in its normal state. |

## StretchWindowButton

`StretchWindowButton` (`UserControl`, `Mosaic.UI.Wpf.Controls`, `Controls/WindowTitleBar/StretchWindowButton.xaml`) is the title-bar button with the tooltip "Auto Resize Window". When clicked, it returns the owning window to its normal state. It then resizes the window to 90% × 90% of the work area on the monitor the window is currently on (the work area excludes the taskbar and is DPI-aware), keeping the window's `MinWidth`/`MinHeight`, and centers it. `WindowTitleBar` hosts it behind `IsStretchButtonVisible`, and you can also place it on its own in custom chrome.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"
xmlns:behaviors="clr-namespace:Mosaic.UI.Wpf.Behaviors;assembly=Mosaic.UI.Wpf"

<Window ...>
    <WindowChrome.WindowChrome>
        <WindowChrome CaptionHeight="32" ResizeBorderThickness="5" GlassFrameThickness="-1" />
    </WindowChrome.WindowChrome>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Title bar occupies the drag region -->
        <mosaic:WindowTitleBar
            Grid.Row="0"
            TitleText="My Application"
            TitleAlignment="Center"
            IconSource="/Assets/icon-16.png"
            ShowMinimizeButton="True"
            ShowMaxRestoreButton="True"
            ShowCloseButton="True">

            <!-- Optional right-side content (before window buttons) -->
            <mosaic:WindowTitleBar.RightContent>
                <Button Content="⚙" ToolTip="Settings" />
            </mosaic:WindowTitleBar.RightContent>
        </mosaic:WindowTitleBar>

        <!-- Main content -->
        <ContentControl Grid.Row="1" ... />
    </Grid>
</Window>
```

## Binding to Parent Window Title

```xml
<mosaic:WindowTitleBar
    TitleText="{Binding Title, RelativeSource={RelativeSource AncestorType=Window}}" />
```

## Notes

- `WindowTitleBar` expects the window to have a `WindowChrome` applied (either via `WindowChromeBehavior` behavior or directly via `WindowChrome.WindowChrome`). Without it, the OS title bar will overlap.
- The caption height in `WindowChrome` should match the `Height` of the `WindowTitleBar` row.
- `MosaicApp<TSettings, TViewModel>` and the `mosaic-setup-project` skill wire this automatically.
- Minimize / Maximize / Close buttons are implemented to call `SystemCommands` methods on the parent window.
