# MusicPlayer

**Base class:** `UserControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/MusicPlayer/` (UserControl)  
**Example:** No dedicated example in the demo app.

## Description

A music playback control with track metadata display, play/pause/stop/next/previous controls, a progress slider, and a volume slider. Built on WPF's `MediaElement`.

## Key Properties

Inspect `src/Mosaic.UI.Wpf/Controls/MusicPlayer/` for the full API. Common properties include:

| Property | Type | Description |
|---|---|---|
| Track metadata | `string` | Title, artist, album display properties. |
| Playback state | `bool` / enum | IsPlaying, IsPaused, etc. |
| Volume | `double` | 0.0–1.0 volume level. |
| Position | `TimeSpan` | Current playback position. |
| Duration | `TimeSpan` | Total track duration (read-only). |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:MusicPlayer />
```

## Notes

- `MusicPlayer` is a `UserControl` and cannot be re-templated.
- Built on WPF `MediaElement`; supports formats that the installed Media Foundation codecs support.
- For detailed API information, read the source at `src/Mosaic.UI.Wpf/Controls/MusicPlayer/`.
