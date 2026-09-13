# DayCalendarView

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/DayCalendarView/DayCalendarView.cs` + `DayCalendarView.xaml`  
**Example:** `src/MosaicWpfDemo/Views/Examples/DayCalendarViewExample.xaml`

## Description

An Outlook-style single-day calendar. It shows a scrollable 24-hour timeline and places each event by its exact start and end time. Events that overlap are split into side-by-side columns, and freed columns are reused within an overlap cluster. Events that start before the displayed day or end after it are clipped to the day.

The control reads your own event objects through **property paths** (`StartDatePath`, `TitlePath`, …), so any POCO or view model works. You can also use the built-in `CalendarEvent` class, which matches the default paths. **Dragging never writes to the source object.** A drag raises `EventTimeChanged` and runs `EventTimeChangedCommand` with a proposed new time, and your code applies the change. Pressing Delete on a focused event raises `EventDeleting`. The control only removes the item itself when `ItemsSource` is a mutable `IList`.

The default style uses theme tokens for colors: `ControlBackgroundBrush`, `ControlHoverBackgroundBrush` (event cards), `AccentBrush` (event border and current-time line), `ControlSeparatorBrush`/`ControlBorderBrush` (hour and quarter-hour lines), `ControlTextSecondaryForegroundBrush` (time labels) and `ControlBackgroundLightBrush` (time column).

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedDate` | `DateTime` | `DateTime.Today` | The day displayed (time part is ignored). |
| `ItemsSource` | `IEnumerable?` | `null` | Event objects. `INotifyCollectionChanged` and per-item `INotifyPropertyChanged` are observed. |
| `SelectedItem` | `object?` | `null` | Selected source event. Two-way by default. Set on click or at drag start. |
| `EventTemplate` | `DataTemplate?` | `null` | Custom card content. Its `DataContext` is the original source object. |
| `EventCommand` | `ICommand?` | `null` | Runs with the source event when it is clicked (without dragging), or when Enter/Space is pressed on it. |
| `EventTimeChangedCommand` | `ICommand?` | `null` | Runs with a `CalendarEventTimeChangedEventArgs` proposal after a drag. |
| `EventDeletingCommand` | `ICommand?` | `null` | Runs with a `CalendarEventDeletingEventArgs` after `EventDeleting`. It can set `Cancel`. |
| `StartDatePath` / `EndDatePath` | `string` | `"StartDate"` / `"EndDate"` | Paths to the `DateTime` start (inclusive) and end (exclusive). |
| `TitlePath` / `DescriptionPath` | `string` | `"Title"` / `"Description"` | Paths to the title and optional secondary text. |
| `BackgroundPath` | `string` | `"Background"` | Path to an optional per-event `Brush` that overrides `EventBackground`. |
| `IsReadOnlyPath` | `string` | `"IsReadOnly"` | `true` blocks dragging. Missing or non-bool values count as `false`. |
| `CanDeletePath` | `string` | `"CanDelete"` | `false` blocks Delete. Missing or non-bool values count as `true`. |
| `HourHeight` | `double` | `80` | Pixels per hour (must be > 0). |
| `TimeColumnWidth` | `double` | `72` | Width of the time-label column. |
| `EventSpacing` | `double` | `3` | Gap between overlapping event columns. |
| `AllowEventDragging` | `bool` | `true` | Enables vertical drag-to-reschedule. |
| `AllowCrossDayEvents` | `bool` | `false` | Lets a dragged event end after midnight. When `false`, events longer than one day can't be dragged. |
| `DragSnapInterval` | `TimeSpan` | `00:15:00` | Snap step for drag proposals (> 0 and ≤ 1 day). |
| `InitialScrollTime` | `TimeSpan` | `08:00:00` | Time scrolled to the top when the control loads. |
| `ScrollToInitialTimeOnLoad` | `bool` | `true` | Whether to apply `InitialScrollTime` when the control loads. |
| `ShowQuarterHourLines` | `bool` | `true` | Draws 15-minute grid lines. |
| `ShowCurrentTimeIndicator` | `bool` | `true` | Draws a "now" line when `SelectedDate` is today. |
| `EventBackground` / `EventBorderBrush` | `Brush` | theme hover bg / accent | Default event card brushes. |
| `EventBorderThickness` / `EventCornerRadius` / `EventPadding` | `Thickness` / `CornerRadius` / `Thickness` | `1` / `0` / `8,5,8,5` | Default event card shape. |
| `HourLineBrush` / `QuarterHourLineBrush` | `Brush` | theme separator / border | Grid line brushes. |
| `CurrentTimeBrush` | `Brush` | theme accent | Current-time line and drag guide lines. |
| `TimeForeground` / `TimeColumnBackground` | `Brush` | theme secondary text / light bg | Time-label column styling. |

