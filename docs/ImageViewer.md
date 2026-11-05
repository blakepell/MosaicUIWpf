# ImageViewer

An image viewer that works like the Windows Photos viewer. The image opens fitted to the available space. The mouse wheel zooms around the cursor. When the image is larger than the viewport, you can click and drag to pan, and double-clicking switches between "fit" and actual size. A status bar shows the resolution, file size, rotate/fit/actual-size buttons and a zoom slider.

## Usage

```xml
<!-- A file path, or a pack/relative resource URI. -->
<mosaic:ImageViewer ImagePath="/Assets/photo.jpg" />

<!-- Or supply any ImageSource directly (no file size is reported). -->
<mosaic:ImageViewer Source="{Binding Thumbnail}" ShowStatusBar="False" />
```

```csharp
viewer.LoadImage(@"C:\Pictures\sunset.jpg");
viewer.ZoomTo(2.0, anchor: null);   // 200%, centered on the viewport
viewer.RotateRight();
```

`ImagePath` reads the file into memory, so no file lock is held. It accepts file system paths, absolute URIs and resource paths such as `/Assets/photo.jpg` (resolved against the application's `pack://application:,,,/` root). If the image can't be loaded, the viewer raises `ImageFailed` and doesn't show a message box itself.

## Zoom

A `Zoom` of `1.0` is 100%: one image pixel per physical screen pixel, whatever the monitor scaling or the image's embedded DPI. When `IsZoomToFit` is `true`, the zoom follows the viewport size. Like Windows Photos, images smaller than the viewport are shown at 100% and aren't enlarged. Any manual zoom turns `IsZoomToFit` off. The zoom slider uses a log scale, so each step feels the same at low and high zoom.

## Keyboard

| Gesture | Action |
|---|---|
| `Ctrl + Plus` / `+` | Zoom in |
| `Ctrl + Minus` / `-` | Zoom out |
| `Ctrl + 0` | Fit to window |
| `Ctrl + 1` | Actual size (100%) |
| `Ctrl + R` / `Ctrl + Shift + R` | Rotate clockwise / counter-clockwise |
| Arrow keys | Pan when zoomed in |

The same actions are available as the `RoutedUICommand`s `ZoomInCommand`, `ZoomOutCommand`, `ZoomToFitCommand`, `ActualSizeCommand`, `RotateRightCommand` and `RotateLeftCommand`, which you can bind to your own buttons.

## Properties

| Property | Default | Description |
|---|---|---|
| `ImagePath` | `null` | File path or resource URI to load. |
| `Source` | `null` | The displayed `ImageSource`. |
| `Zoom` | `1.0` | Zoom level (two-way). |
| `MinZoom` / `MaxZoom` | `0.05` / `8.0` | Zoom limits. |
| `IsZoomToFit` | `true` | Keeps the whole image visible as the control resizes (two-way). |
| `Rotation` | `0` | Display rotation, normalized to 0/90/180/270 (two-way). |
| `ShowStatusBar` | `true` | Shows the status bar. |

Read-only: `HasImage`, `PixelWidth`, `PixelHeight`, `FileSize`, `ResolutionText` and `FileSizeText`.

## Events

| Event | Description |
|---|---|
| `ZoomChanged` | Raised when `Zoom` changes (`RoutedPropertyChangedEventArgs<double>`). |
| `ImageLoaded` | Raised after a new image is shown. |
| `ImageFailed` | Raised when `ImagePath` couldn't be loaded; provides `Path` and `Exception`. |
