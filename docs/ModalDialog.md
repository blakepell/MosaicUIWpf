# ModalDialog

A lookless modal dialog that displays arbitrary content centered over a host element using the WPF adorner layer. While open, the host is dimmed and optionally blurred, the dialog remains sharp, keyboard focus is cycled inside the dialog, and `ShowAsync` completes with the value passed to `Close`.

## Basic usage

```csharp
var dialog = new ModalDialog
{
    Title = "Delete item?",
    Description = "This action cannot be undone.",
    Content = new TextBlock
    {
        Text = "Add any WPF content here.",
        TextWrapping = TextWrapping.Wrap
    }
};

bool? result = await dialog.ShowAsync(Window.GetWindow(this)?.Content as UIElement ?? this);
```

Buttons inside the dialog can close it directly when you keep a reference to the dialog:

```csharp
var confirm = new AccentButton { Content = "Delete" };
confirm.Click += (_, _) => dialog.Close(true);
```

For content that does not have the dialog reference, use `ModalDialog.FindHost`:

```csharp
cancel.Click += (sender, _) =>
    ModalDialog.FindHost((DependencyObject)sender!)?.Close(false);
```

## Key members

| Member | Description |
|---|---|
| `ShowAsync(UIElement host)` | Shows the dialog over a loaded element with an adorner layer and returns the eventual `bool?` result. |
| `Close(bool? result = null)` | Dismisses the dialog and completes the task returned by `ShowAsync`. |
| `FindHost(DependencyObject? element)` | Walks up from a child element to find the containing `ModalDialog`. |
| `Title` | Header text displayed at the top of the dialog. |
| `Description` | Optional secondary text displayed under the title. |
| `ShowCloseButton` | Shows or hides the header close button. |
| `CloseOnEscape` | Allows Escape to close the dialog with a `null` result. |
| `CloseOnBackdropClick` | Allows clicking the dimmed backdrop to close the dialog with a `null` result. |
| `BackdropBrush` | Brush painted over the host while the dialog is open. |
| `BlurRadius` | Blur radius applied to the host. Set to `0` for dimming without blur. |

## Notes

The host passed to `ShowAsync` must be loaded and beneath an `AdornerDecorator`; normal WPF window content satisfies this by default. `ModalDialog` raises `Opened` after it is added to the adorner layer and `Closed` after it has been dismissed.
