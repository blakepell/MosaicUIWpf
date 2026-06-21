# InputWaveformVisualizer

**Base class:** `WaveformVisualizerBase` → `FrameworkElement`  
**Namespace:** `Mosaic.UI.Wpf.Controls.WaveformVisualizer`  
**Source:** `src/Mosaic.UI.Wpf/Controls/WaveformVisualizer/InputWaveformVisualizer.cs`  
**Example:** No dedicated example in the demo app (see `LoopbackWaveformVisualizerExample`).

## Description

Displays a real-time waveform captured from a selectable Windows audio **input** device using WASAPI shared-mode capture, so other applications can use the device concurrently (e.g. visualizing a microphone while it is in use elsewhere).

## Namespace

This control lives in a sub-namespace, so declare it separately:

```xml
xmlns:wave="clr-namespace:Mosaic.UI.Wpf.Controls.WaveformVisualizer;assembly=Mosaic.UI.Wpf"
```

## Key Properties

| Property | Type | Description |
|---|---|---|
| `SelectedInputDevice` | device | The audio input (capture) device to visualize. |
| `IsListening` | `bool` | Starts/stops live capture and rendering (from base). |
| `WaveformBrush` | `Brush` | Brush used to draw the waveform (from base). |

## XAML Example

```xml
<wave:InputWaveformVisualizer
    IsListening="True"
    WaveformBrush="LimeGreen"
    SelectedInputDevice="{Binding ChosenMicrophone}" />
```

## Notes

- Uses WASAPI **shared-mode** capture (non-exclusive), so the device stays available to other apps.
- Pair with a device picker bound to `SelectedInputDevice`.
- For capturing system/playback audio instead, use [LoopbackWaveformVisualizer](LoopbackWaveformVisualizer.md).
