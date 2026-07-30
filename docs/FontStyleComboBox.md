# FontStyleComboBox

A ComboBox that lists the font styles — `Normal`, `Italic` and `Oblique` — each entry rendered in the style it names. The selection is exposed both as a `FontStyle` and as a string name, so a view model can bind whichever it stores.

```xml
<mosaic:FontStyleComboBox SelectedFontStyle="{Binding HeadingStyle, Mode=TwoWay}" />
```

Previews are on by default; turn them off for plain text:

```xml
<mosaic:FontStyleComboBox
    PreviewFontSize="16"
    ShowStylePreview="False"
    SelectedFontStyleName="{Binding HeadingStyleName, Mode=TwoWay}" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedFontStyle` | `FontStyle` | `FontStyles.Normal` | The selected style. Two-way by default. |
| `SelectedFontStyleName` | `string?` | `null` | The selected style name, e.g. `Italic`. Two-way by default. |
| `ShowStylePreview` | `bool` | `true` | Renders each style name in its own style. |
| `PreviewFontSize` | `double` | `14` | Size of the preview text. |

Notes:

- The item list comes from `FontStyleCatalog` — do not set `ItemsSource` manually.
- `SelectedFontStyle`, `SelectedFontStyleName`, and `SelectedItem` all stay in sync; setting any one updates the others.
- Assigning an `ItemTemplate` explicitly overrides whatever `ShowStylePreview` would apply.
- The control reuses the standard (or opt-in native Mosaic) ComboBox styling — no custom template.
- The `PropertyGrid` uses this control automatically for any `FontStyle` property.
- A font family without a true italic face is synthesized (faux italic) by WPF, so `Italic` and `Oblique` can look identical for some fonts.

See also: [FontWeightComboBox](./FontWeightComboBox.md), [FontComboBox](./FontComboBox.md), [FontAutoCompleteBox](./FontAutoCompleteBox.md).
