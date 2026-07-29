# FontComboBox

A ComboBox that lists every font family installed on the system. The selection is exposed both as a `FontFamily` and as a string name, so a view model can bind whichever it stores.

```xml
<mosaic:FontComboBox SelectedFontName="{Binding EditorFontName, Mode=TwoWay}" />
```

Rendering each font name in its own font turns the drop-down into a font preview:

```xml
<mosaic:FontComboBox
    PreviewFontSize="16"
    ShowFontPreview="True"
    SelectedFontFamily="{Binding EditorFont, Mode=TwoWay}" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedFontFamily` | `FontFamily?` | `null` | The selected family. Two-way by default. |
| `SelectedFontName` | `string?` | `null` | The selected family name, e.g. `Segoe UI`. Two-way by default. |
| `ShowFontPreview` | `bool` | `false` | Renders each font name in its own font. |
| `PreviewFontSize` | `double` | `14` | Size of the preview text. |

Notes:

- The item list comes from `FontFamilyCatalog` — do not set `ItemsSource` manually.
- `SelectedFontFamily`, `SelectedFontName`, and `SelectedItem` all stay in sync; setting any one updates the others.
- A family that is not installed cannot be selected and leaves the selection empty. Matching is by family name, ignoring case, so `new FontFamily("Arial")` resolves to the catalog's instance.
- Assigning an `ItemTemplate` explicitly overrides whatever `ShowFontPreview` would apply.
- The control reuses the standard (or opt-in native Mosaic) ComboBox styling — no custom template.
- `FontFamilyCatalog.Families` enumerates the installed fonts once per application and is shared by every font control; the list is not refreshed if fonts are installed or removed while the application is running.

See also: [FontAutoCompleteBox](./FontAutoCompleteBox.md).
