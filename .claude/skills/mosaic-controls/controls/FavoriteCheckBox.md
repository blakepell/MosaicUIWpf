# FavoriteCheckBox

**Base class:** `CheckBox`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/FavoriteCheckBox/FavoriteCheckBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/FavoriteCheckBoxExample.xaml`

## Description

A `CheckBox` that displays a single favorite symbol (defaults to ★) instead of the standard box. The symbol and its checked/unchecked/hover colors are customizable. Use it for favoriting, starring, or pinning items.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `Symbol` | `string` | The glyph to display (default `★`). |
| `SymbolFont` | `FontFamily` | Font used to render the symbol. |
| `SymbolSize` | `double` | Font size of the symbol. |
| `CheckedBrush` | `Brush` | Symbol color when checked. |
| `UncheckedBrush` | `Brush` | Symbol color when unchecked. |
| `HoverBrush` | `Brush` | Symbol color on mouse hover. |

Standard `CheckBox` members apply (`IsChecked`, `Command`, `Checked`/`Unchecked` events, etc.).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:FavoriteCheckBox IsChecked="{Binding IsFavorite}" />

<mosaic:FavoriteCheckBox
    Symbol="♥"
    SymbolSize="20"
    CheckedBrush="Crimson"
    UncheckedBrush="Gray" />
```

## Notes

- Binds two-way on `IsChecked` like a normal `CheckBox`.
- Swap `Symbol` to any glyph available in `SymbolFont` (heart, pin, bookmark, etc.).
