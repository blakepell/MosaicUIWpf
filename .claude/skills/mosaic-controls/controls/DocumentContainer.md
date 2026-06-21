# DocumentContainer

**Base class:** `TabControl`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/DocumentContainer/DocumentContainer.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/DocumentContainerExample.xaml`

## Description

Displays an observable collection of closable, reorderable documents as tabs — an IDE-style document well. Each tab has a close affordance, and tabs can be reordered by drag.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `Documents` | `IEnumerable` | The collection of documents bound as tabs. |
| `ActiveDocument` | `object` | The currently selected/active document (two-way). |
| `HeaderContent` | `object` | Optional content shown in the tab strip header area. |
| `HeaderContentTemplate` | `DataTemplate` | Template for `HeaderContent`. |
| `HeaderContentTemplateSelector` | `DataTemplateSelector` | Selector for `HeaderContent`. |
| `DocumentClosingCommand` | `ICommand` | Invoked when a document is about to close (cancellable). |
| `ActiveTabBackground` | `Brush` | Background of the active tab. |
| `ActiveTabForeground` | `Brush` | Foreground of the active tab. |
| `TabOverflowMode` | enum | How tabs behave when the strip overflows. |

## Events

`DocumentClosing` (`DocumentClosingEventArgs`, cancellable) and `DocumentClosed` (`DocumentClosedEventArgs`).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:DocumentContainer
    Documents="{Binding OpenDocuments}"
    ActiveDocument="{Binding CurrentDocument, Mode=TwoWay}"
    DocumentClosingCommand="{Binding ConfirmCloseCommand}" />
```

## Notes

- Derives from `TabControl`, so item templating and selection work as expected.
- Handle `DocumentClosing` (or the command) to prompt for unsaved changes and cancel the close.
