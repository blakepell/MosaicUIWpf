# ui — dialogs, notifications, and timing

[Table of contents](README.md)

Use `ui` to communicate with the person running a script, pause execution, or send keyboard input.

## Dialogs

| Call | Result | Behavior |
| --- | --- | --- |
| `ui.Alert(message)` | None | Show an OK dialog titled **Alert**. |
| `ui.Confirm(message)` | Boolean | Show an OK/Cancel dialog titled **Confirm**; return `true` only for OK. |
| `ui.InputBox(message)` | String | Show the prompt above an editable text box. Return accepted text, or `""` on cancellation. |
| `ui.ShowString(text)` | None | Show text in a read-only, scrollable **Script Output** window. |

All four calls wait until the dialog closes. `InputBox` cannot distinguish cancellation from accepting empty text.

```javascript
let text = ui.InputBox("Enter text to encode:");
if (text != "") {
    if (ui.Confirm("Encode this text as Base64?")) {
        ui.ShowString(hash.EncodeBase64(text));
    }
}
```

## Toast notifications

`ui.ShowToast(title, message, severity = "Info", durationMs = 5000, quadrant = "BottomRight")` shows a notification without waiting for it to close. It returns no value.

| Argument | Values |
| --- | --- |
| `title` | Bold title text. |
| `message` | Body text. |
| `severity` | `"Success"`, `"Info"`, `"Warning"`, or `"Error"`. |
| `durationMs` | Positive milliseconds until dismissal; zero or negative keeps the toast open until closed. |
| `quadrant` | `"TopLeft"`, `"TopRight"`, `"BottomLeft"`, or `"BottomRight"`. |

Severity and corner names are case-insensitive. An invalid name throws an error. The application's main window must be loaded or another toast host must be available.

```javascript
ui.ShowToast("Script", "Work complete.");
ui.ShowToast("Saved", "Your report is ready.", "Success", 3000, "TopRight");
```

## Pauses and maintenance

| Call | Result | Behavior |
| --- | --- | --- |
| `ui.Pause(milliseconds)` | None | Block the script worker for the specified time. |
| `await ui.PauseAsync(milliseconds)` | No result after awaiting | Wait asynchronously for the specified time. |
| `ui.GarbageCollect()` | None | Request .NET garbage collection for the application. Usually unnecessary; it does not replace `Dispose()`. |

Use nonnegative milliseconds for normal delays. Neither pause accepts a cancellation argument.

```javascript
await ui.PauseAsync(500);
ui.Alert("Half a second has elapsed.");
```

## Keyboard input

`ui.SendKeys(keys)` sends Windows Forms SendKeys input to the focused application/control and returns no value. It does not choose or verify the target. The string uses SendKeys notation, not JavaScript key events.

| Text | Meaning |
| --- | --- |
| `"Hello"` | Type text. |
| `"{ENTER}"`, `"{TAB}"` | Press a named key. |
| `"^a"` | Ctrl+A. |
| `"+{TAB}"` | Shift+Tab. |
| `"%{F4}"` | Alt+F4. |
| `"{+}"` | Type a literal plus sign. |

For example, the following waits while you focus the intended text field, then types a greeting and presses Enter. Running it changes the focused application.

```javascript
await ui.PauseAsync(3000);
ui.SendKeys("Hello{ENTER}");
```

See [process](process.md) for requesting window focus and [mouse](mouse.md) for pointer input.