## Events

| Event | Args | Description |
|---|---|---|
| `EventTimeChanged` | `CalendarEventTimeChangedEventArgs` (bubbling) | A drag finished. Carries `Event`, `OldStart`, `OldEnd`, `NewStart`, `NewEnd`. The source is **not** modified. |
| `EventDeleting` | `CalendarEventDeletingEventArgs` (bubbling) | Delete was requested. Carries `Event`. Set `Cancel = true` to keep the event. |

## Methods

| Member | Description |
|---|---|
| `ScrollToTime(TimeSpan time)` | Scrolls so `time` is near the top edge. |
| `ScrollToCurrentTime()` | Scrolls to the current local time. |
| `bool DeleteEvent(object calendarEvent)` | Raises `EventDeleting`, runs `EventDeletingCommand`, then removes the item from `ItemsSource` unless cancelled. Removal only happens when the source is a mutable `IList`. Returns `false` if cancelled. |

## CalendarEvent (ready-made model)

An `ObservableObject` whose property names match the default paths: `StartDate`, `EndDate`, `Title` (defaults to `""`), `Description`, `Background` (`Brush?`), `IsReadOnly` (default `false`) and `CanDelete` (default `true`).

## Interaction

| Input | Result |
|---|---|
| Click (no drag) | Selects the event and runs `EventCommand`. |
| Vertical drag | Shows a snapped preview with guide lines. Releasing raises `EventTimeChanged` and runs `EventTimeChangedCommand`. |
| Escape during drag / lost capture | Cancels the drag. Nothing is raised. |
| Enter / Space on focused event | Activates it, the same as a click. |
| Delete on focused event | Calls `DeleteEvent` when the event's `CanDelete` is true. |

## XAML Example

```xml
xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"

<!-- Custom model: map its property names through the *Path properties -->
<mosaic:DayCalendarView
    ItemsSource="{Binding Events}"
    SelectedDate="{Binding SelectedDate}"
    SelectedItem="{Binding SelectedEvent}"
    StartDatePath="Begin"
    EndDatePath="Finish"
    TitlePath="Subject"
    DescriptionPath="Location"
    BackgroundPath="Color"
    DragSnapInterval="00:15:00"
    InitialScrollTime="07:30:00"
    EventCommand="{Binding OpenEventCommand}"
    EventTimeChangedCommand="{Binding ChangeEventTimeCommand}" />
```

```csharp
[RelayCommand]
private void ChangeEventTime(CalendarEventTimeChangedEventArgs? proposal)
{
    if (proposal?.Event is not DayCalendarDemoEvent e) return;
    e.Begin = proposal.NewStart;   // the calendar only proposes; apply it yourself
    e.Finish = proposal.NewEnd;
}
```

## Notes

- Template parts: `PART_ScrollViewer` (`ScrollViewer`) and `PART_TimelinePanel` (`DayTimelinePanel`).
- `DayTimelinePanel` (`Panel`) is public so a re-template can use it. It lays out events by time and draws the grid, time labels, current-time line and drag guides. It exposes the same timeline properties (`HourHeight`, `TimeColumnWidth`, `EventSpacing`, `ShowQuarterHourLines`, line/time brushes, `FontFamily`, `FontSize`). `DayCalendarView` normally sets these on it.
- `CalendarEventPresenter` (`ContentControl`) is the container for each event, styled in `DayCalendarView.xaml`. It has read-only `IsSelected`, `IsReadOnly` and `CanDelete`, plus `DisplayTitle`, `DisplayDescription`, `DisplayTimeText` and `CornerRadius` for the default template.
- Automation: the calendar is exposed as `List`, and each event presenter supports the Invoke pattern.
- Property-path accessors are cached per type and path. Changes to observable items re-lay out the timeline.
- For choosing a date, pair with [DatePicker.md](DatePicker.md) or [DateSpinner.md](DateSpinner.md) bound to `SelectedDate`.
