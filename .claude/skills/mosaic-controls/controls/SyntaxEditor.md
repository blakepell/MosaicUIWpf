# SyntaxEditor

**Base class:** `TextEditor` (AvalonEdit)  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/SyntaxEditor/SyntaxEditor.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/SyntaxEditorExample.xaml`

## Description

A code editor built on AvalonEdit (`ICSharpCode.AvalonEdit.TextEditor`) that integrates with the Mosaic theming system and provides bundled, theme-aware syntax highlighting selected via the `Language` property. Includes custom key chords for commenting, uncommenting, and moving lines.

## Key Properties

| Property | Type | Description |
|---|---|---|
| `Language` | `SyntaxLanguage` | Selects the bundled, theme-aware highlighting definition. |
| `Theme` | (theme) | The highlighting/editor theme to apply. |
| `FollowGlobalTheme` | `bool` | Track the global Mosaic light/dark theme automatically. |
| `ClearVisible` | `bool` | Whether a clear affordance is shown. |

`SyntaxLanguage` values: `None`, `Json`, `CSharp`, `Xml`, `JavaScript`, `Sql`, `Markdown`, `C`, `Lua`, `Python`, `Ini`, `Java`, `Swift`, `Basic`, `VbNet`, `Perl`, `Php`.

All standard AvalonEdit `TextEditor` members apply (`Text`, `Document`, `Options`, `ShowLineNumbers`, etc.).

## Events

`ContextMenuRequested` (`SyntaxEditorContextMenuEventArgs`).

## XAML Example

```xml
xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf"

<mosaic:SyntaxEditor
    Language="CSharp"
    FollowGlobalTheme="True"
    ShowLineNumbers="True" />
```

## Notes

- Bundled highlighting definitions are theme-aware and switch with the Mosaic theme when `FollowGlobalTheme` is on.
- Key chords support comment/uncomment and move-line operations.
- For binding `Text`, see the `AvalonEditBindingBehavior` behavior.
