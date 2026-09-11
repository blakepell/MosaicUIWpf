# LineGraph

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/LineGraph/LineGraph.cs` + `LineGraph.xaml`  
**Example:** `src/MosaicWpfDemo/Views/Examples/LineGraphExample.xaml`

## Description

A lookless line graph that plots one or more `LineGraphSeries` of time stamped values onto a canvas. Values run up the Y-axis and time runs along the X-axis, divided into minutes, hours, days, weeks, months or years (`TimeUnit`, `Auto` by default). Each series has its own colour and caption, shown in a legend beneath the plot, and an optional `Title` sits above the plot and collapses when unset. The graph repaints when it is resized, when the series collection changes, and when any series or point changes (all are `ObservableObject`); repaints are coalesced onto the dispatcher.

All colours come from Mosaic theme tokens (`ControlBackgroundBrush`, `ControlForegroundBrush`, `ControlBorderBrush`, `ControlTextSecondaryForegroundBrush`) through the default style, so it works in light and dark mode with no code. Series without an explicit `Brush` are coloured from a ten colour palette by position.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Series` | `ObservableCollection<LineGraphSeries>` | empty collection | The lines to plot. A usable collection is created in the constructor. |
| `Title` | `string?` | `null` | Title above the plot. Null/blank gives the row back to the plot. |
| `TitleFontSize` | `double` | `16` | Title font size. |
| `TimeUnit` | `LineGraphTimeUnit` | `Auto` | `Auto`, `Minutes`, `Hours`, `Days`, `Weeks`, `Months`, `Years`. |
| `TimeLabelFormat` | `string?` | `null` | `DateTime` format for X labels; null picks one per unit. |
| `ValueLabelFormat` | `string?` | `null` | Numeric format for Y labels/tooltips; null derives precision from the axis step. |
| `IntervalCount` | `int` | `5` | Approximate Y-axis interval count (snapped to round numbers). |
| `MinimumValue` / `MaximumValue` | `double?` | `null` | Fixed Y bounds. Null derives from data (zero based when all values are positive). |
| `LineThickness` | `double` | `2` | Default line thickness; a series can override. |
| `PointRadius` | `double` | `4` | Marker radius. |
| `ShowPoints` / `ShowGridLines` / `ShowLegend` | `bool` | `true` | Toggle markers, horizontal grid lines and the legend. |
| `StrokeBrush` / `StrokeThickness` | `Brush` / `double` | theme border / `1` | Axis and grid line styling. |
| `AxisForeground` | `Brush` | theme secondary text | Axis label brush. |
| `SeriesClickCommand` | `ICommand?` | `null` | Invoked with the clicked `LineGraphSeries`. |
| `PointClickCommand` | `ICommand?` | `null` | Invoked with the clicked `LineGraphPoint`. |

`Background`, `Foreground`, `FontFamily` and `FontSize` style the plot, title and labels and force a repaint when changed.

## Events

| Event | Args | Description |
|---|---|---|
| `SeriesClicked` | `LineGraphSeriesEventArgs` (bubbling) | A line or its legend entry was clicked. `Series` is the backing `LineGraphSeries`. |
| `PointClicked` | `LineGraphPointEventArgs` (bubbling) | A point marker was clicked. Carries `Point` and its `Series`. Does not also raise `SeriesClicked`. |

## Methods

| Member | Description |
|---|---|
| `Redraw()` | Clears the canvas and repaints at the current size. Rarely needed. |

## LineGraphSeries

| Property | Type | Description |
|---|---|---|
| `Caption` | `string` | Legend/tooltip caption. |
| `Brush` | `Brush?` | Line colour; `null` uses the palette. |
| `LineThickness` | `double?` | Per-series thickness; `null` uses the graph's. |
| `Points` | `ObservableCollection<LineGraphPoint>` | The values. Plotted in time order. |
| `Tag` | `object?` | Custom payload. |
| `EffectiveBrush` | `Brush?` | Read only; the brush actually painted. |

Constructors: `LineGraphSeries()`, `LineGraphSeries(string caption, Brush? brush = null)`, `LineGraphSeries(string caption, IEnumerable<LineGraphPoint> points, Brush? brush = null)`. `Add(DateTime, double)` appends a point.

## LineGraphPoint

| Property | Type | Description |
|---|---|---|
| `Time` | `DateTime` | X position. |
| `Value` | `double` | Y position. |
| `Label` | `string?` | Optional tooltip override. |
| `Tag` | `object?` | Custom payload. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:LineGraph
    Height="360"
    Title="Daily Active Users"
    TimeUnit="Days"
    Series="{Binding Regions}"
    SeriesClicked="Graph_SeriesClicked"
    PointClicked="Graph_PointClicked" />
```

```csharp
private void Graph_PointClicked(object sender, LineGraphPointEventArgs e)
{
    ViewModel.Inspect(e.Series.Caption, e.Point.Time, e.Point.Value);
}
```

## Notes

- Template parts: `PART_Canvas` (plot), `PART_Title`, `PART_Legend`. The plot is painted in code onto the canvas; everything else is templatable.
- A fat transparent copy of each line and an oversized transparent disc behind each marker give comfortable click targets. Markers are added last so a point wins over a crossing line.
- X ticks align to natural boundaries (:00/:15/:30, midnight, first of month, 1 January) and overlapping labels are dropped. A zero-width time span is padded by one unit either side.
- Y axis uses "nice number" rounding (1/2/5 × 10ⁿ) so bounds and steps read cleanly; negative values are supported.
- Automation peer: `LineGraphAutomationPeer` (Custom control type; name from `Title`, help text lists the series captions).
- Use [ColumnChart.md](ColumnChart.md) for categorical bars and [PieChart.md](PieChart.md) for part-of-whole data.
