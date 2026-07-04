# Toast / ToastManager

`ToastManager` displays transient `ToastMessage` notification cards over a host element using the WPF adorner layer. Toasts can auto-dismiss after a duration or stay open until closed, support success/info/warning/error severities, and can be positioned in any corner of the host surface.

## Basic usage

Create a manager for the element that should host the toast overlay:

```csharp
private ToastManager? _toastManager;

private void OnLoaded(object sender, RoutedEventArgs e)
{
    _toastManager ??= new ToastManager(this);
}
```

Show a timed toast:

```csharp
_toastManager.Show(
    "Saved",
    "Your changes were saved.",
    ToastSeverity.Success,
    TimeSpan.FromSeconds(4),
    ToastQuadrant.BottomRight);
```

Show a persistent toast with a close button by passing `null` for the duration:

```csharp
var toast = _toastManager.Show(
    "Connection lost",
    "Reconnect when the network is available.",
    ToastSeverity.Warning,
    null,
    ToastQuadrant.TopRight);

toast.Dismissed += (_, e) =>
{
    // e.Reason is Timeout, ClosedByUser, or Programmatic.
};
```

For an application-wide instance, initialize the default manager once over a root element:

```csharp
ToastManager.Initialize(MainGrid);
ToastManager.Default?.Show("Ready", "Notifications are enabled.");
```

## Key members

| Member | Description |
|---|---|
| `ToastManager.Initialize(UIElement adornedElement)` | Creates `ToastManager.Default` over a host element. |
| `ToastManager.Show(...)` | Creates and displays a `ToastMessage`. Returns the toast so callers can listen for dismissal or dismiss it manually. |
| `ToastManager.DismissAll()` | Dismisses every open toast managed by the instance. |
| `ToastManager.ActiveCount` | Number of currently displayed toasts. |
| `ToastManager.ToastShown` | Raised when a toast is shown. |
| `ToastManager.AllDismissed` | Raised after the last open toast has been dismissed. |
| `ToastMessage.Dismiss(...)` | Dismisses a toast programmatically. |
| `ToastMessage.Dismissed` | Raised once when the toast finishes dismissing. |

## Options

| Type | Values |
|---|---|
| `ToastSeverity` | `Success`, `Info`, `Warning`, `Error` |
| `ToastQuadrant` | `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` |
| `ToastDismissReason` | `Timeout`, `ClosedByUser`, `Programmatic` |

The host element must be loaded and beneath an `AdornerDecorator`; normal WPF window content satisfies this by default.
