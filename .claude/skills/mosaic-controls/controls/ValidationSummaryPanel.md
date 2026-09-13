# ValidationSummaryPanel

**Base class:** `ItemsControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/ValidationSummaryPanel/ValidationSummaryPanel.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/ValidationSummaryPanelExample.xaml`

## Description

An aggregating validation display that watches a target `FrameworkElement` (a form or panel) for WPF validation errors (`Validation.Errors`) on all descendant controls. Displays a scrollable list of error messages and optionally scrolls/focuses the offending field when clicked.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Target` | `FrameworkElement?` | `null` | The root element to scan for validation errors. If unset, the panel tries the nearest parent container. |
| `Title` | `string` | `"Validation Errors"` | Heading text displayed above the error list. |
| `IconGeometry` | `Geometry?` | `null` | Icon shown next to each error. |
| `IconBrush` | `Brush` | `Brushes.Red` | Fill of the error icon. |
| `HeaderBackground` | `Brush` | `#FEE2E2` | Background of the header area. |
| `Errors` | `ObservableCollection<ValidationError>` (read-only) | empty | The current errors. It is also the control's `ItemsSource`. |
| `HasErrors` | `bool` (read-only) | `false` | `true` when one or more validation errors exist. |
| `ErrorCount` | `int` (read-only) | `0` | Number of current validation errors. |
| `AutoHideWhenValid` | `bool` | `true` | When `true`, the control is collapsed when `HasErrors` is `false`. |
| `FocusControlOnClick` | `bool` | `true` | When `true`, clicking an error item focuses the offending control. |

## Methods

| Method | Description |
|---|---|
| `Refresh()` | Re-scans `Target` for current validation errors. |
| `AddError(string message, string propertyName = "", FrameworkElement? sourceControl = null)` | Manually adds an error (e.g., from server-side validation). |
| `ClearAndRefresh()` | Clears manually added errors and re-scans `Target`. |
| `FocusError(ValidationError error)` | Focuses the error's `SourceControl`. |

`FocusErrorCommand` (static `RoutedCommand`) takes a `ValidationError` parameter. Error item templates use it to call `FocusError`.

## ValidationError

This is Mosaic's own error model, `Mosaic.UI.Wpf.Controls.ValidationError`. It is not `System.Windows.Controls.ValidationError`, so qualify the name if both namespaces are imported.

| Property | Type | Description |
|---|---|---|
| `Message` | `string` | Error text. |
| `PropertyName` | `string` | Bound property name, if known. |
| `SourceControl` | `FrameworkElement?` | Control that owns the error, used by `FocusError`. |

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<StackPanel x:Name="MyForm">

    <TextBox Text="{Binding Name, ValidatesOnDataErrors=True, NotifyOnValidationError=True}" />
    <TextBox Text="{Binding Email, ValidatesOnDataErrors=True, NotifyOnValidationError=True}" />

    <Button Content="Submit" Click="Submit_Click" />

    <mosaic:ValidationSummaryPanel
        Target="{x:Reference MyForm}"
        Title="Please fix the following errors:"
        AutoHideWhenValid="True"
        FocusControlOnClick="True" />
</StackPanel>
```

## Code-Behind

```csharp
private void Submit_Click(object sender, RoutedEventArgs e)
{
    validationPanel.Refresh();

    if (validationPanel.HasErrors)
        return; // show the summary

    // proceed with save
}
```

## Notes

- Implements `INotifyPropertyChanged` for `HasErrors` and `ErrorCount`.
- Works with any `IDataErrorInfo` or `INotifyDataErrorInfo` view model.
- `AddError()` can be called after server-side calls to display backend error messages inside the same panel.
- `AutoHideWhenValid="False"` keeps the panel visible (with title) even when there are no errors.
