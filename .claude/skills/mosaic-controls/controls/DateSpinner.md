# DateSpinner

**Base class:** `Control`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/DateSpinner/DateSpinner.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/DateSpinnerExample.xaml`  
**Full docs:** `docs/DateSpinner.md`

## Description

A date entry control that chooses a date with three coordinated scrolling wheels for month, day, and year rather than with a calendar grid, in the style of the WinUI 3 date picker. The closed control is a compact entry surface with one field per component; clicking anywhere on it opens a drop down containing the wheels.

**Not the same control as `DatePicker`**, which uses a popup calendar grid. Pick `DateSpinner` for dates that are easier to dial in than to find on a calendar — a date of birth, an expiration month and year, a model year.

## Companion Types

| Type | Purpose |
|---|---|
| `DateSpinnerSelector` | The individual wheel (`ListBox` subclass). Virtualized, pixel-scrolling, centred selection band. Usable on its own. |
| `DateSpinnerSelectorItem` | The wheel's item container (`ListBoxItem` subclass). |
| `DateSpinnerItem` | One wheel value: `Value`, `DisplayText`, `IsSelectable`, `IsSpacer`. |
| `DateSpinnerCalendar` | Public static, pure date arithmetic and formatting. Independently testable. |
| `DateSpinnerDateChangedEventArgs` | `OldDate` / `NewDate` for `SelectedDateChanged`. |
| `DateSpinnerField`, `DateSpinnerCommitMode`, `DateSpinnerMonthDisplayMode`, `DateSpinnerLightDismissBehavior` | Enums. |

## Template Parts

| Part | Type | Description |
|---|---|---|
| `PART_Root` | `FrameworkElement` | The closed entry surface. Clicking it toggles the drop down. |
| `PART_Popup` | `Popup` | The drop down. `Placement="Bottom"`, flips above when space is short. |
| `PART_MonthSelector` / `PART_DaySelector` / `PART_YearSelector` | `DateSpinnerSelector` | The three wheels. |
| `PART_ApplyButton` / `PART_CancelButton` | `ButtonBase` | Shown only in `Explicit` commit mode. |
| `PART_ClearButton` | `ButtonBase` | On the entry surface, shown when `IsClearButtonVisible`. |

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedDate` | `DateTime?` | `null` | Binds two way by default. Normalized to midnight, coerced into range. |
| `MinimumDate` / `MaximumDate` | `DateTime` | today ∓ 100 years | Inclusive bounds. `MaximumDate` is coerced up to `MinimumDate` when set below it. |
| `IsDropDownOpen` | `bool` | `false` | **Read only.** Use `Open()`/`Close()` or the routed commands. |
| `CommitMode` | `DateSpinnerCommitMode` | `Explicit` | `Explicit` (Apply/Cancel buttons) or `Immediate` (write-through). |
| `LightDismissBehavior` | `DateSpinnerLightDismissBehavior` | `Cancel` | What clicking outside does with the temporary date. |
| `IsMonthVisible` / `IsDayVisible` / `IsYearVisible` | `bool` | `true` | At least one is always coerced back to visible. |
| `MonthDisplayMode` | `DateSpinnerMonthDisplayMode` | `FullName` | `FullName`, `AbbreviatedName`, `Numeric`, `TwoDigitNumeric`. |
| `MonthFormat` / `DayFormat` / `YearFormat` | `string?` | `null` | Optional .NET date formats; override `MonthDisplayMode`. |
| `Culture` | `CultureInfo?` | `null` | Month names, formatting, **and field order**. Falls back to `CurrentUICulture`. |
| `Header` / `HeaderTemplate` | `object?` / `DataTemplate?` | `null` | Label above the surface. A string header becomes the accessible name. |
| `MonthPlaceholderText` / `DayPlaceholderText` / `YearPlaceholderText` | `string?` | `null` | Fall back to theme resources (see Localization). |
| `IsClearButtonVisible` | `bool` | `false` | Clear button, enabled only while a date is selected. |
| `IsReadOnly` | `bool` | `false` | Visible and focusable, but cannot open or change. |
| `ItemHeight` / `VisibleItemCount` | `double` / `int` | `30` / `5` | Wheel metrics. `VisibleItemCount` coerced to odd, ≥ 3. |
| `IsScrollAnimationEnabled` | `bool` | `true` | Wheel easing. Set `false` to reduce motion. |

Read-only presentation properties the template binds to: `MonthItems`/`DayItems`/`YearItems`, `MonthText`/`DayText`/`YearText`, `HasSelectedDate`, `MonthFieldIndex`/`DayFieldIndex`/`YearFieldIndex`, `MonthSeparatorVisibility`/`DaySeparatorVisibility`/`YearSeparatorVisibility`.

## Events

| Event | Type | Description |
|---|---|---|
| `SelectedDateChanged` | Routed (`DateSpinnerDateChangedEventArgs`) | `OldDate` / `NewDate`. Suppressed when the effective calendar day is unchanged. |
| `DropDownOpened` | Routed (`RoutedEventArgs`) | After the drop down opens. |
| `DropDownClosed` | Routed (`RoutedEventArgs`) | After it closes, however it closed. |

## Methods and Routed Commands

`Open()`, `Close()`, `Apply()`, `Cancel()`, `Clear()` — each also a `RoutedUICommand` static (`DateSpinner.OpenCommand`, etc.) that the default template binds to.

* `Close()` — closes without committing (Explicit discards; Immediate already wrote through).
* `Cancel()` — additionally restores the date from when the drop down opened.

## XAML Example

```xml
<mosaic:DateSpinner
    Header="Arrival date"
    SelectedDate="{Binding ArrivalDate, Mode=TwoWay}"
    MinimumDate="{Binding EarliestArrivalDate}"
    MaximumDate="{Binding LatestArrivalDate}"
    IsClearButtonVisible="True" />

<!-- Month and year only, abbreviated month -->
<mosaic:DateSpinner
    Header="Expiration date"
    SelectedDate="{Binding ExpirationDate, Mode=TwoWay}"
    IsDayVisible="False"
    MonthDisplayMode="AbbreviatedName" />
```

## Gotchas

* **Bind `MinimumDate`/`MaximumDate` from a view model or static resource**, not a XAML literal — WPF cannot reliably convert every date representation from a string.
* Opening and closing without touching a wheel never assigns a date. `SelectedDate` stays `null`.
* Out-of-range values stay on the wheels but are dimmed and unselectable; the wheels' geometry never shifts at a boundary.
* Day clamping is automatic: January 31 → February yields the 28th (or 29th in a leap year).
* `IsDropDownOpen` is read only by design.
* Wheel item heights are stamped on in `PrepareContainerForItemOverride`, not bound — a container is prepared before it joins the visual tree, so a `RelativeSource` binding would not resolve and the wheel's scroll arithmetic would drift.

## Localization

Placeholders and button captions resolve as: explicit property → theme resource → English default. Redefine the `ComponentResourceKey` resources (`MonthPlaceholderText`, `DayPlaceholderText`, `YearPlaceholderText`, `ApplyButtonText`, `CancelButtonText`, `ClearButtonText`) in a merged dictionary to translate every instance at once.

## Accessibility

`DateSpinnerAutomationPeer` exposes the control as a combo box with the ExpandCollapse and Value patterns. The accessible value describes only the visible fields ("July 2029" for a month-and-year picker) and raises a value-changed event on selection change. Each wheel carries an `AutomationProperties.Name` of Month, Day, or Year.
