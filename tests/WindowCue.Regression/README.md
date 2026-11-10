Run on Windows with the .NET 10 SDK:

```powershell
dotnet run --project tests/WindowCue.Regression/WindowCue.Regression.csproj
```

This dependency-free regression executable pumps a real WPF dispatcher while simulated
native providers stall, timeout, recover, and fail. It also verifies confirmed closure,
inconclusive inspections, sleeping-tab identity, and the empty toolbar title. Picker checks
cover stalled monitoring, discovery taking longer than three seconds, progressive results,
selection preservation, provider failure, and closing during a stalled scan. It does not
launch or close browsers, register an AppBar, or change the desktop work area.

The toolbar sizing check creates its hidden native window and verifies the actual HWND
width matches the original 64-DIP compact buttons, catching native caption minimum widths.
