# ScalingTextBlock

**Base class:** `TextBlock`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ScalingTextBlock/ScalingTextBlock.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/ScalingTextBlockExample.xaml`

## Description

A `TextBlock` that automatically scales its font size so all text fits within the available space. `MinFontSize` and `MaxFontSize` serve as the lower and upper boundaries; `FontScaleIncrement` controls the step granularity used while fitting.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `MinFontSize` | `double` | Smallest font size the control will scale down to. |
| `MaxFontSize` | `double` | Largest font size the control will scale up to. |
| `FontScaleIncrement` | `double` | Step size used when searching for the best-fit font size. |

Standard `TextBlock` members apply (`Text`, `TextWrapping`, `Foreground`, etc.).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:ScalingTextBlock
    Text="{Binding Headline}"
    MinFontSize="10"
    MaxFontSize="48"
    TextWrapping="Wrap" />
```

## Notes

- Useful for headlines/labels in resizable containers where text must always fit.
- The control recomputes the best-fit size as its bounds or text change.
