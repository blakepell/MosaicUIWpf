# EnumComboBox

**Base class:** `ComboBox`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/EnumComboBox/EnumComboBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/EnumComboBoxExample.xaml`

## Description

A `ComboBox` that populates itself from the declared members of the enum type assigned to `EnumType`. Bind your view model to **`SelectedValue`** — it holds the actual enum value. `SelectedItem` exposes the internal `EnumComboBoxItem` wrapper instead.

Display text per member resolves in this order: `DisplayNameAttribute` → `DescriptionAttribute` → the member name. C# does not allow `DisplayNameAttribute` on fields, so apply **`EnumDisplayNameAttribute`** to enum members; it derives from `DisplayNameAttribute` and resolves through the same lookup.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `EnumType` | `Type?` | `null` | The enum type whose declared members populate the drop-down. Nullable enum types resolve to their underlying enum. Setting a non-enum type throws. |

`SelectedValuePath` and `DisplayMemberPath` are preconfigured; do not set `ItemsSource` — `EnumType` owns item generation.

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:EnumComboBox
    Width="200"
    EnumType="{x:Type local:OrderStatus}"
    SelectedValue="{Binding Status, Mode=TwoWay}" />
```

```csharp
public enum OrderStatus
{
    [EnumDisplayName("Awaiting Payment")]
    AwaitingPayment,

    [Description("In Transit")]
    Shipped,

    Delivered
}
```

## Notes

- Generated item lists are cached per resolved enum type in a static `ConcurrentDictionary`, so reflection runs once per enum regardless of how many controls display it.
- Fields are reflected directly rather than via `Enum.GetValues`, so aliased members with duplicate numeric values keep their own name and attributes; items appear in declaration order.
- Nullable view-model properties may hold `null`, which simply means no selection.
- `[Flags]` enums show only their explicitly declared members — this binds a single value and is not a flags editor.
- The style resource reference is `typeof(ComboBox)`, so the control renders exactly like the standard `ComboBox` in the current theme (including Mosaic's opt-in native `ComboBox` style).
