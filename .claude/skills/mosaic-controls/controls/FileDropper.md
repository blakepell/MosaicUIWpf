# FileDropper

**Base class:** `Control` (CustomControl)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/FileDropper/FileDropper.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/FileDropperExample.xaml`

## Description

A drop target that accepts files dragged from the operating system. Displays a prompt, an upload icon, and the accepted file types. The border turns green for valid files and red for invalid files. Raises a `FileDrop` event (and optional command) when files are dropped.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `Prompt` | `string` | Instructional text shown in the drop zone. |
| `AcceptedFileTypes` | `string` | Comma/semicolon list of accepted extensions used for validation. |
| `AcceptedFileTypesDisplay` | `string` | Read-only friendly display of accepted types. |
| `DragState` | enum | Read-only current drag state (none/valid/invalid). |
| `CornerRadius` | `CornerRadius` | Corner radius of the drop surface. |
| `FileDropCommand` | `ICommand` | Command invoked when files are dropped. |

## Events

`FileDrop` (`FileDropEventHandler`) — raised with the dropped files.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:FileDropper
    Prompt="Drop images here"
    AcceptedFileTypes=".png,.jpg,.jpeg"
    FileDropCommand="{Binding ImportFilesCommand}" />
```

## Notes

- Validation against `AcceptedFileTypes` drives the green (valid) / red (invalid) border feedback.
- Handle `FileDrop` or bind `FileDropCommand` to process the dropped paths.
