# environ — folders, paths, and computer information

[Table of contents](README.md)

Use `environ` to find paths and inspect the environment in which the application is running. These members are methods, so include `()` even when reading a single value.

## Folders and paths

| Call | Result |
| --- | --- |
| `environ.DesktopFolder()` | Current user's physical desktop folder path. |
| `environ.DocumentsFolder()` | Current user's Documents folder path. |
| `environ.ApplicationDataFolder()` | Current user's roaming application-data folder path. |
| `environ.LocalApplicationDataFolder()` | Current user's local application-data folder path. |
| `environ.CommonApplicationDataFolder()` | Shared application-data folder path. |
| `environ.CurrentDirectory()` | Application process's current working directory. |
| `environ.PathCombine(path1, path2)` | Combined path string. |
| `environ.PathCombine(path1, path2, path3)` | Combined path string from three parts. |
| `environ.LogicalDriveList()` | .NET string array of logical drive paths, with `Length` and zero-based indexing. |

Folder calls return strings and do not create folders. A special folder can be unavailable and return an empty path. The current directory is not necessarily the application installation folder or the script's folder. `PathCombine` joins strings; it does not check existence. A later absolute path replaces earlier path components.

```javascript
let documents = environ.DocumentsFolder();
if (documents != "") {
    let path = environ.PathCombine(documents, "Reports", "summary.txt");
    ui.ShowString(path);
}
```

## Computer and process information

| Call | Result |
| --- | --- |
| `environ.MachineName()` | Computer name string. |
| `environ.Username()` | Username for the account running the application. |
| `environ.UserDomainName()` | Domain associated with that account. |
| `environ.ProcessId()` | Integer process ID of this application. |
| `environ.TickCount()` | Signed 32-bit elapsed milliseconds since system startup; wraps periodically. |
| `environ.HasShutdownStarted()` | Boolean indicating whether this application's runtime is shutting down. |

`TickCount` is not a calendar timestamp or a .NET date tick count. `HasShutdownStarted` is not a general Windows shutdown-status query.

```javascript
log.Info("User: " + environ.UserDomainName() + "\\" + environ.Username());
log.Info("Computer: " + environ.MachineName());
log.Info("Application PID: " + environ.ProcessId());
```

## Environment variables

| Call | Result | Behavior |
| --- | --- | --- |
| `environ.GetEnvironmentVariable(variableName)` | String | Read a process environment variable; return `""` if absent. |
| `environ.SetEnvironmentVariable(variableName, value)` | None | Set a process environment variable. Both arguments are strings. |

Changes apply to the running application process and subsequently launched child processes. They do not persist a Windows user or machine setting, and they can affect other scripts in the same application.

```javascript
let tempFolder = environ.GetEnvironmentVariable("TEMP");
if (tempFolder != "") {
    ui.ShowString(environ.PathCombine(tempFolder, "mosaic-report.txt"));
}
```
