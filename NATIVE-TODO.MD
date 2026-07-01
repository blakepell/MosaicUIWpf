# Native Theme TODO

Generated: 2026-07-01

This compares the opt-in Mosaic native theme dictionaries in `src/Mosaic.UI.Wpf/Themes/Native/*.xaml` against the WPF reference theme snapshot `src/Mosaic.UI.Wpf/Themes/Native/aerolite.normalcolor.xaml`.

`Themes/Native.xaml` currently merges 18 custom per-control dictionaries:

- `Button.xaml`
- `Calendar.xaml`
- `CheckBox.xaml`
- `ComboBox.xaml`
- `DataGrid.xaml`
- `DatePicker.xaml`
- `GroupBox.xaml`
- `ListBox.xaml`
- `ListView.xaml`
- `Menu.xaml`
- `RadioButton.xaml`
- `ScrollBar.xaml`
- `ScrollViewer.xaml`
- `Separator.xaml`
- `Slider.xaml`
- `TextBox.xaml`
- `ToolBar.xaml`
- `TreeView.xaml`

The full reference snapshots `aerolite.normalcolor.xaml`, `aero2.normalcolor.xaml`, and `aero2.darkcolor.xaml` are present in the folder but removed from the build in `Mosaic.UI.Wpf.csproj`; they are useful as source material, not active theme dictionaries.

## Covered From Aerolite

These aerolite controls or control families already have custom Mosaic native theme coverage:

| Aerolite target | Mosaic native file |
| --- | --- |
| `Button`, `ButtonBase`, `ToggleButton`, `RepeatButton` | `Button.xaml` |
| `Calendar`, `CalendarItem`, `CalendarButton`, `CalendarDayButton` | `Calendar.xaml` |
| `CheckBox` | `CheckBox.xaml` |
| `ComboBox`, `ComboBoxItem` | `ComboBox.xaml` |
| `ContextMenu`, `Menu`, `MenuItem` | `Menu.xaml` |
| `DataGrid`, `DataGridCell`, `DataGridColumnHeader`, `DataGridColumnHeadersPresenter`, `DataGridRow`, `DataGridRowHeader`, `DataGridCellsPresenter` | `DataGrid.xaml` |
| `DatePicker`, `DatePickerTextBox` | `DatePicker.xaml` |
| `GridViewColumnHeader`, `ListView`, `ListViewItem` | `ListView.xaml` |
| `GroupBox`, `GroupItem`, `HeaderedContentControl` | `GroupBox.xaml` |
| `ListBox`, `ListBoxItem` | `ListBox.xaml` |
| `RadioButton` | `RadioButton.xaml` |
| `ScrollBar` | `ScrollBar.xaml` |
| `ScrollViewer` | `ScrollViewer.xaml` |
| `Separator` | `Separator.xaml` |
| `Slider` | `Slider.xaml` |
| `TextBox`, `TextBoxBase` | `TextBox.xaml` |
| `ToolBar`, `ToolBarTray` and toolbar-specific item styles | `ToolBar.xaml` |
| `TreeView`, `TreeViewItem` | `TreeView.xaml` |

Some helper targets such as `Thumb`, `Border`, `Control`, `TextBox`, `ComboBox`, `Menu`, `MenuItem`, `Separator`, and `ScrollViewer` also appear inside these files for control-specific templates. Those are counted as covered only where they serve the themed control family.

## Missing Native Theme Files

These aerolite targets have no dedicated Mosaic custom native dictionary and are not represented by a clear family-level file in `Themes/Native.xaml`.

### High Priority

These are common WPF controls that users are likely to notice when `ThemeManager.Native=true` is enabled:

| Missing target | Suggested file | Notes |
| --- | --- | --- |
| `Expander` | `Expander.xaml` | Aerolite includes directional header toggle styles plus the main `Expander` template. |
| `GridSplitter` | `GridSplitter.xaml` | Aerolite includes `GridSplitterPreviewStyle` and the default `GridSplitter` style. |
| `Label` | `Label.xaml` | Should align foreground, disabled state, padding, and access-key behavior with Mosaic tokens. |
| `PasswordBox` | `PasswordBox.xaml` or fold into `TextBox.xaml` | Aerolite styles this separately from `TextBoxBase`; Mosaic currently only themes `TextBoxBase`/`TextBox`. |
| `ProgressBar` | `ProgressBar.xaml` | Missing determinate/indeterminate native restyle. |
| `RichTextBox` | `RichTextBox.xaml` or fold into `TextBox.xaml` | Aerolite bases it on `TextBoxBase` and adds document resources. |
| `StatusBar`, `StatusBarItem` | `StatusBar.xaml` | Includes separator style via `StatusBar.SeparatorStyleKey`. |
| `TabControl`, `TabItem` | `TabControl.xaml` | Aerolite has full tab strip placement templates and selected/hover/disabled states. |
| `ToolTip` | `ToolTip.xaml` | Missing popup surface styling. |

