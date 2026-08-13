# ColumnChart

A responsive column chart that paints an observable collection of `ColumnChartItem` values. It redraws when its size, collection, or an item property changes, and its default brushes follow the active Mosaic theme.

## Usage

```xml
<mosaic:ColumnChart
    Height="300"
    Items="{Binding Sales}"
    ColumnClickCommand="{Binding OpenSalesCommand}"
    IntervalCount="5"
    InnerPadding="60,30,30,45" />
```

```csharp
public ObservableCollection<ColumnChartItem> Sales { get; } =
[
    new("January", 42),
    new("February", 68),
    new("March", 55, Brushes.OrangeRed)
];
```

`Items` is initialized to an empty collection for each chart, so items can also be added directly. Values at or below zero render with zero height. The largest positive value determines the Y-axis scale, rounded up to the next interval.

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Items` | `ObservableCollection<ColumnChartItem>` | empty collection | Columns to display. Collection and item changes repaint automatically. |
| `ColumnBrush` | `Brush` | theme accent | Fill for items whose `ColumnBrush` is `null`. |
| `StrokeBrush` | `Brush` | theme border | Axis and grid-line brush. |
| `StrokeThickness` | `double` | `1` | Axis and grid-line thickness. |
| `IntervalCount` | `int` | `8` | Number of Y-axis intervals. Values at or below zero prevent the chart from painting. |
| `InnerPadding` | `Thickness` | `100` | Space reserved around the plot for labels and column names. |
| `ColumnClickCommand` | `ICommand?` | `null` | Receives the clicked `ColumnChartItem` as its parameter. |

The inherited `Background`, `Foreground`, `FontFamily`, and `FontSize` properties style the plot and labels.

## ColumnChartItem

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | X-axis label. |
| `Value` | `int` | Column value. |
| `ColumnBrush` | `Brush?` | Per-column fill; `null` uses the chart brush. |
| `Tag` | `object?` | Application data associated with the item. |

`ColumnChartItem` implements `INotifyPropertyChanged`, so editing any of these properties updates the chart without replacing the item.

## Click handling

Use either the MVVM command or the bubbling `ColumnClicked` routed event. Both receive the item represented by the clicked column.

```csharp
private void Chart_OnColumnClicked(object sender, ColumnChartItemEventArgs e)
{
    ShowDetails(e.Item);
}
```
