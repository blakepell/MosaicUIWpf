# Files

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/Files/Files.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/FilesExample.xaml`

## Description

A directory listing showing each entry's name with shell icon, modified date, and friendly size. It supports filtering, sorting, single or multi-selection, optional `FileSystemWatcher` refresh, folder navigation (with a `..` parent entry and an optional root boundary), and file activation through event or command.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `DirectoryPath` | `string` | `string.Empty` | Folder whose contents are listed; updates as the user navigates. |
| `Filter` | `string` | `*` | Search pattern such as `*.txt` (applies to files only). |
| `ShowHiddenFiles` | `bool` | `false` | Includes hidden/system entries. |
| `SelectionMode` | `SelectionMode` | `Single` | Single, Multiple, or Extended selection. |
| `EnableFileWatcher` | `bool` | `false` | Watches the directory and coalesces refreshes. |
| `ShowFolders` | `bool` | `true` | Lists sub-folders and a `..` parent entry; activating a folder navigates into it. |
| `RootDirectory` | `string` | `string.Empty` | Bounds upward navigation; the user cannot navigate above this folder. Empty allows navigation anywhere. |
| `ShowNavigateErrorMessageBox` | `bool` | `true` | Shows a warning message box when folder navigation fails. The `NavigateError` event fires regardless. |
| `SelectedItem` | `FileItem?` | `null` | Two-way selected row. |
| `Items` | `ObservableCollection<FileItem>` | read-only | Displayed rows. |
| `FileActivatedCommand` | `ICommand?` | `null` | Executes on file activation with `FileInfo` parameter. |

Computed helpers: `SelectedFile`, `SelectedFiles` (both exclude folders).

Events: `FileDoubleClick`, `SelectionChanged`, `NavigateError`.

Methods: `Refresh()`, `SortBy(memberPath, direction)`.

```xml
<mosaic:Files
    DirectoryPath="{Binding Folder}"
    Filter="*.cs"
    EnableFileWatcher="True"
    SelectionMode="Extended"
    FileActivatedCommand="{Binding OpenFileCommand}" />
```
