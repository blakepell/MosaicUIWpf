# clipboard — clipboard text

[Table of contents](README.md)

Use `clipboard` to exchange text with other applications. Writing replaces the clipboard contents. Calls handle the application's UI thread automatically.

## Reference

| Call | Result | Behavior |
| --- | --- | --- |
| `clipboard.SetText(text)` | None | Write text using the Windows Text clipboard format. |
| `clipboard.SetUnicodeText(text)` | None | Write Unicode text. Use this for general text, including non-ASCII characters. |
| `clipboard.SetHtml(text)` | None | Write the supplied string using the HTML clipboard format. |
| `clipboard.GetText()` | String | Read clipboard text; return an empty string when text is unavailable. |
| `clipboard.ContainsText()` | Boolean | Test whether clipboard text is available. |
| `clipboard.Flush()` | None | Keep clipboard data available after the application that set it exits. |

All `text` arguments are strings. `SetHtml` passes the supplied text through unchanged: it does not build Windows HTML clipboard headers, add a plain-text fallback, or convert Markdown. Supply an HTML clipboard payload suitable for the destination application. `GetText` is a text reader, not an HTML reader.

## Copy a result

```javascript
let encoded = hash.EncodeBase64("Hello from Mosaic");
clipboard.SetUnicodeText(encoded);
clipboard.Flush();
ui.Alert("The encoded text is on the clipboard.");
```

## Inspect clipboard text

```javascript
if (clipboard.ContainsText()) {
    let text = clipboard.GetText();
    ui.ShowString(text);
} else {
    ui.Alert("The clipboard does not contain text.");
}
```

Another application can change or lock the clipboard between calls. Clipboard access errors can be handled with `try...catch` as shown in [Using the commands](Using-Commands.md).
