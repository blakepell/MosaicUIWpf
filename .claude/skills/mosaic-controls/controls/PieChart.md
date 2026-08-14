# PieChart

**Base class:** `UserControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/PieChart/PieChart.xaml(.cs)`  
**Example:** `src/MosaicWpfDemo/Views/Examples/PieChartExample.xaml`

## Description

A pie chart that paints an `ObservableCollection<PieCategory>` onto a canvas with a built-in legend. Each slice is sized by its `Value` relative to the total of all values, so values do not have to add up to 100. It repaints on resize, on collection changes, and when any category property changes; repaints are coalesced onto the dispatcher.

Categories that leave `ColorBrush` null are painted from a built-in ten-color palette based on their index.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Categories` | `ObservableCollection<PieCategory>` | empty collection | The slices to display. A usable collection is created in the constructor. |
| `StrokeBrush` | `Brush` | `White` | Brush for the lines separating slices. |
| `StrokeThickness` | `double` | `5` | Thickness of the slice separator lines. |
| `LegendPosition` | `LegendPosition` | `Right` | `Top`, `Left`, `Right`, or `Bottom`. |
| `SliceClickCommand` | `ICommand` | `null` | Invoked when a slice is clicked; the clicked `PieCategory` is the parameter. |

## Events

| Event | Args | Description |
|---|---|---|
| `SliceClicked` | `PieCategoryEventArgs` (bubbling) | Raised when a slice is clicked. `Category` is the backing `PieCategory`. |

## Methods

| Member | Description |
|---|---|
| `Redraw()` | Repositions the legend, clears the canvas, and repaints at the current size. |

## PieCategory

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Label shown in the legend. |
| `Value` | `double` | Slice value, relative to the total of every value in the chart. |
| `ColorBrush` | `Brush?` | Explicit slice fill. `null` lets the chart assign a palette color. |
| `Tag` | `object?` | Custom payload. |
| `Percentage` | `double` (read-only) | Share of the pie 0–100, computed by the owning chart. |
| `EffectiveBrush` | `Brush?` (read-only) | The brush actually painted (`ColorBrush` or the palette color). The legend swatch binds to this. |

Constructors: `PieCategory()` and `PieCategory(string name, double value, Brush? colorBrush = null)`.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:PieChart
    Height="300"
    LegendPosition="Right"
    StrokeThickness="3"
    Categories="{Binding Breakdown}"
    SliceClicked="PieChart_SliceClicked" />
```

```csharp
private void PieChart_SliceClicked(object sender, PieCategoryEventArgs e)
{
    ViewModel.Select(e.Category.Name);
}
```

## Notes

- A single category at 100% is drawn as a full circle rather than a swept arc.
- Zero-valued categories draw nothing but still appear in the legend.
- Each slice gets a tooltip of `Name: Value (Percentage%)` automatically.
- `Percentage` and `EffectiveBrush` are written by the paint itself and deliberately do not trigger another repaint.
- Use [ColumnChart.md](ColumnChart.md) for magnitude comparisons rather than part-of-whole.
