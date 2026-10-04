# Using the commands

[Table of contents](README.md)

## Names and arguments

Command names are case-sensitive: use `ui.Alert`, `hash.MD5`, and `environ.Username`. The module name is usually lowercase; its methods generally begin with a capital letter. Call methods with parentheses, including methods with no arguments.

```javascript
let user = environ.Username();
let encoded = hash.EncodeBase64("Hello, " + user);
ui.ShowString(encoded);
```

Reference tables show JavaScript calls. A default such as `durationMs = 5000` means that argument can be omitted. Pass arguments in the order shown; to change a later optional argument, supply the earlier ones too. For example, `ui.ShowToast("Saved", "Done", "Success", 5000, "TopRight")` sets the corner.

Text arguments are strings, flags are `true` or `false`, and counts, coordinates, and milliseconds are integers. A result of **none** means the command is used for its effect, without a useful returned value.

## Returned .NET values

Commands call the application's C# objects. Returned values can be used directly in JavaScript, but they retain their .NET members. Strings have `Length`, `Trim()`, and `ToUpper()`. Returned .NET arrays have `Length`; arrays created with JavaScript `[]` have lowercase `length`.

```javascript
let drives = environ.LogicalDriveList();
log.Info("Drives: " + drives.Length);
for (let drive of drives) {
    log.Info(drive);
}
```

Use `for...of` to visit returned items. Use capitalized properties such as `rect.Width` or `bitmap.Height` for returned objects. A bitmap from `screenshot` needs `Dispose()` when you finish with it; see [screenshot](screenshot.md).

## Waiting and errors

Use `await` to get the result of an asynchronous call. Top-level `await` works in the script control.

```javascript
log.Info("Waiting...");
await ui.PauseAsync(250);
log.Info("Continuing.");
```

Dialogs wait for the user to close them. HTTP commands and `ui.Pause` block the script until they finish. The control runs scripts on a worker thread and handles the supplied UI commands' thread requirements for you.

Stop is cooperative. A command that is already inside a blocking operation can delay stopping. `ui.PauseAsync` and `ai.AskAsync` do not accept the control's stop token, so do not assume Stop immediately cancels their underlying work.

Commands can throw exceptions, for example for malformed input, inaccessible files, or failed requests. Catch an error and read its .NET `Message` property:

```javascript
try {
    let decoded = hash.DecodeBase64("not valid base64!");
    ui.ShowString(decoded);
} catch (error) {
    log.Error(error.Message);
}
```

## Other built-in names

The default control also supplies these helpers. They are not command modules in this reference, but examples may use them.

| Name | Purpose |
| --- | --- |
| `string`, `int`, `double` | .NET string and numeric helpers, such as `string.IsNullOrEmpty(text)` and `int.Parse(text)`. |
| `date`, `math`, `guid` | .NET date/time, mathematics, and identifiers, such as `date.Now`, `math.Round(value)`, and `guid.NewGuid()`. |
| `file`, `directory` | .NET file and directory operations, such as `file.ReadAllText(path)` and `directory.Exists(path)`. |
| `StringBuilder` | Construct with `new StringBuilder()` to build text. |
| `DataList` | Construct a table that can display and edit rows. |
| `JSON` | `JSON.parse(text)` and `JSON.stringify(value)` for JSON conversion. |
| `globals` | Shared application dictionary; values can outlive a single script run. |
| `globalThis` | Access to the script engine's global scope. |

The default environment also exposes System types and LINQ extension methods. The separate Mosaic language reference covers JavaScript syntax, `include`, LINQ, and differences from browser JavaScript. Application-supplied objects may expose more C# properties and methods using the same dot-call syntax.
