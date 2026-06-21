# Files

**Base class:** `Control` (CustomControl)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/Files/Files.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/FilesExample.xaml`

## Description

A lookless control that lists the files in a directory using a three-column view (Name with shell icon, Date Modified, Size). Supports single or multiple selection, an optional file-system watcher, manual refresh, and a `FileActivated` (double-click) event.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `DirectoryPath` | `string` | The directory whose files are listed. |
| `Filter` | `string` | Search/glob filter (e.g. `*.txt`). |
| `ShowHiddenFiles` | `bool` | Whether hidden files are listed. |
| `SelectionMode` | `SelectionMode` | Single or multiple selection. |
| `EnableFileWatcher` | `bool` | Live-update the list via a `FileSystemWatcher`. |
| `SelectedItem` | `FileItem` | The currently selected file (two-way). |
| `Items` | collection | Read-only collection of listed `FileItem`s. |
| `FileActivatedCommand` | `ICommand` | Command invoked on file activation (double-click). |

## Events

`FileDoubleClick` (`FileActivatedEventHandler`) and `SelectionChanged` (`SelectionChangedEventHandler`).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:Files
    DirectoryPath="C:\Logs"
    Filter="*.log"
    EnableFileWatcher="True"
    SelectionMode="Extended"
    FileActivatedCommand="{Binding OpenFileCommand}" />
```

## Notes

- Uses the Windows shell to resolve per-file icons.
- Set `EnableFileWatcher` to keep the list in sync as files change on disk; call refresh manually otherwise.
