# MarkdownViewer

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/MarkdownViewer/MarkdownViewer.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/MarkdownViewerExample.xaml`

## Description

A lookless, WPF-native Markdown viewer. It renders the `Markdown` string into a `FlowDocument` hosted by a read-only `RichTextBox`, so formatted content can be selected and copied as rich text. Rendering is defensive: invalid Markdown falls back to plain text instead of throwing.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Markdown` | `string` | `string.Empty` | Markdown source text to render. |
| `IsCopyEnabled` | `bool` | `true` | Enables selection/copy interaction in the hosted `RichTextBox`. |
| `IsDocumentReadOnly` | `bool` | `true` | Sets the hosted document read-only state. |

## XAML Example

```xml
<mosaic:MarkdownViewer
    FontSize="14"
    Markdown="{Binding PreviewMarkdown}" />
```

## Notes

- Template part: `PART_RichTextBox`.
- Hyperlinks are opened through hit-testing inside the read-only `RichTextBox`.
- The rendered document inherits typography and foreground from the control/template so it follows Mosaic theme resources.
