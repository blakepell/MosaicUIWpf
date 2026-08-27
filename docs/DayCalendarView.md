# DayCalendarView

`DayCalendarView` displays a selected day as a vertically scrolling, 24-hour timeline. Events are clipped to the selected day, positioned at exact minute coordinates, and assigned to side-by-side columns only when their real time intervals overlap.

```xml
<mosaic:DayCalendarView
    ItemsSource="{Binding Events}"
    SelectedDate="{Binding SelectedDate}"
    EventCommand="{Binding OpenEventCommand}"
    EventTimeChangedCommand="{Binding ChangeEventTimeCommand}"
    HourHeight="80" />
```

The built-in `CalendarEvent` model uses `StartDate`, `EndDate`, `Title`, `Description`, `Background`, and `IsReadOnly`. Application-specific objects can be used by setting `StartDatePath`, `EndDatePath`, `TitlePath`, `DescriptionPath`, `BackgroundPath`, and `IsReadOnlyPath`. An `EventTemplate` receives the original source object as its data context. A missing read-only property is treated as `false`; setting it to `true` prevents that event from being moved while retaining click and keyboard activation.

Dragging is proposal-based: the control restores its visual after the pointer is released, then raises `EventTimeChanged` and executes `EventTimeChangedCommand` with a `CalendarEventTimeChangedEventArgs`. The view model accepts a move by updating its source object. This keeps persistence, validation, and rejection in application code.

## Time positioning

The timeline uses a continuous pixels-per-minute scale:

```text
pixelsPerMinute = HourHeight / 60
top              = minutes from the selected day's midnight * pixelsPerMinute
height           = displayed duration in minutes * pixelsPerMinute
```

An event crossing midnight retains its real start and end values but is visually clipped to the half-open selected-day interval `[midnight, next midnight)`.

## Overlap layout

Visible intervals are sorted by start time and divided into overlap clusters. Within each cluster, each event is placed in the first column whose last event has already ended. The maximum number of occupied columns becomes the cluster's column count, and each event receives an equal share of the available event-area width. Endpoints use half-open interval semantics, so an event ending at 10:00 AM does not overlap one starting at 10:00 AM.

The example in `MosaicWpfDemo` includes single, two-column, three-column, chained partial-overlap, irregular-minute, and cross-midnight cases.
