# screenshot — desktop captures

[Table of contents](README.md)

Use `screenshot` to capture visible desktop pixels. Each call returns a .NET Bitmap that you own. Call `Dispose()` after saving or otherwise using it, preferably in `finally`.

## Reference

| Call | Capture |
| --- | --- |
| `screenshot.CurrentWindow()` | Desktop rectangle occupied by the foreground window at the time of capture. |
| `screenshot.PrimaryScreen()` | The primary screen. |
| `screenshot.ByLocation(x, y, width, height)` | A desktop rectangle in physical pixels. |

All four rectangle arguments are integers. Width and height must be positive. Coordinates can be negative for monitors to the left of or above the primary screen. Captures read the displayed screen; they do not render a hidden window's contents or combine all monitors automatically.

`CurrentWindow` captures whichever window is foreground, which might be the script editor. Failures to find/capture a window or screen throw errors.

## Save the primary screen as PNG

This example writes a new image to your Documents folder. It uses a unique filename and imports the image-format type needed by `Save`.

```javascript
let folder = environ.DocumentsFolder();
if (folder != "") {
    let path = environ.PathCombine(folder, "capture-" + guid.NewGuid().ToString() + ".png");
    let bitmap = screenshot.PrimaryScreen();
    try {
        include System.Drawing.Imaging;
        bitmap.Save(path, ImageFormat.Png);
        log.Success("Saved " + path);
    } finally {
        bitmap.Dispose();
    }
}
```

Use an explicit image format when saving. A filename extension alone is not an instruction to encode that format. Saving requires an existing, writable destination directory.

## Inspect a region

Choose a rectangle that lies on your desktop before running the example:

```javascript
let bitmap = screenshot.ByLocation(0, 0, 320, 200);
try {
    log.Info("Captured " + bitmap.Width + " x " + bitmap.Height + " pixels.");
} finally {
    bitmap.Dispose();
}
```

The returned Bitmap is a .NET image, not a filename, JavaScript byte array, or WPF image control. Do not use it after disposal.
