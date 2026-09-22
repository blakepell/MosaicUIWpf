# CommandBox

**Base class:** `ICSharpCode.AvalonEdit.TextEditor`  
**Namespace:** `Mosaic.UI.Wpf.Controls`  
**Source:** `src/Mosaic.UI.Wpf/Controls/CommandBox/CommandBox.cs`  
**Example:** `src/MosaicWpfDemo/Views/Examples/CommandBoxExample.xaml`  
**Docs:** `docs/CommandBox.md`

## Description

A single-line command input built on AvalonEdit. `Enter` raises `CommandExecuted`, `Up` / `Down` walk a persistent command history, `Tab` completes from that history, and an optional block caret gives the box a terminal feel. It has no completion window, no syntax highlighting and no persistence — it dispatches the command and gets out of the way.

Use it for a terminal, MUD, REPL, chat or debug-console input line. For multi-line code editing use `SyntaxEditor` instead.

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `CommitBehavior` | `CommandBoxCommitBehavior` | `SelectAll` | `Clear` blanks the box, `Keep` leaves text and caret alone, `SelectAll` keeps the text and selects it so the next keystroke replaces it. |
| `IsHistoryEnabled` | `bool` | `true` | Records commands and enables the arrow-key walk. |
| `MaxHistoryItems` | `int` | `100` | Eviction limit; 0 means unlimited. |
| `HistoryDuplicatePolicy` | `HistoryDuplicatePolicy` | `SkipConsecutive` | `Allow`, `SkipConsecutive`, or `SkipAll`. |
| `IsTabCompletionEnabled` | `bool` | `true` | `Tab` completes from the most recent matching history entry; no match falls through to focus navigation. |
| `ClearOnEscape` | `bool` | `true` | `Escape` clears the box. |
| `SelectAllOnFocus` | `bool` | `true` | Selects existing text on keyboard focus. |
| `SelectAllOnMouseFocus` | `bool` | `true` | First click focuses and selects all; the click is swallowed. |
| `TrimCommand` | `bool` | `true` | Trims the command before dispatch. |
| `AllowEmptyCommands` | `bool` | `false` | Whether a bare `Enter` dispatches an empty command. |
| `ShowPrompt` | `bool` | `false` | Draws a prompt glyph at the left edge, inside the border (an AvalonEdit left margin). |
| `Prompt` | `string` | `">"` | The glyph. Decoration only; never part of `Text` or the command. |
| `PromptBrush` | `Brush?` | `null` | Null uses `Foreground`. |
| `PromptPadding` | `Thickness` | `2,0,6,0` | Left gap from the border, right gap before the typed text. |
| `Watermark` | `string` | `""` | Placeholder text shown while empty. |
| `WatermarkBrush` | `Brush?` | `null` | Null uses `Foreground` at half opacity. |
| `UseBlockCaret` | `bool` | `false` | Installs the shared AvalonEdit block caret renderer. |
| `BlockCaretBrush` | `Brush?` | `null` | Null uses `Foreground`. |
| `BlockCaretTextBrush` | `Brush?` | `null` | Null picks a contrasting color automatically. |
| `BlockCaretOpacity` | `double` | `0.8` | Opacity of the block. |
| `Command` / `CommandParameter` | `ICommand?` / `object?` | `null` | MVVM hook; a null parameter passes the command text. |
| `History` | `CommandBoxHistory` | — | Read-only history list and cursor. |

There is deliberately **no** bindable `CommandText` property — the command arrives through the event.

## Events

| Event | Routing | Args | Description |
|---|---|---|---|
| `PreviewCommandExecuted` | Tunnel | `CommandExecutedEventArgs` | Rewrite `e.Command`, or veto with `e.Handled = true`. |
| `CommandExecuted` | Bubble | `CommandExecutedEventArgs` | The command was dispatched. |
| `EscapePressed` | Bubble | `RoutedEventArgs` | `Escape` was pressed, cleared or not. |

`CommandExecutedEventArgs.Command` is the normalized single-line text; `AddToHistory` (default `true`) can be cleared to keep a command out of the history.

## Methods

| Member | Description |
|---|---|
| `ExecuteCommand()` | Runs the full dispatch pipeline from code; returns whether a command was dispatched. |
| `ImportHistory(IEnumerable<string>?)` | Seeds the history from persisted commands, oldest first. |
| `ClearHistory()` | Empties the history. |

## CommandBoxHistory

UI-free and unit tested. `Items`, `Count`, `MaxItems`, `DuplicatePolicy`, `IsNavigating`, `Add`, `Import`, `MoveBack`, `MoveForward`, `ResetCursor(draft)`, `Clear`.

Cursor semantics match a shell: the cursor sits past the end when idle; the control calls `ResetCursor(Text)` on every keystroke so the in-progress text becomes the **draft**; `Up` clamps at the oldest entry; `Down` past the newest restores the draft; `Add` always resets the cursor.

## Notes

- Single line is enforced: word wrap and scroll bars off, the editor's newline commands removed, `Enter` handled with or without modifiers, pasted line breaks collapsed to spaces, and a catch-all that collapses any break that still reaches the document. `MeasureOverride` reports exactly one line height.
- Subclasses `TextEditor` rather than `CustomControl` (same as `SyntaxEditor` and `VT52Terminal`) because AvalonEdit's editing pipeline is not template-friendly. There is no `.xaml` and no `Themes/Generic.xaml` entry; theme defaults are `DynamicResource` references applied in the constructor.
- `CommandBoxAutomationPeer` reports `AutomationControlType.Edit`, implements the Value pattern, and raises a value-changed notification when history navigation replaces the text.

## XAML

```xml
<mosaic:CommandBox Watermark="Enter a command..."
                   ShowPrompt="True"
                   Prompt="&gt;"
                   UseBlockCaret="True"
                   CommitBehavior="SelectAll"
                   MaxHistoryItems="200"
                   CommandExecuted="OnCommandExecuted" />
```

## See also

- `AvalonEditBlockCaretBehavior` — the same block caret for any other `TextEditor` (see [AvalonEditBehaviors.md](AvalonEditBehaviors.md)).
- [SyntaxEditor.md](SyntaxEditor.md) — the multi-line, syntax highlighting sibling.
