# Support Controls

These are public WPF controls used mainly by Mosaic native style dictionaries and low-level templates. They are available from `Mosaic.UI.Wpf.Controls`, but most application code should use the higher-level controls in the main inventory.

## WDScrollViewer

**Base class:** `ScrollViewer`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ScrollViewer/WDScrollViewer.cs`

`WDScrollViewer` adds optional animated mouse-wheel scrolling.

| Property | Type | Default | Description |
|---|---|---|---|
| `IsScrollAnimation` | `bool` | `false` | Enables animated wheel scrolling. |

| Method | Description |
|---|---|
| `AnimateScroll(double toValue, Action onCompleted = null)` | Animates vertical scroll offset to `toValue` over 800 ms. |

### ScrollViewerBehavior (attached helper)

**Namespace:** `Mosaic.UI.Wpf.Controls` (note: not `Mosaic.UI.Wpf.Behaviors`)  
**Source:** `src/Mosaic.UI.Wpf/Controls/ScrollViewer/ScrollViewerBehavior.cs`

A static class exposing one attached property that lets a `WDScrollViewer`'s vertical offset be set, bound, or animated from XAML (`ScrollViewer.VerticalOffset` itself is read-only).

| Attached property | Type | Default | Description |
|---|---|---|---|
| `VerticalOffset` | `double` | `0.0` | Scrolls the target `WDScrollViewer` to this offset when it changes. Has no effect on other element types. |

```xml
<mosaic:WDScrollViewer mosaic:ScrollViewerBehavior.VerticalOffset="{Binding Offset}" />
```

## SliderRepeatButton

**Base class:** `RepeatButton`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/SliderRepeatButton/SliderRepeatButton.cs`

`SliderRepeatButton` is used by native slider templates to expose radius/orientation metadata.

| Property | Type | Default | Description |
|---|---|---|---|
| `RadiusOrientation` | `RadiusOrientation` | `null` metadata | Indicates which side or corner should receive radius treatment. |

`RadiusOrientation` values: `Down`, `Up`, `Left`, `Right`, `TopLeft`, `TopRight`, `BottomRight`, and `BottomLeft`.

## SystemDropShadowChrome

**Base class:** `Decorator`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/SystemDropShadowChrome/SystemDropShadowChrome.cs`

`SystemDropShadowChrome` renders a WPF-style shadow silhouette around its child content. It is used in native menu, toolbar, and combo box templates.

| Property | Type | Default | Description |
|---|---|---|---|
| `Color` | `Color` | `#71000000` | Shadow color. |
| `CornerRadius` | `CornerRadius` | `0` | Corner radius used when rendering shadow corners. |

## WDBorder

**Base class:** `Border`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ClipBorder/WDBorder.cs`

On every render, `WDBorder` works out a clip geometry for its inner area (its size minus `BorderThickness`, with the rounded `CornerRadius` corners). It publishes that geometry as a read-only property that child elements can bind their `Clip` to.

| Property | Type | Default | Description |
|---|---|---|---|
| `ContentClip` | `Geometry` (read-only) | `null` | Frozen `StreamGeometry` of the inner rounded rectangle, or `null` when the border has no size. |

```xml
<mosaic:WDBorder CornerRadius="6" BorderThickness="1">
    <ScrollViewer Clip="{Binding ContentClip, RelativeSource={RelativeSource AncestorType=mosaic:WDBorder}}" />
</mosaic:WDBorder>
```

For general child clipping, prefer [ClipBorder.md](ClipBorder.md), which clips automatically.

## DatePicker primitives

**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/DatePicker/`

These are public classes that add no members of their own. They exist so the `DatePicker` template can give its parts dedicated default styles.

| Class | Base Class | Used as |
|---|---|---|
| `CalendarBox` | `ListBox` | `PART_ListBox`, the month day grid |
| `CalendarSwitch` | `ToggleButton` | `PART_Switch`, which opens the popup |
| `ChevronButton` | `Button` | `PART_Left` / `PART_Right`, the month navigation buttons |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:WDScrollViewer IsScrollAnimation="True">
    <!-- scrolling content -->
</mosaic:WDScrollViewer>
```

## Notes

- These controls are mostly referenced from `Themes/Native/*.xaml`.
- `WDScrollViewer` and `WDBorder` are used by the native `TreeView` template.
- `CalendarBox`, `CalendarSwitch`, and `ChevronButton` are used only by the `DatePicker` template.
- `SliderRepeatButton` is used by the native `Slider` template.
- `SystemDropShadowChrome` is used by native popup/chrome templates.
