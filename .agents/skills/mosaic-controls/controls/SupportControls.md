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

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:WDScrollViewer IsScrollAnimation="True">
    <!-- scrolling content -->
</mosaic:WDScrollViewer>
```

## Notes

- These controls are mostly referenced from `Themes/Native/*.xaml`.
- `WDScrollViewer` is used by the native `TreeView` template.
- `SliderRepeatButton` is used by the native `Slider` template.
- `SystemDropShadowChrome` is used by native popup/chrome templates.
