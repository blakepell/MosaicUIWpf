# TableSizePicker

A grid of cells used to pick a table size in the same manner as the Microsoft Word `Insert Table` button. Hovering a cell previews a region anchored at the upper left cell, clicking commits it and raises `TableSizeSelected` followed by `RequestClose` so a hosting popup can close.

## Usage

```xml
<mosaic:TableSizePicker
    RowCount="8"
    ColumnCount="8"
    TableSizeSelected="TableSizePicker_OnTableSizeSelected"
    RequestClose="TableSizePicker_OnRequestClose" />
```

```csharp
private void TableSizePicker_OnTableSizeSelected(object sender, TableSizeSelectedEventArgs e)
{
    InsertTable(e.RowCount, e.ColumnCount);
}
```

## Hosting in a Popup

The picker never touches its host. Call `OnShow()` when the popup opens and close the popup from `RequestClose`.

```xml
<ToggleButton x:Name="InsertTableButton" Content="Insert Table" />

<Popup
    AllowsTransparency="True"
    IsOpen="{Binding ElementName=InsertTableButton, Path=IsChecked, Mode=TwoWay}"
    Opened="InsertTablePopup_OnOpened"
    Placement="Bottom"
    PlacementTarget="{Binding ElementName=InsertTableButton}"
    StaysOpen="False">
    <mosaic:TableSizePicker
        x:Name="PopupPicker"
        RequestClose="PopupPicker_OnRequestClose"
        TableSizeSelected="PopupPicker_OnTableSizeSelected" />
</Popup>
```

```csharp
private void InsertTablePopup_OnOpened(object? sender, EventArgs e) => PopupPicker.OnShow();

private void PopupPicker_OnRequestClose(object? sender, EventArgs e) => InsertTableButton.IsChecked = false;
```

## Keyboard

| Key | Action |
| --- | --- |
| Arrow keys | Move the preview region |
| Enter, Space | Commit the previewed size |
| Home | Preview 1 × 1 |
| End | Preview the full grid |
| Escape | Clear the preview and raise `RequestClose` |

## Notable members

| Member | Description |
| --- | --- |
| `RowCount`, `ColumnCount` | Grid dimensions, default 8 × 8, coerced into the range 1 to 50. |
| `SelectedRowCount`, `SelectedColumnCount` | Read only committed selection, zero when nothing is committed. |
| `PreviewRowCount`, `PreviewColumnCount` | Read only hover/keyboard preview, never overwrites the committed selection. |
| `SelectionText`, `SelectionTextFormat`, `EmptySelectionText` | The caption, `{0} × {1} Table` by default, `Insert Table` when nothing is previewed or selected. |
| `CellWidth`, `CellHeight`, `CellMargin` | Cell metrics. |
| `CellBackground`, `CellBorderBrush`, `HighlightBackground`, `HighlightBorderBrush`, `CommittedBackground`, `CommittedBorderBrush` | Cell brushes, all default to Mosaic theme tokens. |
| `ClearSelectionOnShow` | Whether `OnShow()` clears the committed selection, default `true`. |
| `Clear()`, `ClearCommand` | Return the control to its unselected state. |
| `OnShow()`, `OnHide()` | Lifecycle hooks for a hosting popup or dropdown. |
| `Select(int, int)` | Commit a size programmatically, identical to a click. |
