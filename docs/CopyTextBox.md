# CopyTextBox

A Mosaic styled text box with an attached copy button that places the box's text onto the clipboard. The button shares the text box's border (drawing only its left edge) so the two read as a single control, clipboard failures are handled rather than thrown, and an optional toast notification can report whether the copy succeeded.

![CopyTextBox](./images/CopyTextBox.png)

## Key Properties

| Property | Description |
|---|---|
| `Text` | The text displayed and copied to the clipboard. Two-way by default. |
| `Watermark` | Placeholder text shown while the control is empty and unfocused. |
| `IsReadOnly` | Whether the user can edit the text. |
| `CopyIcon` | The geometry rendered inside the copy button. Defaults to a copy glyph. |
| `CopyButtonToolTip` | The tool tip shown when hovering the copy button. |
| `CornerRadius` | The corner radius of the outer border. |
| `SelectedBorderBrush` | The border brush used while the control has keyboard focus. |
| `ShowToast` | Shows a success/error toast after a copy. **Off by default.** |
| `ToastManager` | The `ToastManager` to display through. Falls back to `ToastManager.Default`, then to a manager created over the containing window's content. |
| `ToastQuadrant` / `ToastDuration` | Where the toast appears and how long it stays open. |
| `ToastSuccessTitle` / `ToastSuccessMessage` | The success toast's text. |
| `ToastErrorTitle` / `ToastErrorMessage` | The failure toast's text. The OS error message is appended. |
| `Command` / `CommandParameter` | Executed after the copy is attempted. The parameter defaults to `Text`. |

## Events

| Event | Description |
|---|---|
| `TextCopied` | Bubbles after a copy is attempted. `TextCopiedEventArgs` carries the `Text`, whether it was `Successful`, and any `Exception` the clipboard threw. |

## Example

```xml
<mosaic:CopyTextBox
    Width="380"
    CornerRadius="4"
    ShowToast="True"
    Text="https://www.apexgate.net"
    Watermark="Nothing to copy" />
```
