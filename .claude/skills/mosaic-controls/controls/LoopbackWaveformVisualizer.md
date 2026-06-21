# LoopbackWaveformVisualizer

**Base class:** `WaveformVisualizerBase` → `FrameworkElement`  
**Namespace:** `Mosaic.UI.Wpf.Controls.WaveformVisualizer`  
**Source:** `src/Mosaic.UI.Wpf/Controls/WaveformVisualizer/LoopbackWaveformVisualizer.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/LoopbackWaveformVisualizerExample.xaml`

## Description

Displays a real-time waveform captured from the default Windows audio **render** (playback) device using WASAPI loopback capture — i.e. it visualizes whatever the system is currently playing. Follows changes to the default console render device automatically.

## Namespace

This control lives in a sub-namespace, so declare it separately:

```xml
xmlns:wave="clr-namespace:Mosaic.UI.Wpf.Controls.WaveformVisualizer;assembly=Mosaic.UI.Wpf"
```

## Key Properties

| Property | Type | Description |
|---|---|---|
| `IsListening` | `bool` | Starts/stops live loopback capture and rendering (from base). |
| `WaveformBrush` | `Brush` | Brush used to draw the waveform (from base). |

## XAML Example

```xml
<wave:LoopbackWaveformVisualizer
    IsListening="True"
    WaveformBrush="DeepSkyBlue" />
```

## Notes

- Captures the default playback device via WASAPI loopback; no device selection is required.
- Automatically follows the default render device when the user changes it.
- For capturing a microphone/input device instead, use [InputWaveformVisualizer](InputWaveformVisualizer.md).
