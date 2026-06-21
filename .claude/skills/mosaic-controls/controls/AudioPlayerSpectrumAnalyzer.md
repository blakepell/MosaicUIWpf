# AudioPlayerSpectrumAnalyzer

**Base class:** `FrameworkElement`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/AudioPlayer/AudioPlayerSpectrumAnalyzer.cs`  
**Example:** No dedicated example (used with `AudioPlayer`).

## Description

A real-time spectrum analyzer designed to pair with the `AudioPlayer` control. Renders frequency bands on the horizontal axis and signal strength on the vertical axis, with optional peak-hold indicators and amplitude-driven color intensity.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `IsActive` | `bool` | Enables/disables the live analysis rendering. |
| `BandCount` | `int` | Number of frequency bands to display. |
| `Background` | `Brush` | Background fill behind the bars. |
| `BarBrush` | `Brush` | Brush used to paint the frequency bars. |
| `IntensityBrush` | `Brush` | Brush blended in when `UseColorIntensity` is on. |
| `UseColorIntensity` | `bool` | Drives bar color by amplitude. |
| `ShowPeaks` | `bool` | Shows peak-hold indicators above bars. |
| `PeakBrush` | `Brush` | Brush for peak-hold markers. |
| `BarSpacing` | `double` | Gap between bars. |
| `Sensitivity` | `double` | Amplitude sensitivity multiplier. |
| `CornerRadius` | `CornerRadius` | Corner radius of bars/surface. |

## Events

`OnError` (`EventHandler<Exception>`) — raised when the audio analysis pipeline fails.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:AudioPlayerSpectrumAnalyzer
    IsActive="True"
    BandCount="32"
    ShowPeaks="True"
    UseColorIntensity="True" />
```

## Notes

- Intended to be wired to an `AudioPlayer` instance for live audio data.
- A `FrameworkElement` (not a templated control) — it draws directly via `OnRender`.
