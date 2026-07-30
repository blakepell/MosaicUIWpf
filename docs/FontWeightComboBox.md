# FontWeightComboBox

A ComboBox that lists the standard font weights (Thin through ExtraBlack), each entry rendered in the weight it names. The selection is exposed both as a `FontWeight` and as a string name, so a view model can bind whichever it stores.

```xml
<mosaic:FontWeightComboBox SelectedFontWeight="{Binding HeadingWeight, Mode=TwoWay}" />
```

Previews are on by default; turn them off for plain text:

```xml
<mosaic:FontWeightComboBox
    PreviewFontSize="16"
    ShowWeightPreview="False"
    SelectedFontWeightName="{Binding HeadingWeightName, Mode=TwoWay}" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedFontWeight` | `FontWeight` | `FontWeights.Normal` | The selected weight. Two-way by default. |
| `SelectedFontWeightName` | `string?` | `null` | The selected weight name, e.g. `SemiBold`. Two-way by default. |
| `ShowWeightPreview` | `bool` | `true` | Renders each weight name in its own weight. |
| `PreviewFontSize` | `double` | `14` | Size of the preview text. |

Notes:

- The item list comes from `FontWeightCatalog` — do not set `ItemsSource` manually.
- Ten distinct weights are listed: Thin, ExtraLight, Light, Normal, Medium, SemiBold, Bold, ExtraBold, Black, ExtraBlack. The aliases (`Regular`, `DemiBold`, `UltraBold`, `Heavy`, `UltraLight`, `UltraBlack`) are not listed separately because they resolve to the same OpenType weights, but `FontWeightCatalog.Find` still accepts them — as it does the numeric weights, e.g. `"600"`.
- `SelectedFontWeight`, `SelectedFontWeightName`, and `SelectedItem` all stay in sync; setting any one updates the others.
- A non-standard weight cannot be selected and leaves the selection empty.
- Assigning an `ItemTemplate` explicitly overrides whatever `ShowWeightPreview` would apply.
- The control reuses the standard (or opt-in native Mosaic) ComboBox styling — no custom template.
- The `PropertyGrid` uses this control automatically for any `FontWeight` property.

See also: [FontComboBox](./FontComboBox.md), [FontAutoCompleteBox](./FontAutoCompleteBox.md).
