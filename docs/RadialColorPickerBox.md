# RadialColorPickerBox

A compact color field with a swatch and editable hex value. Its drop-down hosts a full [`RadialColorPicker`](./RadialColorPicker.md), making it suitable for forms, dialogs, and property panels where the full editor should remain hidden until needed.

## Usage

```xml
<mosaic:RadialColorPickerBox
    Width="220"
    CornerRadius="6"
    SelectedColor="{Binding AccentColor, Mode=TwoWay}"
    ShowAlpha="False" />
```

`SelectedColor`, `SelectedBrush`, `HexValue`, and `IsDropDownOpen` bind two way by default. The three value representations remain synchronized; bind whichever representation your view model uses rather than binding multiple representations on the same instance.

Hex values are normalized to `#AARRGGBB`. The field accepts `RGB`, `RRGGBB`, and `AARRGGBB` with an optional leading `#`. Pressing Enter or moving focus commits valid text. Invalid text and Escape restore the previous valid value.

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedColor` | `Color` | black | Selected color, including alpha. |
| `SelectedBrush` | `Brush` | black | Synchronized `SolidColorBrush` representation. |
| `HexValue` | `string` | `#FF000000` | Synchronized normalized hex representation. |
| `IsDropDownOpen` | `bool` | `false` | Two-way bindable drop-down state. |
| `WheelDiameter` | `double` | `200` | Diameter of the hue wheel inside the drop-down. |
| `ShowAlpha` | `bool` | `true` | Shows the alpha editor inside the drop-down. |
| `CornerRadius` | `CornerRadius` | `2` | Compact field border radius. |

The control raises `ColorChanged` with the effective `Color`, `Brush`, and normalized `HexValue` whenever the selection changes.
