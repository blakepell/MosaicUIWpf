# MarkdownViewer

A lookless, WPF-native Markdown viewer that renders Markdown text into a FlowDocument hosted in a read-only RichTextBox so formatted content can be selected and copied as rich text. Markdown can be supplied directly or loaded from a WPF resource/local file with `Source`; relative Markdown links navigate in place and `GoBack()` returns to the previous document.

```xml
<mosaic:MarkdownViewer Source="/MyApp;component/Docs/index.md" />
```

Files loaded from an application pack URI must use the WPF `Resource` build action.

Hold `Ctrl` and turn the mouse wheel to zoom the document from 8 through 32 pixels. Each
wheel notch changes the base size by one pixel and scales headings proportionally.

![MarkdownViewer](./images/MarkdownViewer.png)

