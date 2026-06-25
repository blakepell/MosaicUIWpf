# Files

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/Files/Files.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/FilesExample.xaml`

## Description

A directory file list showing file name with shell icon, modified date, and friendly size. It supports filtering, sorting, single or multi-selection, optional `FileSystemWatcher` refresh, and file activation through event or command.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `DirectoryPath` | `string` | `string.Empty` | Folder whose files are listed. |
| `Filter` | `string` | `*` | Search pattern such as `*.txt`. |
| `ShowHiddenFiles` | `bool` | `false` | Includes hidden/system files. |
| `SelectionMode` | `SelectionMode` | `Single` | Single, Multiple, or Extended selection. |
| `EnableFileWatcher` | `bool` | `false` | Watches the directory and coalesces refreshes. |
| `SelectedItem` | `FileItem?` | `null` | Two-way selected row. |
| `Items` | `ObservableCollection<FileItem>` | read-only | Displayed rows. |
| `FileActivatedCommand` | `ICommand?` | `null` | Executes on file double-click with `FileInfo` parameter. |

Computed helpers: `SelectedFile`, `SelectedFiles`.

Events: `FileDoubleClick`, `SelectionChanged`.

Methods: `Refresh()`, `SortBy(memberPath, direction)`.

```xml
<mosaic:Files
    DirectoryPath="{Binding Folder}"
    Filter="*.cs"
    EnableFileWatcher="True"
    SelectionMode="Extended"
    FileActivatedCommand="{Binding OpenFileCommand}" />
```
