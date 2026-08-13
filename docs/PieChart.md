# PieChart

A pie chart that paints an observable collection of `PieCategory` values. Slice sizes are calculated relative to the total, so values can be raw counts, amounts, or any other non-negative measure and do not need to total 100.

## Usage

```xml
<mosaic:PieChart
    Height="320"
    Categories="{Binding TrafficSources}"
    LegendPosition="Right"
    SliceClickCommand="{Binding OpenSourceCommand}" />
```

```csharp
public ObservableCollection<PieCategory> TrafficSources { get; } =
[
    new("Search", 58),
    new("Direct", 27),
    new("Referral", 15, Brushes.MediumPurple)
];
```

`Categories` is initialized to an empty collection for each chart. The chart redraws when the collection or any category property changes. Negative values are treated as zero; if the total is zero, no slices are painted.

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Categories` | `ObservableCollection<PieCategory>` | empty collection | Slices and legend entries to display. |
| `StrokeBrush` | `Brush` | white | Separator brush between slices. |
| `StrokeThickness` | `double` | `5` | Separator thickness. |
| `LegendPosition` | `LegendPosition` | `Right` | Places the legend at `Top`, `Left`, `Right`, or `Bottom`. |
| `SliceClickCommand` | `ICommand?` | `null` | Receives the clicked `PieCategory` as its parameter. |

## PieCategory

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Label shown in the legend. |
| `Value` | `double` | Relative slice value. |
| `ColorBrush` | `Brush?` | Explicit slice color; `null` selects a color from the built-in palette. |
| `Tag` | `object?` | Application data associated with the category. |
| `Percentage` | `double` | Read-only percentage computed by the owning chart. |
| `EffectiveBrush` | `Brush?` | Read-only brush actually used after palette fallback. |

`PieCategory` implements `INotifyPropertyChanged`, so editing a category updates the slice and legend without replacing the item.

## Click handling

Use either `SliceClickCommand` or the bubbling `SliceClicked` routed event. Both identify the category represented by the clicked slice.

```csharp
private void Chart_OnSliceClicked(object sender, PieCategoryEventArgs e)
{
    ShowDetails(e.Category);
}
```
