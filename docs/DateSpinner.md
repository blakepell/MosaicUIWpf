# DateSpinner

A date entry control that chooses a date with three coordinated scrolling wheels for month, day, and year rather than with a calendar grid. The closed control shows a compact entry surface with one field per date component; clicking it opens a drop down containing the wheels.

The interaction model follows the WinUI 3 date picker: fields appear in the order the current culture writes them, an unset control shows localized placeholders, and by default the drop down commits explicitly through Apply and Cancel buttons.

> This is a different control from [`DatePicker`](./DatePicker.md), which uses a popup **calendar grid**. Pick `DateSpinner` for dates that are easier to dial in than to find on a calendar — a date of birth, an expiration month and year, a model year.

## Usage

```xml
<mosaic:DateSpinner
    Header="Arrival date"
    SelectedDate="{Binding ArrivalDate, Mode=TwoWay}"
    MinimumDate="{Binding EarliestArrivalDate}"
    MaximumDate="{Binding LatestArrivalDate}"
    IsClearButtonVisible="True" />
```

`SelectedDate` is a nullable `DateTime` that binds two way by default, so no `Mode=TwoWay` is strictly required. It is always normalized to midnight and always coerced into `MinimumDate`..`MaximumDate`.

Because WPF cannot reliably convert every date representation from a XAML string, supply `MinimumDate` and `MaximumDate` from a view model property or a static resource rather than as literals.

## Partial dates

Hide any field you do not need. At least one field always stays visible — an attempt to hide the last one is coerced back to `true`. A hidden field's component of the value is preserved.

```xml
<!-- Month and year only -->
<mosaic:DateSpinner
    Header="Expiration date"
    SelectedDate="{Binding ExpirationDate, Mode=TwoWay}"
    IsDayVisible="False"
    MonthDisplayMode="AbbreviatedName" />

<!-- Year only -->
<mosaic:DateSpinner IsMonthVisible="False" IsDayVisible="False" SelectedDate="{Binding ModelYear}" />
```

## Commit modes

```xml
<mosaic:DateSpinner
    Header="Date of birth"
    SelectedDate="{Binding DateOfBirth, Mode=TwoWay}"
    MinimumDate="{Binding EarliestBirthDate}"
    MaximumDate="{Binding Today}"
    CommitMode="Explicit"
    IsClearButtonVisible="True" />
```

| Mode | Behavior |
|---|---|
| `Explicit` (default) | The drop down shows Apply and Cancel. The temporary date only reaches `SelectedDate` on Apply. Cancel, Escape, and (by default) clicking away discard it. Matches the WinUI accept/dismiss model. |
| `Immediate` | Every wheel movement is written straight through to `SelectedDate`, and no action buttons are shown. Closing keeps the latest value. |

`LightDismissBehavior` decides what clicking outside an open drop down does with the temporary date: `Cancel` (default) or `Apply`.

Opening and closing the drop down without touching a wheel never assigns a date.

## Range and boundary filtering

Values outside the permitted range stay on the wheels but are not selectable, which keeps the wheels' geometry stable as the user moves across a boundary.

* With `MinimumDate` of March 15 2025 and the year on 2025, January and February are not selectable.
* With March 2025 selected, days 1 through 14 are not selectable.
* The year wheel only contains years inside the range.

`MaximumDate` is coerced up to `MinimumDate` when set below it, so `MinimumDate <= MaximumDate` always holds. An externally assigned `SelectedDate` outside the range is coerced into it rather than rejected.

## Coordinated fields

The wheels stay synchronized. Changing the month or year rebuilds the day wheel, and the day is clamped to the last valid day of the new month rather than producing an invalid date:

