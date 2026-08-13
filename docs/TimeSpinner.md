# TimeSpinner

A nullable time-of-day picker with coordinated hour, minute, and AM/PM wheels. It is the time counterpart to [`DateSpinner`](./DateSpinner.md) and uses the same explicit Apply/Cancel interaction by default.

## Usage

```xml
<mosaic:TimeSpinner
    Header="Appointment time"
    SelectedTime="{Binding AppointmentTime, Mode=TwoWay}"
    MinimumTime="08:00"
    MaximumTime="17:30"
    MinuteInterval="15"
    IsClearButtonVisible="True" />
```

`SelectedTime` is a nullable `TimeOnly` that binds two way by default. It is normalized to whole minutes, constrained to `MinimumTime`..`MaximumTime`, and snapped to the nearest value offered by `MinuteInterval`. Time literals are supported directly in XAML.

Models that store a time of day as a `TimeSpan` can bind `SelectedTimeSpan` instead. It mirrors `SelectedTime`, so bind one or the other on an instance rather than both.

## Commit modes

| Mode | Behavior |
|---|---|
| `Explicit` (default) | Shows Apply and Cancel. Wheel changes remain temporary until Apply. Cancel, Escape, and by default light dismiss discard them. |
| `Immediate` | Writes every wheel change through immediately and hides the action buttons. |

`LightDismissBehavior` controls an outside click in explicit mode: `Cancel` (default) discards the temporary time, while `Apply` commits it. Opening and closing without moving a wheel never assigns an unset value.

## Range, intervals, and partial time display

`MinimumTime` and `MaximumTime` are inclusive. Out-of-range entries remain visible in the wheels but cannot be selected. `MaximumTime` is coerced up to `MinimumTime` when necessary.

`MinuteInterval` is coerced from 1 through 60. The step restarts each hour: `15` offers 00, 15, 30, and 45. Existing and incoming values are snapped to the nearest selectable time.

Set `IsMinuteVisible="False"` for an hour picker. The existing minute component is preserved; hour and AM/PM remain visible.

## Key properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedTime` | `TimeOnly?` | `null` | Selected time, two-way by default. |
| `SelectedTimeSpan` | `TimeSpan?` | `null` | Mirrored time measured from midnight for `TimeSpan`-based models. |
| `MinimumTime` / `MaximumTime` | `TimeOnly` | `00:00` / `23:59` | Inclusive selectable range. |
| `MinuteInterval` | `int` | `1` | Minute-wheel step, coerced to 1–60. |
| `IsDropDownOpen` | `bool` | `false` | Read only; use methods or routed commands to change it. |
| `CommitMode` | `TimeSpinnerCommitMode` | `Explicit` | When wheel changes reach the selected value. |
| `LightDismissBehavior` | `TimeSpinnerLightDismissBehavior` | `Cancel` | How clicking outside handles a temporary value. |
| `IsMinuteVisible` | `bool` | `true` | Shows or hides the minute field. |
| `MeridiemDisplayMode` | `TimeSpinnerMeridiemDisplayMode` | `Culture` | Uses culture-provided, uppercase, or lowercase AM/PM text. |
| `HourFormat` / `MinuteFormat` | `string?` | `null` | Optional .NET time format strings. Invalid formats fall back safely. |
| `Culture` | `CultureInfo?` | `null` | Formatting and AM/PM designators; falls back to `CurrentUICulture`. |
| `Header` / `HeaderTemplate` | `object?` / `DataTemplate?` | `null` | Optional label and template. A string header is also the accessible name. |
| `HourPlaceholderText` / `MinutePlaceholderText` / `MeridiemPlaceholderText` | `string?` | `null` | Unset-state labels, with localizable resource fallbacks. |
| `IsClearButtonVisible` | `bool` | `false` | Shows a clear action while a value is selected. |
| `IsReadOnly` | `bool` | `false` | Keeps the value focusable and readable but prevents changes. |
| `ItemHeight` / `VisibleItemCount` | `double` / `int` | `30` / `5` | Wheel metrics; visible count is coerced to an odd value of at least three. |
| `IsScrollAnimationEnabled` | `bool` | `true` | Enables wheel easing; disable to reduce motion. |

## Events, methods, and commands

| Event | Description |
|---|---|
| `SelectedTimeChanged` | Bubbling event carrying `OldTime` and `NewTime`. |
| `DropDownOpened` | Raised after the wheel drop-down opens. |
| `DropDownClosed` | Raised whenever the drop-down closes. |

`Open()`, `Close()`, `Apply()`, `Cancel()`, and `Clear()` have matching routed commands: `OpenCommand`, `CloseCommand`, `ApplyCommand`, `CancelCommand`, and `ClearCommand`.

## Keyboard and accessibility

When closed, `Enter`, `Space`, or `Alt`+`Down` opens the picker. When open, `Left` and `Right` move between wheels, wheel navigation keys change the active value, `Enter` applies, and `Escape` cancels. Focus returns to the entry surface on close.

`TimeSpinnerAutomationPeer` exposes ExpandCollapse and Value patterns. Its accessible value describes only the fields displayed by the control, and selection changes raise automation value-change events.

## Localization

Placeholders and action captions resolve from the explicit property, then a component resource, then a built-in English default. The resource IDs are `HourPlaceholderText`, `MinutePlaceholderText`, `MeridiemPlaceholderText`, `ApplyButtonText`, `CancelButtonText`, and `ClearButtonText`; corresponding `ComponentResourceKey` properties are exposed on `TimeSpinner`.
