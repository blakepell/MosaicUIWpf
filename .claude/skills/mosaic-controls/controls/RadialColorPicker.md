# RadialColorPicker / RadialColorPickerBox

**Base class:** `Control` (both)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/RadialColorPicker/RadialColorPicker.cs`, `RadialColorPickerBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/ColorPickerExample.xaml`

## Description

`RadialColorPicker` is an HSV wheel picker: a hue ring around the perimeter plus an inner saturation/value square for fine tuning. A side panel exposes the hex value (with a copy button) and editable R, G, B, and alpha channels.

`RadialColorPickerBox` is the compact form — a hex entry field with a color swatch and a drop-down hosting a `RadialColorPicker`. The hex can be typed directly or picked from the wheel.

Both are Mosaic theme aware and lookless.

## RadialColorPicker

### Template Parts

`PART_HueImage`, `PART_SVImage`, `PART_SVBorder`, `PART_WheelCanvas`, `PART_HueThumb`, `PART_SVThumb`, `PART_HexTextBox`, `PART_CopyButton`, `PART_RedTextBox`, `PART_GreenTextBox`, `PART_BlueTextBox`, `PART_AlphaTextBox`.

### Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedColor` | `Color` | `Red` | The selected color including alpha. Two-way by default. |
| `SelectedBrush` | `Brush` | `Brushes.Red` | `SolidColorBrush` form of the selection. Two-way by default. |
| `HexValue` | `string` | `#FFFF0000` | Hex string, normalized to `#AARRGGBB`. Two-way by default. |
| `WheelDiameter` | `double` | `220` | Diameter of the hue wheel, in DIPs. |
| `RingThickness` | `double` | `24` | Thickness of the hue ring. |
| `ShowAlpha` | `bool` | `true` | Whether the alpha (transparency) editor is shown. |
| `CornerRadius` | `CornerRadius` | `4` | Outer border corner radius. |

### Events

| Event | Args | Description |
|---|---|---|
| `ColorChanged` | `ColorChangedEventArgs` | Raised when the selected color changes. |

## RadialColorPickerBox

### Template Parts

`PART_HexTextBox`, `PART_DropDownToggle`, `PART_Popup`, `PART_Picker`.

### Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedColor` | `Color` | `Black` | The selected color including alpha. Two-way by default. |
| `SelectedBrush` | `Brush` | `Brushes.Black` | `SolidColorBrush` form of the selection. Two-way by default. |
| `HexValue` | `string` | `#FF000000` | Hex string, normalized to `#AARRGGBB`. Two-way by default. |
| `IsDropDownOpen` | `bool` | `false` | Whether the picker drop-down is open. Two-way by default. |
| `ShowAlpha` | `bool` | `true` | Whether the drop-down picker shows the alpha editor. |
| `WheelDiameter` | `double` | `200` | Diameter of the wheel inside the drop-down. |
| `CornerRadius` | `CornerRadius` | `2` | Outer border corner radius. |

### Events

| Event | Args | Description |
|---|---|---|
| `ColorChanged` | `ColorChangedEventArgs` | Raised when the selected color changes. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:RadialColorPicker
    WheelDiameter="240"
    ShowAlpha="True"
    SelectedColor="{Binding AccentColor, Mode=TwoWay}" />

<mosaic:RadialColorPickerBox
    Width="160"
    SelectedColor="{Binding AccentColor, Mode=TwoWay}" />
```

## Notes

- `SelectedColor`, `SelectedBrush`, and `HexValue` are kept in sync — set whichever is most convenient for the binding.
- `ColorChangedEventArgs` is shared with [ColorPicker](ColorPicker.md) (`Controls/ColorPicker/ColorChangedEventArgs.cs`).
- Prefer [ColorPicker](ColorPicker.md) for a swatch/preset-driven picker, `RadialColorPicker` for free-form HSV selection, and `RadialColorPickerBox` when space is tight.
- For hex-only entry with a shade drop-down, see [HexColorTextBox.md](HexColorTextBox.md).
