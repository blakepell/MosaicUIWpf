# Toast (ToastManager / ToastMessage)

**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/Toast/ToastManager.cs`, `ToastMessage.xaml(.cs)`, `ToastEnums.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/ToastExample.xaml`

## Description

Transient notification cards displayed in an adorner overlaying a host element. `ToastManager` owns the stacks; `ToastMessage` is a single card. Toasts stack within their `ToastQuadrant` (newest closest to the corner) and the remaining toasts shift up to fill the gap when one closes. Empty space in the overlay passes hit testing through to the UI beneath.

## ToastManager

| Member | Description |
|---|---|
| `static ToastManager Initialize(UIElement adornedElement)` | Creates the application-wide `Default` instance. Typically called once with the main window's root content. |
| `static ToastManager? Default` | The instance created by `Initialize`, or `null`. |
| `static ToastManager? ForElement(DependencyObject? element)` | Gets (creating on first use) the shared manager for the toast surface above `element`. Returns `null` when the element is not yet in a hostable tree. |
| `static UIElement? FindHost(DependencyObject? element)` | Resolves the top-most toast host: the window's content, else the outermost `AdornerDecorator` child. |
| `ToastManager(UIElement adornedElement)` | Creates a manager over a specific element. |
| `ToastMessage Show(string title, string message, ToastSeverity severity = Info, TimeSpan? duration = null, ToastQuadrant quadrant = BottomRight)` | Shows a toast and returns it. A `null` duration keeps it open until the user closes it. |
| `void DismissAll()` | Dismisses every open toast. |
| `UIElement AdornedElement` / `int ActiveCount` | Host element / number of toasts currently displayed. |
| `event EventHandler? ToastShown` / `AllDismissed` | Raised when a toast is shown / when the last one is dismissed. |

## ToastMessage

| Member | Description |
|---|---|
| `ToastMessage(string title, string message, ToastSeverity severity, TimeSpan? duration)` | A single card. Normally created via `ToastManager.Show`. |
| `ToastSeverity Severity` / `TimeSpan? Duration` | Read-only card state. |
| `void Dismiss(ToastDismissReason reason = Programmatic)` | Fades out and raises `Dismissed`. Idempotent. |
| `event EventHandler<ToastDismissedEventArgs>? Dismissed` | Raised once when the toast has been dismissed for any reason. |

## Enums

| Type | Values |
|---|---|
| `ToastSeverity` | `Success`, `Info`, `Warning`, `Error` — determines color scheme and Segoe MDL2 glyph. |
| `ToastQuadrant` | `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`. |
| `ToastDismissReason` | `Timeout`, `ClosedByUser`, `Programmatic`. |

## Example

```csharp
// Once, at startup (e.g. in MainWindow.Loaded):
ToastManager.Initialize((UIElement)this.Content);

// Anywhere afterwards:
ToastManager.Default!.Show(
    "Saved",
    "Your changes were written to disk.",
    ToastSeverity.Success,
    TimeSpan.FromSeconds(3));

// Or resolve the manager for whatever surface a control lives on:
ToastManager.ForElement(myButton)?.Show("Heads up", "Disk is nearly full.", ToastSeverity.Warning);
```

## Notes

- A toast with a duration auto-closes and shows **no** close button; a toast with `duration: null` shows a close button and stays until dismissed.
- `Show` throws `InvalidOperationException` if no adorner layer exists above the host (the element must be loaded and inside an `AdornerDecorator` — window content is by default).
- The toast host adorner is re-added to the top of the layer on every `Show`, so toasts stay above later adorners such as a [ModalDialog](ModalDialog.md) backdrop.
- Per-host managers are held in a `ConditionalWeakTable`, so closed windows are not kept alive.
- [CopyTextBox](CopyTextBox.md) can raise its own success/failure toasts via `ShowToast`.
