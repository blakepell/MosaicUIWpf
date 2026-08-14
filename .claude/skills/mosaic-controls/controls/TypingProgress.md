# TypingProgress

**Base class:** `UserControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/Chat/TypingProgress.xaml(.cs)`  
**Example:** `src/MosaicWpfDemo/Views/Examples/TypingProgressExample.xaml`

## Description

The animated three-dot "typing…" indicator used in chat and messaging UIs. Three bubbles pulse their opacity in sequence while `IsRunning` is true; the control hides itself (`Visibility.Hidden`, so it holds its layout space) when it is false.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `IsRunning` | `bool` | `false` | Whether the animation runs. `true` makes the control visible and starts the storyboard; `false` hides it and stops it. |

## Methods

| Member | Description |
|---|---|
| `Start()` | Sets `IsRunning` to `true`. |
| `Stop()` | Sets `IsRunning` to `false`. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:TypingProgress IsRunning="{Binding IsAssistantTyping}" />
```

## Notes

- The storyboard is resolved from the control's resources (`TypingStoryboard`) on `Loaded` and removed on `Unloaded`, so the control does not keep an animation clock alive after it leaves the tree.
- Pairs naturally with [ChatThread](ChatThread.md), which lives in the same `Controls/Chat` folder.
