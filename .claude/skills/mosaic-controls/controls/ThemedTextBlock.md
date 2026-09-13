# ThemedTextBlock

**Base class:** `TextBlock`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ThemedTextBlock/ThemedTextBlock.cs` (no XAML; not templated)  
**Example:** `src/MosaicWpfDemo/Views/Examples/ThemedTextBlockExample.xaml`

## Description

A `TextBlock` that picks a readable light or dark `Foreground` for whatever background it sits on. It finds the nearest ancestor (visual tree first, then logical) that is a `Border`, `Panel`, `Control` or `TextBlock` with a `Brush` background. It then watches that background, including changes inside a non-frozen brush, and updates the text color when it changes. You can also give it `BackgroundBrush` directly when the painted background isn't on one of those element types.

The choice uses relative luminance. At or below `LuminanceThreshold` (default `0.21`, from `ContrastBrushHelper.DefaultLuminanceThreshold`) it uses `LightForegroundBrush`, and above it uses `DarkForegroundBrush`. For a gradient brush, the average color of its stops is used. When `PaletteFamily` and `PaletteShade` are set, Mosaic's fixed switch point for that color family is used instead of luminance.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `BackgroundBrush` | `Brush?` | `null` | Background to compare against. `null` = auto-detect from ancestors. |
| `LuminanceThreshold` | `double` | `0.21` | Luminance cutoff, clamped to 0–1 (`NaN` resets it to the default). A higher value uses white text on lighter colors. About 0.179 gives the best WCAG contrast. |
| `LightForegroundBrush` | `Brush` | `Brushes.White` | Used on dark backgrounds. |
| `DarkForegroundBrush` | `Brush` | `Brushes.Black` | Used on light backgrounds. |
| `PaletteFamily` | `string?` | `null` | Mosaic palette family for a family-specific switch point. Use together with `PaletteShade`. |
| `PaletteShade` | `int` | `0` | Palette shade (e.g. `500`). `0` turns off palette-specific selection. |

Built-in palette switch points (the shade at which light text starts, case-insensitive): `Blue`, `Purple`, `Orange`, `Yellow` and `Green` start at 500. `Teal` and `Cyan` start at 600. Other families fall back to the luminance threshold.

## XAML Example

```xml
xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
xmlns:themes="http://schemas.apexgate.net/wpf/mosaic-ui"

<!-- Automatic: reads the parent Border's background -->
<Border Padding="20" Background="{DynamicResource {x:Static themes:AssetResourceKeys.Blue500BackgroundBrushKey}}">
    <mosaic:ThemedTextBlock Text="Blue 500 · automatic" FontWeight="SemiBold" />
</Border>

<!-- Palette-aware switch point -->
<Border Background="{DynamicResource {x:Static themes:AssetResourceKeys.Yellow500BackgroundBrushKey}}">
    <mosaic:ThemedTextBlock Text="Yellow 500" PaletteFamily="Yellow" PaletteShade="500" />
</Border>

<!-- Explicit background, when detection isn't appropriate -->
<mosaic:ThemedTextBlock BackgroundBrush="{Binding TagBrush}" LuminanceThreshold="0.3" Text="{Binding TagName}" />
```

## Notes

- The foreground is set with `SetCurrentValue`, so any later background change overwrites a local `Foreground`. To change the colors, use `LightForegroundBrush`/`DarkForegroundBrush` instead.
- Brushes that are not solid or gradient (such as `ImageBrush` or `VisualBrush`) leave `Foreground` unchanged.
- The watcher is attached on `Loaded` and removed on `Unloaded`. It is re-attached when the visual parent changes.
- `ContrastBrushHelper` (`Mosaic.UI.Wpf.Themes`) exposes the same logic for code: `GetForegroundBrush`, `GetPaletteForegroundBrush`, `ShouldUseLightForeground` and `GetRelativeLuminance`.
