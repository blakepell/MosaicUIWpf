# process — programs and windows

[Table of contents](README.md)

Use `process` to start programs, inspect running processes, and request changes to their windows. Process names are normally names without the `.exe` extension, such as `"notepad"`.

## Start a program or open a document

All `Start` overloads return no value. They start the operation and do not wait for the program to exit or return its process ID/output.

| Call | Behavior |
| --- | --- |
| `process.Start(filePath)` | Open using the Windows shell, with no arguments. |
| `process.Start(filePath, args)` | Open using the shell and a single argument string. |
| `process.Start(filePath, args, useShellExecute)` | Choose whether to use the shell. |
| `process.Start(filePath, args, useShellExecute, workingDirectory)` | Also set the working directory. |
| `process.Start(fileName, arguments)` | Start an executable with a .NET string array of separate arguments; uses direct process execution. |

`filePath`, `fileName`, `args`, and `workingDirectory` are strings. `useShellExecute` is a Boolean. Shell execution supports opening documents with their associated applications. With `false`, supply an executable. For a single `args` string, quote arguments containing spaces as required by the target program.

This example opens Notepad when run:

```javascript
process.Start("notepad.exe");
```

For the separate-arguments overload, use a typed .NET string array. The default LINQ helpers can create one from a script array. This example opens a specific file; replace the path first:

```javascript
let arguments = ["C:\\Reports\\monthly report.txt"]
    .GenericArguments(System.Object, string).Select(value => value)
    .GenericArguments(string).ToArray();
process.Start("notepad.exe", arguments);
```

## Find processes

| Call | Result |
| --- | --- |
| `process.GetProcessNames()` | .NET string array of names. |
| `process.GetProcessIds()` | .NET integer array of process IDs. |
| `process.GetProcesses()` | .NET array of Process objects, with properties such as `Id` and `ProcessName`. Dispose these objects when finished. |
| `process.GetProcessIdByName(name, exactMatch)` | First matching process ID, or `-1` if none is found. |
| `process.IsProcessRunning(processName, exactMatch)` | Whether any process matches. |

Both lookup methods require the Boolean `exactMatch`. With `true`, the comparison is a case-sensitive exact name. With `false`, it is a case-insensitive substring search. Multiple processes can have the same name. Separate list calls are separate snapshots; do not pair names and IDs by array index.

```javascript
let names = process.GetProcessNames();
log.Info("Running processes: " + names.Length);
for (let name of names) {
    log.Info(name);
}
```

Prefer the names/IDs helpers when that is all you need. To read paired values from the same objects:

```javascript
let processes = process.GetProcesses();
try {
    for (let item of processes) {
        log.Info(item.Id + ": " + item.ProcessName);
    }
} finally {
    for (let item of processes) {
        item.Dispose();
    }
}
```

A process can exit between enumeration and access, so process operations may throw errors.

## Focus, titles, and geometry

| Call | Result and behavior |
| --- | --- |
| `process.SetFocus(processId)` | No result. Request foreground focus for the process's main window. Invalid IDs can throw. |
| `process.SetFocus(processName)` | No result. Request focus for the first process with that name; do nothing if absent. |
| `process.WindowTitle(returnParent)` | String title of the active window; `true` requests the parent window, `false` the focused child window. |
| `process.GetPosition(id)` | Rectangle for the process's main window. Invalid IDs can throw. |
| `process.GetPosition(processName)` | Rectangle for the first matching process; all-zero rectangle if absent. |
| `ProcessScriptCommands.SetPosition(processId, x, y, width = 0, height = 0)` | No result. Move a main window; also resize it when both dimensions are nonzero. Requires the import below. |

IDs, coordinates, and dimensions are integers; `returnParent` is a Boolean. Returned rectangles have `X`, `Y`, `Width`, and `Height`. Coordinates come from Windows desktop APIs and can be negative on additional monitors. They are not coordinates inside the script editor.

`SetPosition` is currently a static method and is not callable as `process.SetPosition` in the default engine, even if it appears in completion. Import its namespace and call it on `ProcessScriptCommands`, as below. Omitting dimensions, or setting either dimension to zero, preserves the window's size.

Name-based focus/position calls use process-name lookup; they do not offer substring matching. A process without a usable main window can yield an all-zero rectangle, and focus/move requests do not report whether Windows accepted the change.

This example moves the first matching Notepad window when one is running:

```javascript
include Mosaic.UI.Wpf.Scripting.ScriptCommands;

let id = process.GetProcessIdByName("notepad", false);
if (id != -1) {
    let bounds = process.GetPosition(id);
    log.Info("Size: " + bounds.Width + " x " + bounds.Height);
    ProcessScriptCommands.SetPosition(id, 100, 100);
}
```

See [ui.SendKeys](ui.md#keyboard-input) for keyboard automation. A focus request can be denied by Windows; it is not proof that subsequent input will reach the intended window.
