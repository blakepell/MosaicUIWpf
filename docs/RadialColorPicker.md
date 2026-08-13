# RadialColorPicker

A full HSV wheel color editor with a hue ring, saturation/value square, hex field, copy action, and red, green, blue, and optional alpha channel fields.

For a compact swatch and hex field that opens this editor in a drop-down, see [`RadialColorPickerBox`](./RadialColorPickerBox.md).

## Usage

```xml
<mosaic:RadialColorPicker
    SelectedColor="{Binding AccentColor, Mode=TwoWay}"
    ShowAlpha="True"
    WheelDiameter="220" />
```

`SelectedColor`, `SelectedBrush`, and `HexValue` bind two way by default and stay synchronized. Bind whichever representation your view model uses; binding more than one on the same instance can create competing sources.

Hex values are normalized to `#AARRGGBB`. The editor accepts `RGB`, `RRGGBB`, and `AARRGGBB` with an optional leading `#`. Invalid hex or channel text is restored to the current selected value when editing completes.

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedColor` | `Color` | red | Selected color, including alpha. |
| `SelectedBrush` | `Brush` | red | Synchronized `SolidColorBrush` representation. |
| `HexValue` | `string` | `#FFFF0000` | Synchronized normalized hex representation. |
| `WheelDiameter` | `double` | `220` | Hue-wheel diameter in device-independent pixels. |
| `RingThickness` | `double` | `24` | Hue-ring thickness. |
| `ShowAlpha` | `bool` | `true` | Shows the alpha channel editor. |
| `CornerRadius` | `CornerRadius` | `4` | Outer border radius. |

The control raises `ColorChanged` with the effective `Color`, `Brush`, and normalized `HexValue` whenever the selection changes.
