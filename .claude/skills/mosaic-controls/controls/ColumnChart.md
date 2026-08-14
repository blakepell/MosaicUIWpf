# ColumnChart

**Base class:** `UserControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ColumnChart/ColumnChart.xaml(.cs)`  
**Example:** `src/MosaicWpfDemo/Views/Examples/ColumnChartExample.xaml`

## Description

A responsive column (bar) chart that paints an `ObservableCollection<ColumnChartItem>` onto a canvas. It repaints when the control resizes, when the collection changes, and when any property of any item changes (items are `ObservableObject`). Repaints are coalesced onto the dispatcher, so a bulk update produces one paint.

Colors default to Mosaic theme tokens (`ControlBackgroundBrush`, `ControlForegroundBrush`, `AccentBrush`, `ControlBorderBrush`) via resource references, so the chart follows theme switches with no code.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Items` | `ObservableCollection<ColumnChartItem>` | empty collection | The columns to display. A usable collection is created in the constructor. |
| `ColumnBrush` | `Brush` | theme accent | Fill for columns that do not set their own `ColumnBrush`. |
| `StrokeBrush` | `Brush` | theme border | Axis and grid line brush. |
| `StrokeThickness` | `double` | `1` | Axis and grid line thickness. |
| `IntervalCount` | `int` | `8` | Number of Y-axis intervals. |
| `InnerPadding` | `Thickness` | `100` | Space reserved around the plot for axis labels and column names. |
| `ColumnClickCommand` | `ICommand` | `null` | Invoked when a column is clicked; the clicked `ColumnChartItem` is the parameter. |

`Foreground`, `FontFamily`, and `FontSize` are used for the axis labels and column names and force a repaint when changed.

## Events

| Event | Args | Description |
|---|---|---|
| `ColumnClicked` | `ColumnChartItemEventArgs` (bubbling) | Raised when a column is clicked. `Item` is the backing `ColumnChartItem`. |

## Methods

| Member | Description |
|---|---|
| `Redraw()` | Clears the canvas and repaints at the current size. Rarely needed — changes repaint automatically. |

## ColumnChartItem

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Label rendered beneath the column on the X-axis. |
| `Value` | `int` | The column value; the largest value across items sets the Y-axis scale. |
| `ColumnBrush` | `Brush?` | Per-column fill. `null` falls back to the chart's `ColumnBrush`. |
| `Tag` | `object?` | Custom payload. |

Constructors: `ColumnChartItem()` and `ColumnChartItem(string name, int value, Brush? columnBrush = null)`.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:ColumnChart
    Height="320"
    IntervalCount="5"
    InnerPadding="60,20,20,40"
    Items="{Binding Sales}"
    ColumnClicked="ColumnChart_ColumnClicked" />
```

```csharp
private void ColumnChart_ColumnClicked(object sender, ColumnChartItemEventArgs e)
{
    ViewModel.Drill(e.Item.Name);
}
```

## Notes

- A transparent hit strip spans the full plot height behind each column, so even a zero-valued column is clickable.
- Each column gets a tooltip of `Name: Value` automatically.
- Values ≤ 0 across all items fall back to one unit per interval rather than dividing by zero.
- Use [PieChart.md](PieChart.md) for part-of-whole data.
