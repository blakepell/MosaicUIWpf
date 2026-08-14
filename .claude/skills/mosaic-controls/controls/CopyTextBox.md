# CopyTextBox

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/CopyTextBox/CopyTextBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/CopyTextBoxExample.xaml`

## Description

A themed text box with an attached copy button docked to its right edge, sharing the box's border. Clicking the button writes `Text` to the clipboard (retrying up to three times, since the clipboard is a shared OS resource), raises `TextCopied`, executes `Command`, and optionally shows a [Toast](Toast.md) reporting the outcome.

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_TextBox` | `TextBox` | The editable text area. |
| `PART_CopyButton` | `ButtonBase` | The copy button. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Text` | `string` | `""` | The displayed text, copied to the clipboard. Two-way by default. |
| `Watermark` | `string` | `""` | Placeholder shown when empty and unfocused. |
| `IsReadOnly` | `bool` | `false` | Whether the user can edit the text. |
| `CornerRadius` | `CornerRadius` | `0` | Outer border corner radius. |
| `SelectedBorderBrush` | `Brush?` | `null` | Border brush while the control has keyboard focus. |
| `CopyIcon` | `Geometry?` | copy glyph | Geometry rendered inside the copy button. |
| `CopyButtonToolTip` | `object?` | `"Copy to clipboard"` | Tool tip for the copy button. |
| `Command` / `CommandParameter` | `ICommand?` / `object?` | `null` | Executed after the copy is attempted. A null parameter passes `Text`. |

The control's default `Height` is `28`.

### Toast properties

| Property | Type | Default | Description |
|---|---|---|---|
| `ShowToast` | `bool` | `false` | Show a toast reporting success/failure. Off by default. |
| `ToastManager` | `ToastManager?` | `null` | Manager used. Falls back to `ToastManager.Default`, then `ToastManager.ForElement(this)`. |
| `ToastQuadrant` | `ToastQuadrant` | `BottomRight` | Corner the toast appears in. |
| `ToastDuration` | `TimeSpan?` | 3 seconds | `null` keeps the toast open until the user closes it. |
| `ToastSuccessTitle` / `ToastSuccessMessage` | `string` | `"Copied"` / `"The text was copied to the clipboard."` | Success toast text. |
| `ToastErrorTitle` / `ToastErrorMessage` | `string` | `"Copy Failed"` / `"The text could not be copied to the clipboard."` | Failure toast text; the OS error message is appended. |

## Events

| Event | Args | Description |
|---|---|---|
| `TextCopied` | `TextCopiedEventArgs` (bubbling) | Raised after every copy attempt. Carries the text, a success flag, and any exception. |

## Methods

| Member | Description |
|---|---|
| `bool Copy()` | Performs the copy programmatically; returns `true` on success. |
| `SelectAllText()` | Focuses the inner text box and selects all text. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:CopyTextBox
    Width="360"
    IsReadOnly="True"
    ShowToast="True"
    ToastQuadrant="BottomRight"
    Watermark="API key"
    Text="{Binding ApiKey}" />
```

## Notes

- Toast display is best effort: with no adorner layer above the control the notification is skipped rather than throwing.
- A `CopyTextBoxAutomationPeer` exposes the control to UI Automation.
- For copying from an existing `TextBox` you do not own, use `TextBoxCopyBehavior` instead (see [Behaviors.md](Behaviors.md)).
