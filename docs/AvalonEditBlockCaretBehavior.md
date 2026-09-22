# AvalonEditBlockCaretBehavior

Attaches to an AvalonEdit `TextEditor` and swaps the default thin line caret for a solid block caret. The character sitting underneath the block is re-drawn on top of it in a contrasting color so it stays legible.

This is the AvalonEdit counterpart of [`BlockCaretBehavior`](./BlockCaretBehavior.md), which targets a plain WPF `TextBox` and cannot be attached to a `TextEditor`. [`CommandBox`](./CommandBox.md) has the same renderer built in behind its `UseBlockCaret` property, so the behavior is only needed for other editors &mdash; [`SyntaxEditor`](./SyntaxEditor.md) or a bare `TextEditor`.

```xml
<avalonedit:TextEditor FontFamily="Consolas" FontSize="14">
    <i:Interaction.Behaviors>
        <mosaic:AvalonEditBlockCaretBehavior IsEnabled="{Binding UseBlockCaret}"
                                             CaretBrush="LimeGreen"
                                             CaretTextBrush="Black"
                                             CaretOpacity="0.8" />
    </i:Interaction.Behaviors>
</avalonedit:TextEditor>
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `IsEnabled` | `bool` | `true` | Whether the block caret is shown in place of the editor's normal caret. Can be toggled or bound at runtime; the renderer is installed and removed as it changes. |
| `CaretBrush` | `Brush?` | `null` | The fill of the block. `null` uses the editor's `Foreground`. A `SolidColorBrush` is recommended &mdash; it is the only brush type the automatic `CaretTextBrush` contrast calculation can inspect. |
| `CaretTextBrush` | `Brush?` | `null` | The color the covered character is re-drawn in. `null` picks black or white automatically, based on the block color composited over the editor background. |
| `CaretOpacity` | `double` | `0.8` | The opacity of the block, from 0.0 to 1.0. |

## Notes

- The block is drawn on `KnownLayer.Caret` and only while the text area has focus.
- It tracks the editor's own caret, so it follows the arrow keys, the mouse and programmatic caret moves alike.
- The typeface for the covered character is taken from the visual line element, so syntax highlighting (bold, italic, a different font) is honored.
- The real advance width of the covered character is used, so wide (CJK) glyphs are not clipped by a one-space-wide block, and surrogate pairs are drawn as a unit rather than as two replacement boxes.
- Brushes are cached because the caret layer redraws on every blink.
- While the behavior is attached the editor's `Caret.CaretBrush` is set to transparent; detaching (or disabling) restores it.

`VT52Terminal` keeps its own block caret renderer: its caret is driven by terminal escape sequences rather than by the editor caret, so it is not a candidate for this one.
