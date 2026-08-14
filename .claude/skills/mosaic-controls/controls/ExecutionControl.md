# ExecutionControl

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ExecutionControl/ExecutionControl.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/ExecutionControlExample.xaml`

## Description

A transport control exposing Play, Pause, and Stop buttons suitable for a tool bar. Each button binds to its own command and is automatically disabled (and visually muted with `DisabledBrush`) when that command reports it cannot execute.

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_PlayButton` | `ButtonBase` | Play button. |
| `PART_PauseButton` | `ButtonBase` | Pause button. |
| `PART_StopButton` | `ButtonBase` | Stop button. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `PlayCommand` / `PauseCommand` / `StopCommand` | `ICommand?` | `null` | Command invoked by each button; `CanExecute` drives the button's enabled state. |
| `PlayCommandParameter` / `PauseCommandParameter` / `StopCommandParameter` | `object?` | `null` | Parameters passed to the matching command. |
| `ShowPlayButton` / `ShowPauseButton` / `ShowStopButton` | `bool` | `true` | Whether each button is shown. |
| `PlayToolTip` / `PauseToolTip` / `StopToolTip` | `object?` | `"Play"` / `"Pause"` / `"Stop"` | Per-button tool tips. |
| `IconSize` | `double` | `16` | Rendered width/height of each transport icon. |
| `ButtonPadding` | `Thickness` | `6,3,6,3` | Padding around each icon. |
| `Orientation` | `Orientation` | `Horizontal` | Layout direction of the buttons. |
| `PlayBrush` / `PauseBrush` / `StopBrush` | `Brush?` | `null` (theme) | Icon brushes when enabled. |
| `DisabledBrush` | `Brush?` | `null` (theme) | Icon brush when the matching command cannot execute. |

## Events

| Event | Args | Description |
|---|---|---|
| `PlayClick` | `RoutedEventArgs` (bubbling) | Raised when the play button is clicked. |
| `PauseClick` | `RoutedEventArgs` (bubbling) | Raised when the pause button is clicked. |
| `StopClick` | `RoutedEventArgs` (bubbling) | Raised when the stop button is clicked. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:ExecutionControl
    IconSize="18"
    ShowPauseButton="False"
    PlayCommand="{Binding RunCommand}"
    StopCommand="{Binding CancelCommand}" />
```

## Notes

- The routed events are raised in addition to the commands, so either MVVM or code-behind works; button clicks are marked handled before the routed event bubbles.
- An `ExecutionControlAutomationPeer` exposes the control to UI Automation.
- Used by [SqliteQueryControl](SqliteQueryControl.md) as its run/cancel tool bar.
