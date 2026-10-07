# log — application logging

[Table of contents](README.md)

Use `log` for progress and diagnostic messages. Messages go to the application's Mosaic log. Where the log is displayed and which severities are visible depend on the application; these calls do not open a log window or select an output file.

## Reference

All calls return no value. Each `msg` argument is a string.

| Call | Purpose |
| --- | --- |
| `log.Debug(msg)` | Diagnostic details; may be filtered out. |
| `log.Info(msg)` | Normal progress or informational message. |
| `log.Warning(msg)` | A condition that needs attention. |
| `log.Error(msg)` | A failure message. Logging it does not throw an exception or stop the script. |
| `log.Success(msg)` | Successful completion. |
| `log.BeginUpdate()` | Begin a bulk update of the log collection. |
| `log.EndUpdate()` | End the bulk update and allow the collection to refresh. |

## Report progress

```javascript
log.Info("Checking available drives...");
for (let drive of environ.LogicalDriveList()) {
    log.Info("Found " + drive);
}
log.Success("Drive check complete.");
```

## Batch many messages

Pair `BeginUpdate` with `EndUpdate` in `finally` so a script error does not leave the log in a bulk update. Keep the block short; the log is shared by the application.

```javascript
log.BeginUpdate();
try {
    for (let i = 1; i <= 20; i++) {
        log.Info("Item " + i);
    }
} finally {
    log.EndUpdate();
}
```

For a result that the user must immediately see, use [ui.Alert or ui.ShowString](ui.md).
