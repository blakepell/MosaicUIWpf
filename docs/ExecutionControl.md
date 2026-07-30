# ExecutionControl

A transport style control that exposes Play, Pause and Stop buttons suitable for a tool bar. Each button binds to its own command and is automatically disabled — and visually muted with the theme's disabled brush — when that command reports it cannot execute.

```xml
<ToolBar>
    <mosaic:ExecutionControl
        PlayCommand="{Binding PlayCommand}"
        PauseCommand="{Binding PauseCommand}"
        StopCommand="{Binding StopCommand}" />
</ToolBar>
```

| Property | Description |
|---|---|
| `PlayCommand`, `PauseCommand`, `StopCommand` | The command invoked by each button. `CanExecute` drives the button's enabled/muted state. |
| `PlayCommandParameter`, `PauseCommandParameter`, `StopCommandParameter` | Optional parameter passed to the matching command. |
| `ShowPlayButton`, `ShowPauseButton`, `ShowStopButton` | Collapses an individual button when set to `False`. |
| `PlayToolTip`, `PauseToolTip`, `StopToolTip` | Tool tip content for each button (shown even while disabled). |
| `PlayBrush`, `PauseBrush`, `StopBrush` | Icon brushes; default to the theme's success, control foreground and error brushes. |
| `DisabledBrush` | Icon brush used when a command cannot execute; defaults to the theme's disabled foreground brush. |
| `IconSize` | Rendered width and height of each icon (default `16`). |
| `ButtonPadding` | Padding around each icon. |
| `Orientation` | Lays the buttons out horizontally (default) or vertically. |

The `PlayClick`, `PauseClick` and `StopClick` routed events are raised alongside the commands for code-behind scenarios.