* January 31 2025 → February becomes February 28 2025.
* January 31 2024 → February becomes February 29 2024.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedDate` | `DateTime?` | `null` | The selected date. Binds two way by default, normalized to midnight, coerced into range. |
| `MinimumDate` | `DateTime` | today − 100 years | Earliest selectable date, inclusive. |
| `MaximumDate` | `DateTime` | today + 100 years | Latest selectable date, inclusive. Coerced up to `MinimumDate` when set below it. |
| `IsDropDownOpen` | `bool` | `false` | Read only. Use `Open()`/`Close()` or the routed commands. |
| `CommitMode` | `DateSpinnerCommitMode` | `Explicit` | When wheel edits reach `SelectedDate`. |
| `LightDismissBehavior` | `DateSpinnerLightDismissBehavior` | `Cancel` | What clicking away does with the temporary date. |
| `IsMonthVisible` / `IsDayVisible` / `IsYearVisible` | `bool` | `true` | Field visibility. At least one stays visible. |
| `MonthDisplayMode` | `DateSpinnerMonthDisplayMode` | `FullName` | `FullName`, `AbbreviatedName`, `Numeric`, `TwoDigitNumeric`. |
| `MonthFormat` / `DayFormat` / `YearFormat` | `string?` | `null` | Optional .NET date format strings. A format the runtime cannot honor degrades instead of throwing. |
| `Culture` | `CultureInfo?` | `null` | Month names, number formatting, and field order. Falls back to `CultureInfo.CurrentUICulture`. |
| `Header` / `HeaderTemplate` | `object?` / `DataTemplate?` | `null` | Optional label above the entry surface. A string header also becomes the accessible name. |
| `MonthPlaceholderText` / `DayPlaceholderText` / `YearPlaceholderText` | `string?` | `null` | Placeholders for the unset state. Fall back to theme resources so they can be localized centrally. |
| `IsClearButtonVisible` | `bool` | `false` | Shows a clear button, enabled only while a date is selected. |
| `IsReadOnly` | `bool` | `false` | Value stays visible and the control stays focusable, but cannot be opened or changed. |
| `ItemHeight` / `VisibleItemCount` | `double` / `int` | `30` / `5` | Wheel metrics. `VisibleItemCount` is coerced to an odd number of at least three. |
| `IsScrollAnimationEnabled` | `bool` | `true` | Whether the wheels ease into position. Set `false` to reduce motion. |

## Events

| Event | Type | Description |
|---|---|---|
| `SelectedDateChanged` | Routed (`DateSpinnerDateChangedEventArgs`) | Carries `OldDate` and `NewDate`. Not raised when the effective calendar day is unchanged. |
| `DropDownOpened` | Routed (`RoutedEventArgs`) | Raised after the drop down opens. |
| `DropDownClosed` | Routed (`RoutedEventArgs`) | Raised after the drop down closes, however it was closed. |

## Methods and Commands

`Open()`, `Close()`, `Apply()`, `Cancel()`, and `Clear()` are available as public methods and as the routed commands `DateSpinner.OpenCommand`, `CloseCommand`, `ApplyCommand`, `CancelCommand`, and `ClearCommand`.

* `Close()` closes without committing. In `Explicit` mode the temporary date is discarded; in `Immediate` mode the value has already been written through.
* `Cancel()` additionally restores the date that was selected when the drop down opened.
* `Clear()` sets `SelectedDate` to `null`.

## Keyboard

| Closed | |
|---|---|
| `Enter`, `Space`, `Alt`+`Down` | Open the drop down. |

| Open | |
|---|---|
| `Left` / `Right` | Move between visible wheels. |
| `Up` / `Down` | Change the value on the active wheel, skipping values outside the range. |
| `Page Up` / `Page Down` | Move by a viewport. |
| `Home` / `End` | First / last selectable value. |
| `Enter` | Apply and close. |
| `Escape` | Cancel and close. |
| `Tab` | Moves through the wheels and action buttons normally; focus is never trapped. |

Focus returns to the entry surface when the drop down closes.

## Localization

Placeholders and button captions resolve from the explicit property, then a theme resource, then a built-in English default. Merge a dictionary that redefines the resource keys to translate every spinner at once:

```xml
<ResourceDictionary xmlns:controls="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"
                    xmlns:sys="clr-namespace:System;assembly=System.Runtime">
    <sys:String x:Key="{ComponentResourceKey TypeInTargetAssembly={x:Type controls:DateSpinner}, ResourceId=MonthPlaceholderText}">Mois</sys:String>
    <sys:String x:Key="{ComponentResourceKey TypeInTargetAssembly={x:Type controls:DateSpinner}, ResourceId=ApplyButtonText}">Appliquer</sys:String>
</ResourceDictionary>
```

The keys are `MonthPlaceholderText`, `DayPlaceholderText`, `YearPlaceholderText`, `ApplyButtonText`, `CancelButtonText`, and `ClearButtonText`, also exposed as `ComponentResourceKey` statics on `DateSpinner`.

## Accessibility

`DateSpinnerAutomationPeer` exposes the control as a combo box supporting the ExpandCollapse and Value patterns. The accessible name comes from a string `Header`; the accessible value is the selected date, described using only the fields that are actually visible ("July 2029" for a month and year picker). Changes raise a value-changed automation event. Selection and the unset state are conveyed with weight and style as well as color.

## Notes

* Date arithmetic and formatting live in the public static `DateSpinnerCalendar` helper, which is pure and independently testable.
* The wheels virtualize, so a wide year range costs no more realized containers than a narrow one.
