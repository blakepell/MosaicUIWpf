# FontAutoCompleteBox

An [AutoCompleteBox](./AutoCompleteBox.md) that suggests the font families installed on the system and renders every suggestion in the font it names.

```xml
<mosaic:FontAutoCompleteBox
    Watermark="Search fonts..."
    SelectedFontName="{Binding EditorFontName, Mode=TwoWay}" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedFontFamily` | `FontFamily?` | `null` | The selected family. Two-way by default. |
| `SelectedFontName` | `string?` | `null` | The selected family name, e.g. `Segoe UI`. Two-way by default. |
| `ShowFontPreview` | `bool` | `true` | Renders each suggested font name in its own font. |
| `PreviewFontSize` | `double` | `15` | Size of the preview text. |

Everything inherited from `AutoCompleteBox` still applies (`Text`, `Watermark`, `FilterMode`, `DropDownMaxHeight`, `IsDropDownOpen`, the `DropDownOpened`/`DropDownClosed` routed events, and so on), with these defaults adjusted for a fixed, in-memory list:

- `IsTextRequiredForSuggestions` is `false` and `MinimumPrefixLength` is `0`, so opening the drop-down lists the whole catalog.
- `MaxSuggestionCount` covers every installed font.
- `LookupDelay` is 75 ms rather than 250 ms, since filtering is a local operation.

Notes:

- The item list comes from `FontFamilyCatalog` — do not set `ItemsSource` manually.
- `SelectedFontFamily`, `SelectedFontName`, and `SelectedItem` all stay in sync; setting any one updates the others.
- Filtering is `Contains` by default; set `FilterMode="StartsWith"` for prefix matching.
- Typed text that does not resolve to an installed font leaves the selection empty.
- Assigning an `ItemTemplate` explicitly overrides whatever `ShowFontPreview` would apply.
- The control reuses the `AutoCompleteBox` default template rather than registering its own style.

See also: [FontComboBox](./FontComboBox.md).
