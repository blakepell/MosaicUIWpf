# AudioPlayer

**Base class:** `Control` (CustomControl)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/AudioPlayer/AudioPlayer.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/AudioPlayerExample.xaml`

## Description

An audio player with a familiar transport layout: a centered Previous / Play-Stop / Next button row above a full-width seek slider flanked by current playback time and total track length. Backed by WPF `MediaPlayer` and manages an internal playlist. Pairs with `AudioPlayerSpectrumAnalyzer` for visualization.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `Source` | `Uri` / `string` | The current audio source. |
| `Playlist` | collection | Internal/bindable playlist of tracks. |
| `CurrentIndex` | `int` | Index of the active track in the playlist. |
| `Position` | `TimeSpan` | Current playback position (seekable). |
| `Duration` | `TimeSpan` | Total track duration (read-only). |
| `Volume` | `double` | Volume level (0.0–1.0). |
| `IsPlaying` | `bool` | Whether playback is active (read-only). |
| `AutoPlay` | `bool` | Begin playback automatically when a source loads. |
| `AutoAdvance` | `bool` | Advance to the next playlist track when one ends. |
| `CornerRadius` | `CornerRadius` | Corner radius of the control surface. |

## Events

`PlaybackStarted`, `PlaybackStopped`, `MediaOpened`, `MediaEnded`, `TrackChanged` (all routed events).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:AudioPlayer Source="C:\Music\track.mp3" AutoPlay="True" Volume="0.8" />
```

## Notes

- Lookless `Control` — re-templatable, unlike the older `UserControl`-based player.
- Built on WPF `MediaPlayer`; supports formats provided by installed Media Foundation codecs.
- Use `AudioPlayerSpectrumAnalyzer` alongside it for a real-time frequency display.
