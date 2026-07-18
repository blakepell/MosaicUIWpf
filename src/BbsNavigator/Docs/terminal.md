# Terminal Sessions

[← Back to contents](index.md)

Each connection opens in its own tab. A session tab has three parts: a status banner at
the top, the terminal itself, and a status bar at the bottom.

## Status banner

The banner shows a badge with the connection state (Connecting, Connected, Disconnected)
and a message with details such as connection errors. Two buttons sit on the right:

- **Reconnect** — drops the current connection, if any, and connects again.
- **Disconnect** — ends the session but leaves the tab open so you can reconnect later.

If the profile has *Reconnect automatically* enabled, a dropped connection is retried
after the delay configured in [Options](options.md).

## The terminal

Everything you type is sent to the board, and the board's output — including ANSI color
and cursor control — is rendered in the terminal.

- **Zoom:** hold `Ctrl` and scroll the mouse wheel to change the font size (8–32 pt).
  The size you choose is remembered as the default for new sessions.
- **Encoding:** the profile's text encoding is shown in the status bar. If art or
  box-drawing characters look wrong, edit the profile and switch the encoding — see
  [text encodings](bbs-directory.md).
- **Keepalive:** after a configurable idle period the client sends a telnet NOP so
  routers do not silently drop quiet sessions ([Options](options.md)).

## Status bar

From left to right:

- **Protocol picker and Download / Upload buttons** — see [File Transfers](file-transfers.md).
- **Capture toggle** — see below.
- **Statistics** — the active encoding, bytes received/sent, and how long the session
  has been connected.

## Session capture

The **Capture** toggle (also **Transfer → Toggle Session Capture** in the menu) records
everything the board sends to a plain-text log file. ANSI escape sequences are stripped
so the log stays readable. The red dot on the button shows that capture is active; click
the toggle again to stop.

Logs are written to a `Logs` subfolder of your download folder, named after the board and
the current date and time — for example
`Downloads\BBS Navigator\Logs\My Favorite BBS 2026-07-17 213000.log`. Use
**Transfer → Open Download Folder** to jump there.
