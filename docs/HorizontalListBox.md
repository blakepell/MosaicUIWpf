# HorizontalListBox

A ListBox variant that lays its items out horizontally as a row of cells which toggle on and off when clicked. Unlike CheckBoxList there is no checkbox glyph; selection is conveyed entirely by the cell fill color.

Defaults to `SelectionMode.Multiple` for click-to-toggle behavior. Set `SelectionMode="Single"` (with `ItemSpacing="0"` and `ItemCornerRadius="0"`) for a flush segmented picker.

| Property | Description |
| --- | --- |
| `ItemSpacing` | The space rendered between each cell. Default `4`. |
| `ItemCornerRadius` | The corner radius applied to each cell. Default `4`. |
| `ItemMinWidth` | The minimum width of each cell, keeping short values evenly sized. Default `40`. |
| `ItemPadding` | The padding applied inside each cell. Default `10,5`. |
| `SelectedBackground` | The background brush used by a cell that is toggled on. Defaults to the theme accent brush. |
| `SelectedForeground` | The foreground brush used by a cell that is toggled on. Defaults to the theme selection foreground brush. |