### Medium Priority

These are visible controls or containers, but often less central to app chrome:

| Missing target | Suggested file | Notes |
| --- | --- | --- |
| `ContentControl` | `ContentControl.xaml` | Aerolite provides a default content presenter template. Mosaic has a submenu `ContentControl` helper in `Menu.xaml`, but no general default. |
| `DocumentViewer` | `DocumentViewer.xaml` | Relevant for document-heavy apps; aerolite has a full `DocumentViewer` style. |
| `Frame` | `Frame.xaml` | Aerolite includes frame navigation chrome. |
| `Hyperlink` | `Hyperlink.xaml` or typography/native text file | Aerolite sets enabled/disabled and hover behavior. |
| `ItemsControl` | `ItemsControl.xaml` | Aerolite provides a default `ItemsPresenter` template. |
| `NavigationWindow` | `NavigationWindow.xaml` | Aerolite includes navigation button/menu chrome and browser-host downlevel styles. |
| `Page` | `Page.xaml` | Simple content template in aerolite. |
| `ResizeGrip` | `ResizeGrip.xaml` | Small but visible in resizable shell/status scenarios. |
| `Thumb` | `Thumb.xaml` | Aerolite has a generic `Thumb`; Mosaic has several control-specific thumb templates but no general default. |

### Low Priority / Text and Document Defaults

These are framework text/document defaults from aerolite. They may be better handled by Mosaic foundation typography resources than by native control files:

| Missing target | Suggested handling |
| --- | --- |
| `TextBlock` | Consider a native typography/default text dictionary only if implicit restyling is desired. |
| `Bold` | Usually low value unless document content needs full native theming. |
| `Italic` | Usually low value unless document content needs full native theming. |
| `Underline` | Usually low value unless document content needs full native theming. |
| `Paragraph` | Usually low value unless document content needs full native theming. |
| `List` | Usually low value unless document content needs full native theming. |
| `Floater` | Usually low value unless document content needs full native theming. |
| `FlowDocument` | Needed if `RichTextBox` or document viewer theming is completed. |
| `FixedDocument` / `FixedDocumentSequence` data templates | Relevant only if document viewing gets native coverage. |
| `CollectionViewGroup` data template | Optional grouped-items default; not a standalone control. |

### Window and Application Shell Defaults

Aerolite includes these shell-level defaults:

| Missing target | Suggested handling |
| --- | --- |
| `UserControl` | Usually avoid implicit restyling unless there is a concrete Mosaic shell goal. |
| `Window` | Mosaic already has window chrome resources elsewhere; do not duplicate without checking `Themes/Foundation/WindowChrome.xaml`. |
| `SystemParameters.NavigationChromeStyleKey` / `SystemParameters.NavigationChromeDownLevelStyleKey` | Treat as part of `NavigationWindow.xaml` if navigation chrome is added. |
| `SystemParameters.FocusVisualStyleKey`, `FocusVisual`, option/combo/tree focus visual helper styles | Consider a shared `FocusVisual.xaml` only if focus visuals need consistent Mosaic-wide native behavior. |

## Suggested Implementation Order

1. Add `Expander.xaml`, `TabControl.xaml`, `ProgressBar.xaml`, `StatusBar.xaml`, `ToolTip.xaml`, `Label.xaml`, and `GridSplitter.xaml`.
2. Extend `TextBox.xaml` or add adjacent files for `PasswordBox.xaml` and `RichTextBox.xaml`.
3. Add document/navigation coverage only if the demo or consumers actively use it: `DocumentViewer.xaml`, `Frame.xaml`, `NavigationWindow.xaml`, `Page.xaml`, and document text defaults.
4. Add broad framework defaults (`ContentControl`, `ItemsControl`, `Thumb`, text/document inline styles) carefully, because implicit styles for base controls can have wider impact than leaf-control styles.

When adding any file, also merge it from `src/Mosaic.UI.Wpf/Themes/Native.xaml` and keep using Mosaic theme tokens and `DynamicResource` references instead of hard-coded aerolite brushes.
