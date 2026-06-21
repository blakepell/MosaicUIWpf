# SplitPanel

**Base class:** `Control` (CustomControl)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/SplitPanel/SplitPanel.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/SplitPanelExample.xaml`

## Description

A two-pane container whose panes are separated by a draggable `GridSplitter`. The proportion of space allocated to the first pane is controlled by the two-way `SplitterPosition` property (0.0–1.0). Supports both vertical (top/bottom) and horizontal (side-by-side) orientation.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Pane1` | `object` | `null` | Content of the first pane. |
| `Pane2` | `object` | `null` | Content of the second pane. |
| `SplitterPosition` | `double` | — | Fraction (0.0–1.0) of space given to `Pane1` (two-way). |
| `Orientation` | `Orientation` | — | `Horizontal` (side-by-side) or `Vertical` (top/bottom). |
| `SplitterThickness` | `double` | — | Thickness of the draggable splitter. |
| `MinPaneSize` | `double` | — | Minimum size for either pane. |

## Events

`SplitterPositionChanged` (`RoutedPropertyChangedEventHandler<double>`).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:SplitPanel Orientation="Horizontal" SplitterPosition="0.3" MinPaneSize="120">
    <mosaic:SplitPanel.Pane1>
        <TextBlock Text="Left / navigation" />
    </mosaic:SplitPanel.Pane1>
    <mosaic:SplitPanel.Pane2>
        <TextBlock Text="Right / content" />
    </mosaic:SplitPanel.Pane2>
</mosaic:SplitPanel>
```

## Notes

- Bind `SplitterPosition` two-way to persist the user's chosen split.
- `MinPaneSize` prevents either pane from collapsing entirely while dragging.
