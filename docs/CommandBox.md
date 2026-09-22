# CommandBox

A single-line command input built on AvalonEdit's `TextEditor`. Pressing `Enter` raises a `CommandExecuted` event, `Up` / `Down` walk a persistent command history, `Tab` completes from that history, and an optional block caret gives the box a terminal feel.

The control is deliberately general purpose. It dispatches the command and gets out of the way &mdash; what a command *means* is entirely up to the host.

```xml
<mosaic:CommandBox x:Name="Input"
                   Watermark="Enter a command..."
                   UseBlockCaret="True"
                   CommitBehavior="SelectAll"
                   CommandExecuted="OnCommandExecuted" />
```

```csharp
private void OnCommandExecuted(object sender, CommandExecutedEventArgs e)
{
    this.Output.AppendText($"> {e.Command}\n");
}
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `CommitBehavior` | `CommandBoxCommitBehavior` | `SelectAll` | What happens to the text after a command is dispatched. `Clear` blanks the box, `Keep` leaves the text and caret alone, `SelectAll` keeps the text and selects it so the next keystroke replaces it. |
| `IsHistoryEnabled` | `bool` | `true` | Whether commands are recorded and the arrow keys walk the history. |
| `MaxHistoryItems` | `int` | `100` | The maximum number of commands retained. Zero means unlimited. |
| `HistoryDuplicatePolicy` | `HistoryDuplicatePolicy` | `SkipConsecutive` | Whether a repeated command is recorded again. See [History](#history). |
| `IsTabCompletionEnabled` | `bool` | `true` | `Tab` completes the current text from the most recent matching history entry. With no match the key falls through to normal focus navigation. |
| `ClearOnEscape` | `bool` | `true` | `Escape` clears the box. `EscapePressed` is raised either way. |
| `SelectAllOnFocus` | `bool` | `true` | Selects the existing text when the box receives keyboard focus. |
| `SelectAllOnMouseFocus` | `bool` | `true` | The first click into an unfocused box focuses it and selects everything; the click itself is swallowed so it does not collapse the selection. |
| `TrimCommand` | `bool` | `true` | Strips leading and trailing whitespace from a command before it is dispatched. |
| `AllowEmptyCommands` | `bool` | `false` | When `false` a bare `Enter` on an empty box does nothing. |
| `ShowPrompt` | `bool` | `false` | Draws a prompt glyph at the left edge, inside the control's border. |
| `Prompt` | `string` | `">"` | The glyph drawn when `ShowPrompt` is on. Decoration only &mdash; never part of the text or the command. |
| `PromptBrush` | `Brush?` | `null` | The brush the prompt is painted with. `null` uses `Foreground`. |
| `PromptPadding` | `Thickness` | `2,0,6,0` | Space around the prompt: left separates it from the border, right from where typing starts. |
| `Watermark` | `string` | `""` | Placeholder text painted behind the box while it is empty. |
| `WatermarkBrush` | `Brush?` | `null` | The brush the watermark is painted with. `null` uses `Foreground` at half opacity. |
| `UseBlockCaret` | `bool` | `false` | Replaces the thin line caret with a solid block caret. Can be toggled or bound at runtime. |
| `BlockCaretBrush` | `Brush?` | `null` | The fill of the block caret. `null` uses `Foreground`, so a theme switch carries through. |
| `BlockCaretTextBrush` | `Brush?` | `null` | The color the covered character is re-drawn in. `null` picks black or white automatically. |
| `BlockCaretOpacity` | `double` | `0.8` | The opacity of the block caret. |
| `Command` | `ICommand?` | `null` | MVVM hook, executed after `CommandExecuted` has been raised. |
| `CommandParameter` | `object?` | `null` | The parameter passed to `Command`. When left `null` the command text is passed instead. |
| `History` | `CommandBoxHistory` | &mdash; | Read-only. The history list and its cursor. |

Everything `TextEditor` exposes (`Text`, `CaretOffset`, `SelectionStart`, `SelectionLength`, `FontFamily`, `IsReadOnly`, …) is available as usual. The font defaults to Consolas and the background, foreground and border are wired to `MosaicTheme.ControlBackgroundBrush`, `ControlForegroundBrush` and `ControlBorderBrush` with `DynamicResource` references, so a theme switch carries through without extra work.

> There is deliberately **no** two-way bindable `CommandText` property. The command arrives through the event; that is the whole contract.

## Events

| Event | Routing | Description |
|---|---|---|
| `PreviewCommandExecuted` | Tunnel | Raised before a command is dispatched. A handler may rewrite `e.Command`, or veto the command outright by setting `e.Handled = true`. |
| `CommandExecuted` | Bubble | Raised when the user presses `Enter` on a command. |
| `EscapePressed` | Bubble | Raised when the user presses `Escape`, whether or not the box was cleared. |

`CommandExecutedEventArgs` carries:

| Member | Type | Description |
|---|---|---|
| `Command` | `string` | The normalized, single-line command text. Settable from `PreviewCommandExecuted` to rewrite the command. |
| `AddToHistory` | `bool` | Defaults to `true`. Set it to `false` to keep a one-off command (a password, say) out of the history. |

### Dispatch order

On `Enter`:

1. The text is collapsed to one line and, unless `TrimCommand` is `false`, trimmed.
2. An empty command is dropped unless `AllowEmptyCommands` is set.
3. `PreviewCommandExecuted` is raised. A handler may rewrite `e.Command`; setting `e.Handled` vetoes everything below.
4. `CommitBehavior` is applied to the box. This happens **before** the public event so a handler that pushes new text into the box wins.
5. `CommandExecuted` is raised.
6. The command is recorded in `History` unless `IsHistoryEnabled` is `false` or a handler cleared `e.AddToHistory`.
7. `Command` is executed, if it can.

`ExecuteCommand()` runs the same pipeline from code and returns whether a command was actually dispatched.

## History

`CommandBoxHistory` is a plain, UI-free class, so its semantics are easy to reason about (and are unit tested without a dispatcher).

| Member | Description |
|---|---|
| `Items` | The recorded commands, oldest first. |
| `Count` | The number of recorded commands. |
| `MaxItems` | Eviction limit; the oldest entries go first. Zero means unlimited. |
| `DuplicatePolicy` | `Allow`, `SkipConsecutive` (default) or `SkipAll`. |
| `IsNavigating` | Whether the user is currently walking the history. |
| `Add(string?)` | Records a command. Trims it, ignores empty input, applies the duplicate policy, and always resets the cursor. |
| `Import(IEnumerable<string>?)` | Appends previously persisted commands. |
| `MoveBack()` / `MoveForward()` | The `Up` / `Down` walk. |
| `ResetCursor(string? draft)` | Returns the cursor to past-the-end and stashes the in-progress draft. |
| `Clear()` | Empties the list. |

Cursor semantics match a shell:

- The cursor normally sits *past the end* of the list, meaning no navigation is in progress.
- The command box calls `ResetCursor(Text)` every time the user types, so whatever is on screen when navigation starts becomes the **draft**.
- `Up` walks toward older entries and clamps at the oldest. `Down` walks back toward newer ones; stepping past the newest restores the draft, and the step after that does nothing.
- Recalling an entry puts the caret at the end with nothing selected.
- Recording a command always returns the cursor to past-the-end, even when the duplicate policy meant nothing was actually recorded.

Duplicate policies:

| Policy | Behavior |
|---|---|
| `Allow` | Every command is recorded. |
| `SkipConsecutive` | A command identical to the most recent entry is skipped. Running the same command ten times in a row yields one entry. |
| `SkipAll` | A command already anywhere in the list is skipped; existing entries keep their original position. |

### Persistence

The control does not save history to disk &mdash; that is the host's job. Persist `History.Items` however you like and restore it with:

```csharp
this.Input.ImportHistory(settings.RecentCommands);
...
settings.RecentCommands = this.Input.History.Items.ToList();
```

`ClearHistory()` empties it again.

## Single-line enforcement

The document is kept to one line at all times:

- `WordWrap` is off and both scroll bars are hidden.
- The editor's own newline commands (`EnterParagraphBreak`, `EnterLineBreak`) are removed, and `Enter` is handled unconditionally &mdash; with or without modifiers &mdash; so no key combination can insert a break.
- A paste is rewritten before it lands, collapsing `\r\n`, `\r` and `\n` to single spaces. A multi-line paste becomes one line rather than being rejected.
- As a catch-all for every other path (a programmatic assignment, an IME, a drop), any line break that reaches the document is collapsed once the edit completes.
- `MeasureOverride` reports the height of exactly one text line plus padding and border, so the box never grows to a second row.

## Prompt

`ShowPrompt="True"` draws a shell-style prompt at the left edge:

```xml
<mosaic:CommandBox ShowPrompt="True" Prompt="&gt;" PromptPadding="4,0,8,0" />
```

The prompt is an AvalonEdit **left margin**, not a sibling control glued to the outside of the box. It therefore lives inside the editor's own border and background, shares the theme brushes and the `Consolas` font with the text, and the editable text simply starts to its right &mdash; there is no seam, no second border and no alignment drift when the font size changes. It does not scroll with the text and it is not hit-testable, so clicking it focuses the box and places the caret exactly as clicking the text would.

`PromptPadding` controls both gaps: `Left` is the space between the control's border and the glyph, `Right` the space between the glyph and the first character the user types. It sits *inside* the control's own `Padding`, so the two add up on the left.

The glyph is decoration only. It is never part of `Text`, never part of `CommandExecutedEventArgs.Command`, and the caret can never be placed before it. Any string works &mdash; `>`, `$`, `&gt;&gt;&gt;`, `PS&gt;`, a user name &mdash; and setting `Prompt` to an empty string collapses the margin to nothing even while `ShowPrompt` is `true`.

## Block caret

`UseBlockCaret="True"` installs the same renderer that backs [`AvalonEditBlockCaretBehavior`](./AvalonEditBlockCaretBehavior.md): a solid block on the caret layer, with the character underneath re-drawn on top in a contrasting color so it stays legible. Wide (CJK) glyphs and surrogate pairs are measured and drawn correctly.

The block follows the editor's caret, so it moves with the arrow keys, the mouse and history recall alike. Toggling `UseBlockCaret` back to `False` removes the renderer and restores the editor's own caret.

To put a block caret on some *other* `TextEditor`, attach the behavior instead.

## Accessibility

`CommandBoxAutomationPeer` reports the control as `AutomationControlType.Edit` and implements the Value pattern over the command text. When history navigation replaces the text a value-changed notification is raised, so a screen reader announces the recalled command. With no `AutomationProperties.Name` set, the peer falls back to the `Watermark` so the box is never announced as unnamed.

Every affordance has a keyboard path; nothing is mouse-only.

## See also

- [AvalonEditBlockCaretBehavior](./AvalonEditBlockCaretBehavior.md) &mdash; the block caret for any `TextEditor`.
- [BlockCaretBehavior](./BlockCaretBehavior.md) &mdash; the block caret for a plain WPF `TextBox`.
- [SyntaxEditor](./SyntaxEditor.md) &mdash; the multi-line, syntax highlighting sibling.
