# EnumComboBox

A ComboBox that populates itself from the declared members of an enum type. The drop-down shows a friendly name for each member while `SelectedValue` reads and writes the actual enum value, so view models bind a plain enum (or nullable enum) property directly.

```xml
<mosaic:EnumComboBox
    EnumType="{x:Type local:OrderStatus}"
    SelectedValue="{Binding Status, Mode=TwoWay}" />
```

Display-name resolution precedence:

```text
DisplayNameAttribute (via EnumDisplayNameAttribute) → DescriptionAttribute → enum member name
```

Because C# does not allow `DisplayNameAttribute` directly on enum fields, Mosaic provides `EnumDisplayNameAttribute` (a field-applicable `DisplayNameAttribute` subclass):

```csharp
public enum OrderStatus
{
    Pending,

    [EnumDisplayName("In Progress")]
    InProgress,

    [Description("Waiting for Customer")]
    WaitingForCustomer
}
```

Notes:

- `EnumType` is the source of truth — do not set `ItemsSource` manually.
- Nullable enum types are supported; a `null` bound value simply means no selection.
- Setting `EnumType` to `null` clears the items; assigning a non-enum type throws an `ArgumentException`.
- `[Flags]` enums are shown as their explicitly declared members only — this is a single-selection control, not a flags editor.
- Aliased members with duplicate numeric values are listed separately with their own display names.
- The control reuses the standard (or opt-in native Mosaic) ComboBox styling — no custom template.
