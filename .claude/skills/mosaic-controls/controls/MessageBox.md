# MessageBox

**Type:** `static class` (not a control)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/MessageBox/MessageBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/MessageBoxExample.xaml`

## Description

A themed, drop-in replacement for `System.Windows.MessageBox`. Mirrors the full set of `Show` overloads and reuses the standard WPF dialog enums (`MessageBoxButton`, `MessageBoxImage`, `MessageBoxResult`, `MessageBoxOptions`). Honors the active Mosaic light/dark/high-contrast theme. The underlying dialog window is `MessageBoxWindow`.

## Usage

Switch an existing call site with a single `using` alias — no other code changes required:

```csharp
using MessageBox = Mosaic.UI.Wpf.Controls.MessageBox;

// Same signatures as System.Windows.MessageBox
MessageBox.Show("Operation complete.");
MessageBox.Show("Save changes?", "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

var result = MessageBox.Show(ownerWindow, "Delete this item?", "Confirm",
    MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);

if (result == MessageBoxResult.OK)
{
    // ...
}
```

## Overloads

All standard overloads are provided, including `Show(string)`, `Show(string, string)`, `Show(string, string, MessageBoxButton)`, `Show(string, string, MessageBoxButton, MessageBoxImage)`, `Show(string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult)`, `Show(string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult, MessageBoxOptions)`, plus `Window owner` variants of each.

## Notes

- This is a static helper, not a XAML control — call it from code-behind or a view model dialog service.
- The dialog automatically adopts the current Mosaic theme.
