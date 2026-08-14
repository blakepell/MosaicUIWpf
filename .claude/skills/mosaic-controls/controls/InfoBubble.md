# InfoBubble

**Base class:** `ContentControl` (sealed)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/InfoBubble/InfoBubble.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/InfoBubbleExample.xaml`

## Description

Wraps arbitrary content (a button, an icon, an avatar) and overlays a count / status / notification indicator on it. The indicator can be a numeric badge with an overflow cap (`99+`), an arbitrary templated payload, or a plain dot.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Content` | `object` | `null` | The content the indicator is overlaid on (this is the XAML content property). |
| `InfoBubbleContent` | `object?` | `null` | The content displayed inside the indicator. |
| `InfoBubbleContentTemplate` | `DataTemplate?` | `null` | Template for the indicator content. |
| `InfoBubblePlacementMode` | `InfoBubblePlacementMode` | `TopRight` | `TopLeft`, `Top`, `TopRight`, `Right`, `BottomRight`, `Bottom`, `BottomLeft`, `Left`. |
| `InfoBubbleMargin` | `Thickness` | `0` | Fine-tunes the indicator position; negative values push it outside the content bounds. |
| `IsInfoBubbleVisible` | `bool` | `true` | Whether the indicator is shown at all. |
| `IsDot` | `bool` | `false` | Render as a bare dot with no content. |
| `InfoBubbleMaxValue` | `int` | `0` | Largest numeric value shown as-is; above it the indicator shows `{Max}+`. `0` disables truncation. |
| `HideWhenZero` | `bool` | `false` | Hide the indicator when its numeric content is zero. |
| `InfoBubbleBackground` / `InfoBubbleForeground` / `InfoBubbleBorderBrush` | `Brush?` | `null` (theme) | Indicator brushes. |
| `InfoBubbleBorderThickness` | `Thickness` | `0` | Indicator border thickness. |
| `InfoBubbleCornerRadius` | `CornerRadius` | `8` | Indicator corner radius. |
| `InfoBubblePadding` | `Thickness` | `4,2,4,2` | Padding around the indicator content. |
| `InfoBubbleMinWidth` / `InfoBubbleMinHeight` | `double` | `16` | Minimum indicator size. |
| `InfoBubbleFontFamily` / `InfoBubbleFontSize` / `InfoBubbleFontWeight` | — | Segoe UI / `10` / `Normal` | Indicator typography. |

### Read-only (template) properties

| Property | Type | Description |
|---|---|---|
| `DisplayInfoBubbleContent` | `object?` | The content after dot and max-value rules are applied. |
| `ComputedInfoBubbleVisibility` | `bool` | Whether every visibility condition is satisfied. |
| `InfoBubbleOverhangMargin` | `Thickness` | Space reserved around content for indicator overhang (from the negative parts of `InfoBubbleMargin`). |
| `InfoBubbleIndicatorMargin` | `Thickness` | Inward offset (from the non-negative parts of `InfoBubbleMargin`). |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:InfoBubble
    InfoBubbleContent="{Binding UnreadCount}"
    InfoBubbleMaxValue="99"
    HideWhenZero="True"
    InfoBubblePlacementMode="TopRight"
    InfoBubbleMargin="-6,-6,0,0">
    <Button Content="Inbox" Padding="12,6" />
</mosaic:InfoBubble>
```

Dot form:

```xml
<mosaic:InfoBubble IsDot="True" InfoBubblePlacementMode="TopRight">
    <mosaic:Avatar ImageSource="{Binding Photo}" />
</mosaic:InfoBubble>
```

## Notes

- Numeric content is parsed from `int`, `long`, `double`, or a numeric `string`, so `HideWhenZero` and `InfoBubbleMaxValue` work with string bindings too.
- An indicator with null or empty-string content is hidden regardless of `IsInfoBubbleVisible`.
- Adapted from atc-wpf (MIT).
- For a standalone (non-overlay) badge, use [Badge.md](Badge.md); for a label/value pair, see [Shield.md](Shield.md).
