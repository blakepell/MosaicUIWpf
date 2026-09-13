# HorizontalListBox

**Base class:** `ListBox`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/HorizontalListBox/HorizontalListBox.cs` + `HorizontalListBox.xaml`  
**Example:** `src/MosaicWpfDemo/Views/Examples/HorizontalListBoxExample.xaml`

## Description

A `ListBox` that shows its items as a horizontal row of cells that toggle on and off when clicked. Unlike `CheckBoxList` there is no checkbox. A selected cell is shown only by its fill color (`SelectedBackground`/`SelectedForeground`, which default to the theme accent and selection foreground). It defaults to `SelectionMode.Multiple`, which suits pickers like "repeat on these weekdays". Set `SelectionMode="Single"` to get a segmented control.

Everything from `ListBox` still works: `ItemsSource`, `DisplayMemberPath`, `ItemTemplate`, `SelectedItem`/`SelectedItems` and `SelectionChanged`. The row scrolls horizontally if it overflows.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectionMode` | `SelectionMode` | `Multiple` | `Multiple` = click toggles each cell. `Single` = segmented picker. |
| `ItemSpacing` | `double` | `4` | Space between cells. |
| `ItemCornerRadius` | `CornerRadius` | `4` | Cell corner radius. Use `0` with `ItemSpacing="0"` for a flush segmented look. |
| `ItemMinWidth` | `double` | `40` | Minimum cell width, which keeps short labels evenly sized. |
| `ItemPadding` | `Thickness` | `10,5,10,5` | Padding inside each cell. |
| `SelectedBackground` | `Brush?` | theme `AccentBrush` | Fill of a selected cell. |
| `SelectedForeground` | `Brush?` | theme `SelectionForegroundBrush` | Text color of a selected cell. |

Unselected cells use `ControlTextBackgroundBrush` with a `ControlBorderBrush` border, and disabled cells use `ControlDisabledForegroundBrush`.

## Events

Standard `ListBox.SelectionChanged` (the `[DefaultEvent]`).

## XAML Example

```xml
xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"
xmlns:themes="http://schemas.apexgate.net/wpf/mosaic-ui"

<!-- Multi-select weekday picker -->
<mosaic:HorizontalListBox x:Name="DayList"
                          ItemsSource="{Binding Days}"
                          DisplayMemberPath="Abbreviation"
                          SelectionChanged="DayList_SelectionChanged" />

<!-- Single-select segmented control -->
<mosaic:HorizontalListBox SelectionMode="Single"
                          ItemsSource="{Binding Views}"
                          SelectedItem="{Binding CurrentView}"
                          ItemSpacing="0" ItemCornerRadius="0" ItemMinWidth="72" />

<!-- Pill cells with a custom selected brush -->
<mosaic:HorizontalListBox ItemsSource="{Binding Priorities}"
                          ItemSpacing="8" ItemCornerRadius="14" ItemMinWidth="0" ItemPadding="14,5"
                          SelectedBackground="{DynamicResource {x:Static themes:MosaicTheme.SuccessBrush}}" />
```

```csharp
// Pre-select in multi mode through SelectedItems
for (int i = 1; i <= 5; i++) DayList.SelectedItems.Add(Days[i]);
```

## Notes

- Item container: `HorizontalListBoxItem` (`ListBoxItem`). Its `CornerRadius` property is bound to the owner's `ItemCornerRadius`, and `Padding`/`MinWidth` are bound to `ItemPadding`/`ItemMinWidth`.
- `SelectedItems` is not a bindable dependency property, just like on a normal `ListBox`. In multi mode, sync it from `SelectionChanged` or with a behavior.
- Automation peer: `HorizontalListBoxAutomationPeer`.
- Use [CheckBoxList.md](CheckBoxList.md) for a vertical list with checkbox glyphs.
