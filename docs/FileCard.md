# FileCard

A clickable card that represents a single file on disk. The operating system's icon for the file type
is shown on the left, with the file name and its formatted size (B, KB, MB, GB) stacked to the right.
Only the file name is displayed; the full path is the tooltip.

Missing files fall back to an error glyph and drop the size, while still showing the intended name.
With `IsTintEnabled` the card background is washed with a small amount of the icon's dominant color,
always mixed into the active theme's control background so the card stays inside the Light, Dark, and
Blue palettes. The card raises on hover, lowers while pressed, and fires `Click` plus a `Command`
(which receives the `FilePath` when no `CommandParameter` is set) on release.

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `FilePath` | `string?` | `null` | The full path of the represented file. The card displays only the file name. |
| `IsTintEnabled` | `bool` | `true` | Tints the card background with a small amount of the file icon's dominant color. |
| `OpenFileOnClick` | `bool` | `false` | Opens `FilePath` through the operating system shell when the card is clicked. |
| `Command` | `ICommand?` | `null` | An optional command executed when the card is clicked. |
| `CommandParameter` | `object?` | `null` | The command parameter. When unset, `FilePath` is used. |

## Events

| Event | Event arguments | Description |
| --- | --- | --- |
| `Click` | `RoutedEventArgs` | Raised when the card is clicked with the mouse or activated with Space or Enter. |
| `OnError` | `Exception` | Raised when `OpenFileOnClick` is enabled but the operating system shell cannot open the file. |

## Opening a file on click

Shell opening is opt-in so existing `FileCard` instances continue to act only as clickable command
surfaces. Set `OpenFileOnClick="True"` to ask Windows to open the file with its registered application,
or to execute it when `FilePath` refers to an executable:

```xml
<mosaic:FileCard
    FilePath="{Binding ReportPath}"
    OpenFileOnClick="True"
    Command="{Binding FileOpenedCommand}"
    OnError="FileCard_OnError" />
```

The `Click` event and optional `Command` still run when shell opening is enabled. If the path is blank
or the file no longer exists, shell opening stops without raising an error. If Windows cannot open or
execute an existing file, the exception is caught by the control and passed to `OnError`; it does not
escape through the click handler.

```csharp
private void FileCard_OnError(object? sender, Exception exception)
{
    // Log or display exception.Message as appropriate for the host application.
}
```
