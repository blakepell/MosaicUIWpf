# IPv4TextBox

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/IPv4TextBox/IPv4TextBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/IPv4TextBoxExample.xaml`

## Description

A single themed editor for a four-segment IPv4 address. Four `NumericTextBox` segments are coordinated into one aggregate `Text` value, with Windows-style typing behavior: typing `.` or a valid third digit advances to the next segment, and Left/Right/Backspace cross segment boundaries at the caret edges.

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_Segment1` … `PART_Segment4` | `NumericTextBox` | The four octet editors. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Text` | `string` | `""` | The last complete, valid IPv4 address. Two-way by default, `UpdateSourceTrigger=PropertyChanged`. |

## Commands

| Command | Description |
|---|---|
| `IPv4TextBox.CopyAddressCommand` | Copies the complete address to the clipboard. Used by the default context menu; `CanExecute` is false unless the current `Text` parses. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:IPv4TextBox Width="180" Text="{Binding ServerAddress, Mode=TwoWay}" />
```

## Notes

- **Validation is strict and coercive.** Assigning anything that is not exactly four decimal segments in `0`–`255` is rejected — the coercion callback restores the last accepted address. Abbreviated legacy forms (`10.1`) are not accepted.
- `Text` is only updated from the segments once **all four** are valid, so a binding retains the last complete address while the user is mid-edit.
- Paste is transactional: a complete valid address is applied to all four segments, anything else is cancelled. A child segment can never receive a partial paste.
- Focusing the control moves focus to the first empty segment, or selects the first segment when all are filled.
- Clipboard copy tolerates `ExternalException` (another process holding the clipboard) without throwing.
- A private automation peer implements `IValueProvider`, exposing the whole control as one editable value of type `Edit`.
