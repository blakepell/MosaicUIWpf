# ModalDialog

**Base class:** `ContentControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ModalDialog/ModalDialog.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/ModalDialogExample.xaml`

## Description

A lookless in-window modal dialog. `ShowAsync(host)` places the dialog card centered in the host element's **adorner layer**, blurs and dims the host beneath it, traps keyboard navigation inside the dialog, and returns a `Task<bool?>` that completes with the value passed to `Close`. No separate `Window` is involved.

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_CloseButton` | `ButtonBase` | Header close (X) button; closes with a `null` result. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string` | `""` | Header title. |
| `Description` | `string` | `""` | Optional secondary text under the title. |
| `Content` | `object` | `null` | The dialog body (XAML content property). |
| `CornerRadius` | `CornerRadius` | `10` | Dialog card corner radius. |
| `ShowCloseButton` | `bool` | `true` | Whether the header X button is shown. |
| `CloseOnEscape` | `bool` | `true` | Escape closes with a `null` result. |
| `CloseOnBackdropClick` | `bool` | `false` | Clicking the dimmed backdrop closes with a `null` result. |
| `BackdropBrush` | `Brush` | `#66000000` | Brush painted over the blurred host. |
| `BlurRadius` | `double` | `12` | Blur applied to the host. Set to `0` for a dim-only backdrop. |
| `IsOpen` | `bool` (read-only) | `false` | Whether the dialog is currently displayed. |

## Events

| Event | Args | Description |
|---|---|---|
| `Opened` | `RoutedEventArgs` (bubbling) | Raised after the dialog is added to the adorner layer. |
| `Closed` | `RoutedEventArgs` (bubbling) | Raised after the dialog is dismissed. |

## Methods

| Member | Description |
|---|---|
| `Task<bool?> ShowAsync(UIElement host)` | Shows the dialog over `host` and completes when it is closed. Throws `InvalidOperationException` if already open or no adorner layer is found. |
| `void Close(bool? result = null)` | Closes the dialog and completes the task. Safe to call when already closed. |
| `static ModalDialog? FindHost(DependencyObject? element)` | Walks up from an element inside the dialog content to the containing `ModalDialog`. Useful for buttons in the content that need to call `Close`. |

## Example

```xml
<mosaic:ModalDialog x:Name="ConfirmDialog" Title="Delete project" Description="This cannot be undone.">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
        <Button Content="Cancel" Click="Cancel_Click" Margin="0,0,8,0" />
        <mosaic:AccentButton Content="Delete" Click="Confirm_Click" />
    </StackPanel>
</mosaic:ModalDialog>
```

```csharp
var result = await ConfirmDialog.ShowAsync((UIElement)this.Content);

if (result == true)
{
    ViewModel.Delete();
}

private void Confirm_Click(object sender, RoutedEventArgs e)
    => ModalDialog.FindHost((DependencyObject)sender)?.Close(true);
```

## Notes

- The host must be loaded and beneath an `AdornerDecorator` — a window's content is by default. Pass the window's root content element so everything beneath it blurs.
- The blur effect is applied to the *adorned element*, so the dialog itself stays sharp in the adorner layer above.
- The entrance animation drops its `ScaleTransform` once it settles; a held transform would keep text unsnapped and blurry for as long as the dialog is open.
- Keyboard focus moves into the dialog on open and is restored to the previously focused element on close; tab/arrow navigation cycles inside the dialog while open.
- The dialog is removed from the adorner's tree on close, so the same instance can be shown again.
- For simple message prompts prefer [MessageBox.md](MessageBox.md); for transient non-blocking notifications use [Toast.md](Toast.md).
