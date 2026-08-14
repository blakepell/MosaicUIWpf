# TableSizePicker

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/TableSizePicker/TableSizePicker.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/TableSizePickerExample.xaml`

## Description

A grid of cells for picking a table size, matching the Microsoft Word *Insert Table* button. Hovering (or arrowing to) a cell previews the region anchored at the upper-left cell; clicking commits it, raising `TableSizeSelected` and then `RequestClose` so a hosting popup can close itself. Fully keyboard operable — Escape raises `RequestClose` without committing.

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_Grid` | `ItemsControl` | The uniform grid of cells, bound to `Cells`. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `RowCount` | `int` | `8` | Rows of cells displayed. Coerced to 1–50. |
| `ColumnCount` | `int` | `8` | Columns of cells displayed. Coerced to 1–50. |
| `SelectionTextFormat` | `string` | `"{0} × {1} Table"` | Composite format for the caption; `{0}` rows, `{1}` columns. |
| `EmptySelectionText` | `string` | `"Insert Table"` | Caption when nothing is previewed or selected. |
| `ClearSelectionOnShow` | `bool` | `true` | Whether `OnShow()` clears the committed selection. |
| `CellWidth` / `CellHeight` | `double` | `16` | Size of an individual cell. |
| `CellMargin` | `Thickness` | `1` | Spacing around each cell. |
| `CellBackground` / `CellBorderBrush` | `Brush?` | `null` (theme) | Unselected cell brushes. |
| `HighlightBackground` / `HighlightBorderBrush` | `Brush?` | `null` (theme) | Brushes for cells in the hover/keyboard preview region. |
| `CommittedBackground` / `CommittedBorderBrush` | `Brush?` | `null` (theme) | Brushes for cells in the committed selection. |

### Read-only properties

| Property | Type | Description |
|---|---|---|
| `SelectedRowCount` / `SelectedColumnCount` | `int` | The committed selection, or `0` when nothing is committed. |
| `PreviewRowCount` / `PreviewColumnCount` | `int` | The region currently being previewed, or `0`. Never overwrites the committed selection. |
| `SelectionText` | `string` | The caption — preview dimensions while hovering, committed otherwise, `EmptySelectionText` when neither. |
| `Cells` | `ObservableCollection<TableSizePickerCell>` | Cells in row-major order; rebuilt when `RowCount`/`ColumnCount` change. |

## Events and Commands

| Member | Description |
|---|---|
| `TableSizeSelected` (`TableSizeSelectedEventArgs`, bubbling) | Raised when the user commits a size. Carries `RowCount` and `ColumnCount`. |
| `RequestClose` (`EventHandler`) | Raised after a commit or on Escape. The picker has no knowledge of its host — close the popup here. |
| `ClearCommand` (`ICommand`) | Calls `Clear()`. |

## Methods

| Member | Description |
|---|---|
| `void Clear()` | Clears the preview and the committed selection. |
| `void OnShow()` | Call when the hosting popup opens: clears stale hover state, optionally clears the selection, and focuses the control for keyboard use. |
| `void OnHide()` | Clears the preview and releases mouse capture; the committed selection is preserved. Called automatically after a commit. |
| `void Select(int rowCount, int columnCount)` | Commits a size programmatically, exactly as a click would. |

## TableSizePickerCell

Created and mutated only by the picker; bind to it from an item template.

| Property | Type | Description |
|---|---|---|
| `Row` / `Column` | `int` | One-based position. |
| `IsPreviewSelected` | `bool` | Inside the active preview region. |
| `IsCommittedSelected` | `bool` | Inside the committed region (suppressed while a preview is active, so only one region highlights). |
| `IsAnchor` | `bool` | The lower-right corner of the active region — render a focus/hover affordance that does not rely on color alone. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<Popup x:Name="TablePopup" StaysOpen="False" Opened="TablePopup_Opened">
    <mosaic:TableSizePicker
        x:Name="Picker"
        RowCount="8"
        ColumnCount="10"
        TableSizeSelected="Picker_TableSizeSelected"
        RequestClose="Picker_RequestClose" />
</Popup>
```

```csharp
private void TablePopup_Opened(object sender, EventArgs e) => Picker.OnShow();
private void Picker_RequestClose(object? sender, EventArgs e) => TablePopup.IsOpen = false;

private void Picker_TableSizeSelected(object sender, TableSizeSelectedEventArgs e)
{
    Editor.InsertTable(e.RowCount, e.ColumnCount);
}
```

## Notes

- The cell under a point is computed from the uniform grid's geometry rather than hit tested.
- A `TableSizePickerAutomationPeer` exposes the control to UI Automation.
- Used by [MarkdownEditor](MarkdownEditor.md) for table insertion.
