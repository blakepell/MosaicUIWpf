# Mosaic script command reference

These are the pre-packaged commands available to JavaScript in the Mosaic script control's default environment. Call them directly, for example `ui.Alert("Hello")` or `hash.MD5("Hello")`. You do not need an `include` statement or a new command object.

## Table of contents

- [Using the commands](Using-Commands.md) — naming, returned objects, async calls, errors, and other built-in names.
- [ui](ui.md) — alerts, confirmation, text input and output, toast notifications, pauses, and keyboard input.
- [hash](hash.md) — hashes, checksums, Base64, and URL encoding.
- [clipboard](clipboard.md) — read and write clipboard text and HTML.
- [http](http.md) — HTTP requests and URL encoding.
- [regex](regex.md) — pattern matching and simple alternatives lists.
- [environ](environ.md) — folders, paths, environment variables, and computer information.
- [log](log.md) — application log messages and bulk updates.
- [process](process.md) — start programs, find processes, and work with their windows.
- [mouse](mouse.md) — pointer position, buttons, dragging, and scrolling.
- [screenshot](screenshot.md) — capture a window, screen, or rectangle and save an image.
- [ai](ai.md) — ask a local model, manage conversations, and supply context.

## First script

```javascript
let name = ui.InputBox("What is your name?");
if (name != "") {
    ui.Alert("Hello, " + name + "!");
    log.Info("Greeting displayed.");
}
```

This reference is for writing scripts inside an application. The application can add, replace, or omit commands, so its own documentation may describe additional names or different behavior. The pages here describe Mosaic's supplied implementations.
