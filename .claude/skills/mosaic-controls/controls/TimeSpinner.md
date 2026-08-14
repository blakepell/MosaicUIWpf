# TimeSpinner

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/TimeSpinner/TimeSpinner.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/TimeSpinnerExample.xaml`

## Description

A WinUI-style time-of-day picker using three coordinated scroll wheels (hour, minute, AM/PM) instead of a text field. This is the twelve-hour sibling of [DateSpinner](DateSpinner.md) and shares its interaction model exactly, reusing `DateSpinnerSelector` as its wheel.

While the drop-down is open the control edits a *temporary* time, so opening and closing without applying leaves `SelectedTime` untouched (subject to `CommitMode` and `LightDismissBehavior`). Values outside the range, and minutes that fall off `MinuteInterval`, are coerced.

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_Root` | `FrameworkElement` | The closed entry surface. |
| `PART_Popup` | `Popup` | The wheel drop-down. |
| `PART_HourSelector` / `PART_MinuteSelector` / `PART_MeridiemSelector` | `DateSpinnerSelector` | The three wheels. |
| `PART_ApplyButton` / `PART_CancelButton` / `PART_ClearButton` | `ButtonBase` | Commit, cancel, and clear buttons. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedTime` | `TimeOnly?` | `null` | The selected time, normalized to whole minutes. Two-way by default; accepts a XAML literal such as `8:30 AM`. |
| `SelectedTimeSpan` | `TimeSpan?` | `null` | `TimeSpan` projection of `SelectedTime`, for view models that prefer it. Two-way by default. |
| `MinimumTime` | `TimeOnly` | `00:00` | Earliest selectable time, inclusive. |
| `MaximumTime` | `TimeOnly` | `23:59` | Latest selectable time, inclusive. Coerced to be ≥ `MinimumTime`. |
| `MinuteInterval` | `int` | `1` | Step between offered minutes (e.g. `15`). The step restarts every hour. |
| `CommitMode` | `TimeSpinnerCommitMode` | `Explicit` | `Explicit` shows Apply/Cancel; `Immediate` writes every wheel change straight through. |
| `LightDismissBehavior` | `TimeSpinnerLightDismissBehavior` | `Cancel` | What clicking outside an open drop-down does: `Cancel` or `Apply`. |
| `IsMinuteVisible` | `bool` | `true` | Hiding the minute field turns the control into an hour picker. |
| `MeridiemDisplayMode` | `TimeSpinnerMeridiemDisplayMode` | `Culture` | `Culture`, `UpperCase`, or `LowerCase` AM/PM designators. |
| `HourFormat` / `MinuteFormat` | `string?` | `null` | Optional .NET time format strings (e.g. `hh`). Invalid formats fall back to the default. |
| `Culture` | `CultureInfo?` | `null` | Supplies designators and number formatting. Defaults to `CurrentUICulture`. Unlike `DateSpinner`, culture does not reorder fields. |
| `Header` / `HeaderTemplate` | `object?` / `DataTemplate?` | `null` | Optional header above the entry surface. |
| `HourPlaceholderText` / `MinutePlaceholderText` / `MeridiemPlaceholderText` | `string?` | `null` | Placeholder text per field; falls back to the theme resource keys below. |
| `IsClearButtonVisible` | `bool` | `false` | Shows a clear button on the entry surface (enabled only while a time is selected). |
| `IsReadOnly` | `bool` | `false` | Keeps the value visible and the control focusable, but blocks editing. |
| `ItemHeight` | `double` | `30` | Height of one wheel entry. |
| `VisibleItemCount` | `int` | `5` | Entries shown per wheel; coerced to an odd number ≥ 3. |
| `IsScrollAnimationEnabled` | `bool` | `true` | Wheels ease into position via `InertiaScrollViewer`. Set `false` for reduced motion. |

### Read-only (template) properties

`IsDropDownOpen`, `HourItems`, `MinuteItems`, `MeridiemItems` (`IReadOnlyList<DateSpinnerItem>`), `HourText`, `MinuteText`, `MeridiemText`, `HasSelectedTime`, `MinuteColumnWidth` (collapses to zero when `IsMinuteVisible` is false), and `EffectiveCulture`.

## Events

| Event | Args | Description |
|---|---|---|
| `SelectedTimeChanged` | `TimeSpinnerTimeChangedEventArgs` (bubbling) | Raised when `SelectedTime` changes to a different minute. Carries `OldTime` and `NewTime`. |
| `DropDownOpened` / `DropDownClosed` | `RoutedEventArgs` (bubbling) | Raised after the drop-down opens / closes, however it was closed. |

## Commands and Methods

| Member | Description |
|---|---|
| `OpenCommand`, `CloseCommand`, `ClearCommand`, `ApplyCommand`, `CancelCommand` (`static RoutedUICommand`) | Bound by the default template to the entry surface and buttons. |
| `Open()` / `Close()` | Opens / closes the drop-down without committing. |
| `Apply()` / `Cancel()` | Commits the temporary time / restores the time selected when the drop-down opened, then closes. |
| `Clear()` | Sets `SelectedTime` to `null`. |

## Theme resource keys

`HourPlaceholderTextKey`, `MinutePlaceholderTextKey`, `MeridiemPlaceholderTextKey`, `ApplyButtonTextKey`, `CancelButtonTextKey`, `ClearButtonTextKey` — `ComponentResourceKey` values you can override in a merged dictionary to localize the default strings.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:TimeSpinner
    Header="Reminder"
    MinuteInterval="15"
    MinimumTime="6:00 AM"
    MaximumTime="10:00 PM"
    IsClearButtonVisible="True"
    SelectedTime="{Binding ReminderTime, Mode=TwoWay}"
    SelectedTimeChanged="TimeSpinner_SelectedTimeChanged" />
```

## Notes

- Assigning a value that normalizes to the same minute does not raise `SelectedTimeChanged`.
- Hour wheel entries are ordered 12, 1…11 so scrolling down moves forward in time.
- A `TimeSpinnerAutomationPeer` exposes the control to UI Automation.
- For dates use [DateSpinner](DateSpinner.md) or [DatePicker](DatePicker.md).
