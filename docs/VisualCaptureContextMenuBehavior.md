# VisualCaptureContextMenuBehavior

Adds "Copy as Image" and "Save as Image..." items to the context menu of the element the behavior is attached to. When the element has no context menu one is created; when it already has one the items are appended after a separator, and only the behavior's own items are removed again when it detaches. By default the attached element itself is captured; a different element can be chosen with `Target` or `TargetName`. Scrolling elements are captured at their full scroll extent (see [VisualCaptureButton](./VisualCaptureButton.md) for details on the underlying `VisualCapture` utility).

## Key Properties

| Property | Description |
|---|---|
| `Target` | The element to capture. Defaults to the attached element and takes precedence over `TargetName`. |
| `TargetName` | The `x:Name` of the element to capture, resolved from the attached element's name scope. |
| `CopyHeader` / `SaveHeader` | The menu item headers. Default to "Copy as Image" and "Save as Image...". |
| `ShowCopy` / `ShowSave` | Hide either item. Both default to `true`. |
| `InsertSeparator` | Whether a separator is placed before the items when they are appended to a menu that already has items. Defaults to `true`. |
| `FilePath` | The file the image is saved to. When empty the user is prompted with a save dialog. |
| `DefaultFileName` | The file name pre-populated in the save dialog. |
| `CaptureBackground` | A brush painted behind the element. `null` (default) keeps unpainted areas transparent. |
| `CaptureScrollExtent` | Whether a scrolling target is expanded to its full extent. Defaults to `true`. |

## Events

| Event | Description |
|---|---|
| `Captured` | Raised after the image was copied or saved. |
| `CaptureFailed` | Raised when the capture could not complete. `Error` holds the exception. |

## Example

```xml
xmlns:i="http://schemas.microsoft.com/xaml/behaviors"

<!-- No context menu of its own: the behavior creates one. -->
<DataGrid x:Name="Grid">
    <i:Interaction.Behaviors>
        <mosaic:VisualCaptureContextMenuBehavior DefaultFileName="grid.png" />
    </i:Interaction.Behaviors>
</DataGrid>

<!-- Existing menu: the items are appended after a separator. -->
<TextBox AcceptsReturn="True">
    <TextBox.ContextMenu>
        <ContextMenu>
            <MenuItem Command="ApplicationCommands.Cut" />
            <MenuItem Command="ApplicationCommands.Copy" />
            <MenuItem Command="ApplicationCommands.Paste" />
        </ContextMenu>
    </TextBox.ContextMenu>
    <i:Interaction.Behaviors>
        <mosaic:VisualCaptureContextMenuBehavior />
    </i:Interaction.Behaviors>
</TextBox>
```

> Controls such as `TextBox` show a built-in editing menu when their `ContextMenu` is null. Attaching this behavior to such a control replaces that built-in menu with one containing only the image items, so declare an explicit `ContextMenu` with the editing commands (as above) if both are needed.
