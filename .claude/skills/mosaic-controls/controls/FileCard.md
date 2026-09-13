# FileCard

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/FileCard/FileCard.cs` + `FileCard.xaml`  
**Example:** `src/MosaicWpfDemo/Views/Examples/FileCardExample.xaml`

## Description

A clickable card for one file on disk. It shows the Windows shell icon for the file type, the file name and a formatted size (such as `2.1 MB`). Only the file name is shown, and the full path appears as the tooltip. If the file doesn't exist, an error glyph replaces the icon and the size is hidden, but the file name is still shown.

The card rises with a deeper shadow on hover and flattens when pressed. With `IsTintEnabled` (on by default), the background gets a light tint (16%) of the icon's main color. The tint is mixed into the current theme's control background, so it stays within the Light, Dark or Blue palette and updates when the theme changes.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `FilePath` | `string?` | `null` | Full path of the file. Changing it re-reads the file from disk. |
| `IsTintEnabled` | `bool` | `true` | Tints the background with the icon's main color. |
| `CornerRadius` | `CornerRadius` | `6` | Card corner radius. |
| `IconSize` | `double` | `32` | Icon width and height. |
| `Command` | `ICommand?` | `null` | Runs on click. Receives `CommandParameter`, or `FilePath` when no parameter is set. |
| `CommandParameter` | `object?` | `null` | Optional command parameter. |
| `OpenFileOnClick` | `bool` | `false` | Opens the file through the Windows shell on click. Missing files do nothing. |

### Read-only state

| Property | Type | Description |
|---|---|---|
| `FileName` | `string` | File name part of `FilePath` (the text shown). |
| `FileSizeText` | `string` | Formatted size, or `""` when the file is missing. |
| `Icon` | `ImageSource?` | Shell icon, or the error glyph. |
| `FileExists` | `bool` | Whether the file was found at the last read. |
| `CardBackground` | `Brush?` | Theme background, tinted if enabled. |
| `IsPressed` | `bool` | True while the mouse button or Space/Enter is held down. |

## Events

| Event | Args | Description |
|---|---|---|
| `Click` | `RoutedEventArgs` (bubbling) | The card was clicked or activated with Space/Enter. |
| `OnError` | `EventHandler<Exception>` | `OpenFileOnClick` failed to open the file through the shell. Exceptions thrown by handlers are swallowed. |

## Methods

| Member | Description |
|---|---|
| `Refresh()` | Re-reads `FilePath` (name, size, icon, tint). The card doesn't watch the file system, so call this after the file changes. |
| `RaiseClick()` | Raises `Click`, runs `Command`, and opens the file if `OpenFileOnClick` is set. |

## XAML Example

```xml
xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"

<WrapPanel>
    <!-- Routed event -->
    <mosaic:FileCard Width="260" Margin="0,0,8,8"
                     FilePath="C:\Windows\System32\notepad.exe"
                     Click="FileCard_OnClick" />

    <!-- MVVM: the command receives the FilePath because CommandParameter is unset -->
    <mosaic:FileCard Width="260" Margin="0,0,8,8"
                     FilePath="{Binding ReportPath}"
                     Command="{Binding OpenFileCommand}"
                     IconSize="40"
                     IsTintEnabled="False" />

    <!-- Let the shell open the file -->
    <mosaic:FileCard Width="260" FilePath="{Binding LogPath}" OpenFileOnClick="True" />
</WrapPanel>
```

## Notes

- Template part: `PART_Card` (`Border`). The raise/press animation uses a transform and a `DropShadowEffect` on that border.
- Keyboard: Space/Enter works like a mouse click. A disabled card returns to its resting height.
- Automation peer: `FileCardAutomationPeer`. It is exposed as a `Button` named after the file and supports Invoke.
- For a whole directory listing, use [Files.md](Files.md). For drag-and-drop input, use [FileDropper.md](FileDropper.md).
