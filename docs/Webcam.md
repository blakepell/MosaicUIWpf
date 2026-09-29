# Webcam

Displays a live webcam feed captured through Windows Media Foundation. No third-party video libraries are used.

```xml
<mosaic:Webcam x:Name="Webcam" Stretch="Uniform" Mirror="True" />
```

```csharp
IReadOnlyList<WebcamDevice> devices = WebcamDeviceManager.GetDevices();
Webcam.Device = devices.FirstOrDefault();

await Webcam.StartAsync();                                  // throws WebcamException on failure (also raised via Error)
BitmapSource? snapshot = await Webcam.CaptureFrameAsync();  // frozen, independent copy; camera keeps running
await Webcam.StopAsync();                                   // device is free for other apps when this completes
```

## Key members

| Member | Notes |
|---|---|
| `Device` | The camera to show. Changing it while running switches cameras; rapid changes are coalesced. `WebcamDevice` equality uses the symbolic link, so it can be persisted in settings. |
| `Resolution` / `AvailableResolutions` / `ActualResolution` | `null` selects the best mode near 1920 × 1080 @ 30 fps. `AvailableResolutions` is filled when the camera starts; `WebcamDeviceManager.GetResolutionsAsync` queries a device that is not running. |
| `Stretch`, `Mirror` | Mirroring is a render transform; frame data and snapshots are never flipped. |
| `AutoStart` | Starts when loaded and a device is set. |
| `State`, `IsRunning` | `Stopped`, `Starting`, `Running`, `Stopping`, `Error`. `StateChanged` is a bubbling routed event. |
| `Started`, `Stopped`, `Error`, `FrameReceived` | `Error` carries a `WebcamErrorKind` (in use, access denied, disconnected, unsupported format, ...). `FrameReceived` costs nothing without subscribers and exposes pixels via `CopyPixels` during the handler. |
| `StartCommand`, `StopCommand` | Routed commands for XAML-only wiring. |

## Behavior notes

- Frames are read on a dedicated background thread. The UI always shows the newest frame and drops stale ones instead of queueing them.
- The camera is released when the control is unloaded (for example when its window closes) and resumed if it is loaded again while still started.
- In the XAML designer the control shows a placeholder and never opens a device.
