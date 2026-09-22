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

## AvalonEditBlockCaretBehavior

Replaces the thin line caret of a `TextEditor` with a solid block caret, re-drawing the character underneath it in a contrasting color so it stays legible. Attaches to the editor itself. The same renderer is built into `CommandBox` behind its `UseBlockCaret` property, so this behavior is for *other* editors (`SyntaxEditor`, a bare `TextEditor`).

Note that `BlockCaretBehavior` (no `AvalonEdit` prefix) is a different type: it targets a plain WPF `TextBox` and cannot be attached to a `TextEditor`.

| Property | Type | Default | Description |
|---|---|---|---|
| `IsEnabled` | `bool` | `true` | Shows the block caret. Toggling it installs or removes the renderer at runtime. |
| `CaretBrush` | `Brush?` | `null` | Fill of the block. Null uses the editor `Foreground`. A `SolidColorBrush` is recommended, it is the only type the automatic contrast calculation can inspect. |
| `CaretTextBrush` | `Brush?` | `null` | Color of the covered character. Null picks black or white based on the block composited over the editor background. |
| `CaretOpacity` | `double` | `0.8` | Opacity of the block, 0.0 to 1.0. |

```xml
<avalonedit:TextEditor>
    <i:Interaction.Behaviors>
        <mosaic:AvalonEditBlockCaretBehavior CaretBrush="LimeGreen" CaretOpacity="0.8" />
    </i:Interaction.Behaviors>
</avalonedit:TextEditor>
```

Drawn on `KnownLayer.Caret` and only while the text area is focused. It honors per-run typefaces from syntax highlighting, uses the real advance width so wide (CJK) glyphs are not clipped, draws surrogate pairs as a unit, and caches brushes because the caret layer redraws on every blink. `VT52Terminal` keeps its own renderer, its caret is driven by escape sequences rather than the editor caret.

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

## AvalonEditCopyBehavior

Attached to a `Button` (not the editor). On click, copies the target `TextEditor`'s selected text to the clipboard, or the entire text if nothing is selected.

| Property | Type | Description |
|---|---|---|
| `TargetTextEditor` | `TextEditor` | The editor to copy from. |

```xml
<Button Content="Copy">
    <i:Interaction.Behaviors>
        <behaviors:AvalonEditCopyBehavior TargetTextEditor="{Binding ElementName=XamlEditor}" />
    </i:Interaction.Behaviors>
</Button>
```
