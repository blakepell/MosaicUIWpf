# AvalonEdit Behaviors

**Namespace:** `Mosaic.UI.Wpf.Behaviors`  
**Sources:** `src/Mosaic.UI.Wpf/Behaviors/AvalonEdit*.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/AvalonEditVtTerminalBehaviorExample.xaml`

## AvalonEditVtTerminalBehavior

Applies a retro VT/CRT terminal skin to an AvalonEdit `TextEditor`: phosphor foreground/background colors, block caret, scan lines, vignette, glow, and subtle noise overlay.

| Property | Type | Default | Description |
|---|---|---|---|
| `IsActive` | `bool` | `true` | Enables or disables the skin and restores previous editor styling when disabled. |
| `ForegroundColor` | `Color` | `#33FF33` | Phosphor foreground and caret color. |
| `BackgroundColor` | `Color` | `#050505` | Terminal background color. |

```xml
<avalonedit:TextEditor>
    <i:Interaction.Behaviors>
        <behaviors:AvalonEditVtTerminalBehavior IsActive="True" />
    </i:Interaction.Behaviors>
</avalonedit:TextEditor>
```

## AvalonTextEditorBindingBehavior

Adds binding-friendly dependency properties to AvalonEdit `TextEditor`.

| Property | Type | Description |
|---|---|---|
| `Text` | `string` | Two-way editor text binding. Avoid for very large documents because AvalonEdit's rope document is converted to strings. |
| `SelectedText` | `string` | Two-way selected text binding. |
| `Selection` | `(int start, int length)` | Two-way selection range binding. |
| `CursorPosition` | `int` | Two-way caret offset binding. |

## AvalonEditPropertiesBehavior

Small property bridge for AvalonEdit runtime styling and hyperlink behavior.

| Property | Type | Description |
|---|---|---|
| `CaretBrush` | `Brush` | Sets `TextArea.Caret.CaretBrush`. |
| `EnableHyperLinks` | `bool` | Enables AvalonEdit hyperlink detection/clicking and disables the Ctrl modifier requirement. |
