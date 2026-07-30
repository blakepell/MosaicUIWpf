# FontComboBox / FontAutoCompleteBox / FontWeightComboBox

**Base classes:** `ComboBox` / `AutoCompleteBox` / `ComboBox`
**Namespace:** `Mosaic.UI.Wpf.Controls`
**Source:** `src/Mosaic.UI.Wpf/Controls/Font/FontComboBox.cs`, `src/Mosaic.UI.Wpf/Controls/Font/FontAutoCompleteBox.cs`, `src/Mosaic.UI.Wpf/Controls/Font/FontFamilyCatalog.cs`, `src/Mosaic.UI.Wpf/Controls/Font/FontWeightComboBox.cs`, `src/Mosaic.UI.Wpf/Controls/Font/FontWeightCatalog.cs`
**Example:** `src/MosaicWpfDemo/Views/Examples/FontControlsExample.xaml`

## Description

Pickers over the fonts installed on the system plus the standard font weights. `FontComboBox` is a plain drop-down; `FontAutoCompleteBox` filters as the user types and previews each suggestion in the font it names; `FontWeightComboBox` lists the ten standard weights, each rendered in its own weight. The family pickers populate themselves from `FontFamilyCatalog` and expose the same selection surface; the weight picker uses `FontWeightCatalog`.

## Key Properties (family pickers)

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedFontFamily` | `FontFamily?` | `null` | The selected family. Two-way by default. |
| `SelectedFontName` | `string?` | `null` | The selected family name, e.g. `Segoe UI`. Two-way by default. |
| `ShowFontPreview` | `bool` | `false` (ComboBox) / `true` (AutoComplete) | Renders each font name in its own font. |
| `PreviewFontSize` | `double` | `14` / `15` | Size of the preview text. |

`FontAutoCompleteBox` inherits everything from `AutoCompleteBox` (`Text`, `Watermark`, `FilterMode`, `DropDownMaxHeight`, …) and adjusts these defaults for a fixed, in-memory list: `IsTextRequiredForSuggestions=false`, `MinimumPrefixLength=0`, `MaxSuggestionCount` covering the whole catalog, and a 75 ms `LookupDelay`.

## Key Properties (FontWeightComboBox)

| Property | Type | Default | Description |
|---|---|---|---|
| `SelectedFontWeight` | `FontWeight` | `FontWeights.Normal` | The selected weight. Two-way by default. |
| `SelectedFontWeightName` | `string?` | `null` | The selected weight name, e.g. `SemiBold`. Two-way by default. |
| `ShowWeightPreview` | `bool` | `true` | Renders each weight name in its own weight. |
| `PreviewFontSize` | `double` | `14` | Size of the preview text. |

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

<!-- Weight picker; each entry renders in its own weight -->
<mosaic:FontWeightComboBox
    Width="180"
    SelectedFontWeight="{Binding HeadingWeight, Mode=TwoWay}" />
```

## FontFamilyCatalog

```csharp
IReadOnlyList<FontFamily> families = FontFamilyCatalog.Families;   // cached, sorted by name
FontFamily? consolas = FontFamilyCatalog.Find("consolas");         // case-insensitive by name
FontFamily? resolved = FontFamilyCatalog.Resolve(someFontFamily);  // maps to the cached instance
```

## FontWeightCatalog

```csharp
IReadOnlyList<FontWeight> weights = FontWeightCatalog.Weights;     // Thin .. ExtraBlack
FontWeight? semiBold = FontWeightCatalog.Find("demibold");         // aliases and "600" also work
FontWeight? resolved = FontWeightCatalog.Resolve(someFontWeight);  // null when non-standard
```

## Notes

- Do not set `ItemsSource` — the controls own their item generation.
- The `PropertyGrid` picks these up automatically: a `FontFamily` property gets a `FontComboBox` (previews on) and a `FontWeight` property gets a `FontWeightComboBox`.
- `SelectedFontFamily`, `SelectedFontName`, and `SelectedItem` all stay in sync; setting any one updates the others.
- A family that is not installed cannot be selected and leaves the selection empty. Matching is by name, ignoring case, so `new FontFamily("Arial")` resolves to the catalog's instance.
- Assigning an `ItemTemplate` explicitly overrides whatever `ShowFontPreview` would apply.
- `FontFamilyCatalog.Families` enumerates the installed fonts once per application; it is not refreshed if fonts are installed or removed while running.
- Neither control registers a new default style: `FontComboBox` uses the standard (or opt-in native Mosaic) ComboBox style, and `FontAutoCompleteBox` reuses the `AutoCompleteBox` template.
