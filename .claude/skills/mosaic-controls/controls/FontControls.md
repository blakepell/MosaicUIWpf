# FontComboBox / FontAutoCompleteBox

**Base classes:** `ComboBox` / `AutoCompleteBox`
**Namespace:** `Mosaic.UI.Wpf.Controls`
**Source:** `src/Mosaic.UI.Wpf/Controls/Font/FontComboBox.cs`, `src/Mosaic.UI.Wpf/Controls/Font/FontAutoCompleteBox.cs`, `src/Mosaic.UI.Wpf/Controls/Font/FontFamilyCatalog.cs`
**Example:** `src/MosaicWpfDemo/Views/Examples/FontControlsExample.xaml`

## Description

Two pickers over the font families installed on the system. `FontComboBox` is a plain drop-down; `FontAutoCompleteBox` filters as the user types and previews each suggestion in the font it names. Both populate themselves from `FontFamilyCatalog` and expose the same selection surface.

## Key Properties (both controls)

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedFontFamily` | `FontFamily?` | `null` | The selected family. Two-way by default. |
| `SelectedFontName` | `string?` | `null` | The selected family name, e.g. `Segoe UI`. Two-way by default. |
| `ShowFontPreview` | `bool` | `false` (ComboBox) / `true` (AutoComplete) | Renders each font name in its own font. |
| `PreviewFontSize` | `double` | `14` / `15` | Size of the preview text. |

`FontAutoCompleteBox` inherits everything from `AutoCompleteBox` (`Text`, `Watermark`, `FilterMode`, `DropDownMaxHeight`, …) and adjusts these defaults for a fixed, in-memory list: `IsTextRequiredForSuggestions=false`, `MinimumPrefixLength=0`, `MaxSuggestionCount` covering the whole catalog, and a 75 ms `LookupDelay`.

## XAML Examples

```xml
xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"

<!-- Persist the choice as a string -->
<mosaic:FontComboBox Width="260" SelectedFontName="{Binding EditorFontName, Mode=TwoWay}" />

<!-- In-font previews, bound as a FontFamily -->
<mosaic:FontComboBox
    Width="260"
    PreviewFontSize="16"
    SelectedFontFamily="{Binding EditorFont, Mode=TwoWay}"
    ShowFontPreview="True" />

<!-- Type-to-filter with previews (on by default) -->
<mosaic:FontAutoCompleteBox
    Width="260"
    SelectedFontName="{Binding EditorFontName, Mode=TwoWay}"
    Watermark="Search fonts..." />
```

## FontFamilyCatalog

```csharp
IReadOnlyList<FontFamily> families = FontFamilyCatalog.Families;   // cached, sorted by name
FontFamily? consolas = FontFamilyCatalog.Find("consolas");         // case-insensitive by name
FontFamily? resolved = FontFamilyCatalog.Resolve(someFontFamily);  // maps to the cached instance
```

## Notes

- Do not set `ItemsSource` — the controls own their item generation.
- `SelectedFontFamily`, `SelectedFontName`, and `SelectedItem` all stay in sync; setting any one updates the others.
- A family that is not installed cannot be selected and leaves the selection empty. Matching is by name, ignoring case, so `new FontFamily("Arial")` resolves to the catalog's instance.
- Assigning an `ItemTemplate` explicitly overrides whatever `ShowFontPreview` would apply.
- `FontFamilyCatalog.Families` enumerates the installed fonts once per application; it is not refreshed if fonts are installed or removed while running.
- Neither control registers a new default style: `FontComboBox` uses the standard (or opt-in native Mosaic) ComboBox style, and `FontAutoCompleteBox` reuses the `AutoCompleteBox` template.
