# VT52Terminal

A terminal emulator hosted inside an AvalonEdit TextEditor. Call `Add(string)` or
`Add(byte[])` with remote data and assign an `ITerminalConnection` for keyboard input and
terminal responses.

The control supports ANSI/VT parsing, VT52 mode, plain TTY mode, 256-color display,
scrollback, alternate screens, OSC titles, bracketed paste, DoorWay DOS scan codes, and
both responsive and fixed-size terminal grids.

Important properties include:

- `EmulationMode` — ANSI, VT52, or plain TTY parsing.
- `AutoResizeTerminal` — follows the control size when true; call `Resize(rows, columns)`
  and set it false for a fixed grid.
- `DoorwayMode` — sends DOS extended keys as NUL plus IBM PC scan code.
- `MaxScrollbackLines` — bounds retained terminal history.
- `PasteCharacterDelay` — optional millisecond delay between pasted characters.

`SendTextAsync` sends programmatic text with the same optional pacing used by paste, and
`OpenSearch` opens incremental search across the current screen and retained scrollback.

![VT52Terminal](./images/VT52Terminal.png)

