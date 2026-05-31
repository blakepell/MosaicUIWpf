# InertiaScrollViewer

**Base class:** `ScrollViewer`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/InertiaScrollViewer/InertiaScrollViewer.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/InertiaScrollViewerExample.xaml`

## Description

Extends the standard `ScrollViewer` to add smooth animated (inertia/momentum) scrolling on mouse wheel. When `IsScrollAnimation` is `true`, each wheel event triggers a cubic-eased `DoubleAnimation` instead of an instant jump. The companion `InertiaScrollViewerBehavior` provides the `VerticalOffset` attached property used by the animation.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `IsScrollAnimation` | `bool` | `false` | Enables the animated inertia scrolling. |
| `AnimationDurationMilliseconds` | `int` | `800` | Duration of each scroll animation in ms. |
| `DesiredFrameRate` | `int` | `40` | Frame rate cap for the animation. |

All standard `ScrollViewer` properties apply (`HorizontalScrollBarVisibility`, `VerticalScrollBarVisibility`, `CanContentScroll`, etc.).

## Methods

| Method | Description |
|---|---|
| `AnimateScroll(double toValue, Action? onCompleted = null)` | Programmatically animates to a vertical offset. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:InertiaScrollViewer
    IsScrollAnimation="True"
    AnimationDurationMilliseconds="600"
    VerticalScrollBarVisibility="Auto">
    <StackPanel>
        <!-- content items -->
    </StackPanel>
</mosaic:InertiaScrollViewer>
```

## Notes

- `IsScrollAnimation=False` (default) makes the control behave exactly like a standard `ScrollViewer`.
- Mouse wheel events are marked `Handled=true` when animation is active, which prevents parent scrollers from also scrolling.
- The easing function is `CubicEase` with `EasingMode.EaseOut` — adjust `AnimationDurationMilliseconds` to tune the feel.
