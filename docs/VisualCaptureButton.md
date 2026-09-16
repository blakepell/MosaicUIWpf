# VisualCaptureButton

A button that renders another element to an image and either copies it to the clipboard or saves it to a file, as selected by `Mode`. If the target element scrolls (it is, or contains, a `ScrollViewer`), the element is temporarily laid out at its full scroll extent so the entire vertical and horizontal content is captured rather than only the visible viewport. Virtualized lists realize all of their items for the capture.

The button derives from `AccentButton`, so `AccentButtonType` and `CornerRadius` work unchanged. When no `Content` is supplied it shows "Copy Image" or "Save Image..." depending on `Mode`.

## Key Properties

| Property | Description |
|---|---|
| `TargetName` | The `x:Name` of the element to capture. Resolved through the button's name scope and then the name scopes of its logical ancestors, so siblings in the same XAML file and elements in an enclosing UserControl, Window, or template are found. |
| `Target` | The element to capture, typically `{Binding ElementName=...}`. Takes precedence over `TargetName`. |
| `Mode` | `CopyToClipboard` (default) or `SaveToFile`. |
| `FilePath` | For `SaveToFile`, the file to write. When empty the user is prompted with a save dialog; the format is inferred from the chosen extension (.png, .jpg, .bmp, .gif, .tif). |
| `DefaultFileName` | The file name pre-populated in the save dialog. |
| `CaptureBackground` | A brush painted behind the element. `null` (default) keeps unpainted areas transparent. Set this to the window background when pasting into applications that do not handle transparency. |
| `CaptureScrollExtent` | Whether a scrolling target is expanded to its full extent. Defaults to `true`. |

## Events

| Event | Description |
|---|---|
| `Captured` | Bubbles after the image was copied or saved. `VisualCapturedEventArgs` carries the `Mode`, the `Target`, and the `FilePath` for saves. Not raised when the user cancels the save dialog. |
| `CaptureFailed` | Bubbles when the capture could not complete (target not found, clipboard unavailable, file error). `Error` holds the exception. |

## Example

```xml
<DataGrid x:Name="ResultsGrid" ... />

<mosaic:VisualCaptureButton TargetName="ResultsGrid" Mode="CopyToClipboard" />
<mosaic:VisualCaptureButton
    Target="{Binding ElementName=ResultsGrid}"
    Mode="SaveToFile"
    DefaultFileName="results.png"
    CaptureBackground="{DynamicResource {x:Static themes:MosaicTheme.WindowBackgroundBrush}}"
    Content="Export PNG" />
```

## The VisualCapture utility

The button is a thin wrapper over `Mosaic.UI.Wpf.Common.VisualCapture`, which can be used directly from code:

```csharp
// Copy the full contents of a DataGrid, including rows scrolled out of view.
VisualCapture.CopyToClipboard(ResultsGrid);

// Prompt for a file name and save; returns null if the user cancels.
string? path = VisualCapture.SaveToFile(ResultsGrid);

// Save to a known path; format inferred from the extension.
VisualCapture.SaveToFile(ResultsGrid, @"C:\Temp\results.jpg", new VisualCaptureOptions { JpegQuality = 85 });

// Get the bitmap and do something else with it.
RenderTargetBitmap bitmap = VisualCapture.Render(ResultsGrid, new VisualCaptureOptions { Background = Brushes.White, Dpi = 192 });
```

`VisualCaptureOptions` exposes `CaptureScrollExtent`, `Background`, `Dpi` (defaults to the monitor's DPI), `JpegQuality`, `DefaultFileName`, and `InitialDirectory`.

Clipboard copies place both a device independent bitmap and a PNG stream on the clipboard so applications that understand PNG keep transparency.
