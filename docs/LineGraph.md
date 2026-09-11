# LineGraph

A line graph that plots one or more `LineGraphSeries` of time stamped values. Values run up the Y-axis, time runs along the X-axis in minutes, hours, days, weeks, months or years, and each series has its own colour and caption shown in a legend beneath the plot. An optional title sits above the plot and gives its space back when not set. The graph redraws when its size, the series collection, a series, or a point changes, and its default brushes follow the active Mosaic theme in light and dark mode.

## Usage

```xml
<mosaic:LineGraph
    Height="360"
    Title="Daily Active Users by Region"
    Series="{Binding Regions}"
    SeriesClicked="Graph_OnSeriesClicked"
    PointClicked="Graph_OnPointClicked" />
```

```csharp
public ObservableCollection<LineGraphSeries> Regions { get; } = new()
{
    new LineGraphSeries("North America")
    {
        Points =
        {
            new(new DateTime(2026, 1, 1), 512),
            new(new DateTime(2026, 1, 2), 498),
            new(new DateTime(2026, 1, 3), 540)
        }
    },
    new LineGraphSeries("Europe", Brushes.OrangeRed)
};

Regions[1].Add(new DateTime(2026, 1, 1), 310);
```

`Series` is initialized to an empty collection for each graph, so series can also be added directly. Points are plotted in time order regardless of the order they were added. A series that leaves `Brush` null is coloured from the graph's palette by its position in the collection.

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Series` | `ObservableCollection<LineGraphSeries>` | empty collection | Lines to plot. Collection, series and point changes repaint automatically. |
| `Title` | `string?` | `null` | Title shown above the plot. Null or blank collapses the title row. |
| `TitleFontSize` | `double` | `16` | Font size of the title. |
| `TimeUnit` | `LineGraphTimeUnit` | `Auto` | Unit the X-axis is divided into: `Auto`, `Minutes`, `Hours`, `Days`, `Weeks`, `Months` or `Years`. `Auto` picks one from the span of the data. |
| `TimeLabelFormat` | `string?` | `null` | `DateTime` format for X-axis labels. Null picks a format to suit the unit (`HH:mm`, `MMM d`, `MMM yyyy`, `yyyy`). |
| `ValueLabelFormat` | `string?` | `null` | Numeric format for Y-axis labels and tooltips. Null derives the precision from the axis step. |
| `IntervalCount` | `int` | `5` | Approximate number of Y-axis intervals. The axis snaps to round numbers, so the count may differ slightly. |
| `MinimumValue` | `double?` | `null` | Fixed lower bound for the Y-axis. Null starts at zero, or at the lowest value when any value is negative. |
| `MaximumValue` | `double?` | `null` | Fixed upper bound for the Y-axis. Null rounds the largest value up. |
| `LineThickness` | `double` | `2` | Thickness of each line. A series can override this. |
| `PointRadius` | `double` | `4` | Radius of the marker drawn at each point. |
| `ShowPoints` | `bool` | `true` | Whether point markers are drawn. Markers are the click target for `PointClicked`. |
| `ShowGridLines` | `bool` | `true` | Whether horizontal grid lines are drawn at each Y-axis interval. |
| `ShowLegend` | `bool` | `true` | Whether the legend of captions is shown beneath the plot. |
| `StrokeBrush` | `Brush` | theme border | Axis and grid-line brush. |
| `StrokeThickness` | `double` | `1` | Axis and grid-line thickness. |
| `AxisForeground` | `Brush` | theme secondary text | Brush for the axis labels. |
| `SeriesClickCommand` | `ICommand?` | `null` | Receives the clicked `LineGraphSeries` as its parameter. |
| `PointClickCommand` | `ICommand?` | `null` | Receives the clicked `LineGraphPoint` as its parameter. |

The inherited `Background`, `Foreground`, `FontFamily` and `FontSize` properties style the plot, title and labels.

## LineGraphSeries

| Property | Type | Description |
|---|---|---|
| `Caption` | `string` | Caption shown in the legend and tooltips. |
| `Brush` | `Brush?` | Colour of the line and its markers; `null` uses the graph palette. |
| `LineThickness` | `double?` | Per-series thickness; `null` uses the graph's `LineThickness`. |
| `Points` | `ObservableCollection<LineGraphPoint>` | The time stamped values. |
| `Tag` | `object?` | Application data associated with the series. |
| `EffectiveBrush` | `Brush?` | Read only. The brush actually used, whether explicit or from the palette. |

`Add(DateTime time, double value)` appends a point and returns it.

## LineGraphPoint

| Property | Type | Description |
|---|---|---|
| `Time` | `DateTime` | Position along the X-axis. |
| `Value` | `double` | Position along the Y-axis. |
| `Label` | `string?` | Optional tooltip text in place of the formatted time and value. |
| `Tag` | `object?` | Application data associated with the point. |

Both `LineGraphSeries` and `LineGraphPoint` implement `INotifyPropertyChanged`, so editing any property updates the graph without replacing the object.

## Click handling

Clicking a line, or its entry in the legend, raises the bubbling `SeriesClicked` event with the series. Clicking a point marker raises `PointClicked` with the point and the series it belongs to; a point click does not also raise `SeriesClicked`. Both events have MVVM command equivalents.

```csharp
private void Graph_OnSeriesClicked(object sender, LineGraphSeriesEventArgs e)
{
    ShowSeries(e.Series);
}

private void Graph_OnPointClicked(object sender, LineGraphPointEventArgs e)
{
    ShowReading(e.Series.Caption, e.Point.Time, e.Point.Value);
}
```

## Notes

- Every point gets a tooltip of the series caption, time and value unless `Label` is set.
- X-axis ticks land on natural boundaries (for example :00/:15/:30/:45 for minutes, the first of the month for months) and labels that would overlap are dropped.
- A single point, or a set of points at one instant, is padded by one time unit either side so it still has somewhere to sit.
- Redraws are coalesced onto the dispatcher, so a bulk update produces one paint. `Redraw()` is available but rarely needed.
