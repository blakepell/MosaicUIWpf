# TagBox

**Base class:** `Control` (CustomControl)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/TagBox/TagBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/TagBoxExample.xaml`

## Description

A specialized input control that turns typed text into removable, vibrantly-colored tags. `Enter` commits the current text as a tag, each tag has an ✕ button, and `Backspace` (on an empty input) removes the last tag. Tags are surfaced through a bindable `Tags` collection.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `Tags` | collection | The bindable collection of committed tags. |
| `Watermark` | `string` | Placeholder text shown when the input is empty. |
| `AllowDuplicates` | `bool` | Whether the same tag may be added more than once. |
| `TagBackground` | `Brush` | Background of each tag chip. |
| `TagForeground` | `Brush` | Foreground/text of each tag chip. |
| `TagBorderBrush` | `Brush` | Border of each tag chip. |
| `TagDeleteHoverBackground` | `Brush` | Hover background of the ✕ delete button. |
| `TagCornerRadius` | `CornerRadius` | Corner radius of tag chips. |
| `CornerRadius` | `CornerRadius` | Corner radius of the control surface. |
| `ShowWatermark` | `bool` | Read-only — whether the watermark is currently shown. |

## Events

`TagChanging` (`TagChangingEventArgs`, cancellable) and `TagChanged` (`TagChangedEventArgs`).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:TagBox
    Tags="{Binding SelectedTags}"
    Watermark="Add a tag and press Enter"
    AllowDuplicates="False" />
```

## Notes

- `Enter` commits, ✕ removes a specific tag, and `Backspace` removes the last tag when the input is empty.
- Handle `TagChanging` to validate or reject a tag before it is added.
