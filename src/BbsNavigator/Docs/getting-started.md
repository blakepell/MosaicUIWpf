# Getting Started

[← Back to contents](index.md)

## The main window

The window is split into two areas:

- **BBS Directory** (left) — your saved systems. The panel behaves like an IDE tool
  window: drag its title to float it, use the pin icon to auto-hide it, or resize it by
  dragging its edge.
- **Document area** (right) — a tab opens here for every session you start. Tabs can be
  rearranged, split side by side, or floated into their own windows.

Above the document area is a traditional menu bar (File, Transfer, Directory, Setup,
Help), and the title bar has a ◐ button that switches between the light and dark theme.

## Add your first board

1. Choose **Directory → Add BBS…** (or press `Ctrl+N`).
2. Enter a display name, the host name, and the telnet port (usually `23`).
3. Pick a text encoding — **CP437** is the right choice for almost every classic board,
   because it renders DOS box-drawing and ANSI art correctly.
4. Click **Save**. The board appears in the directory.

The other profile settings (auto-reconnect, local echo, backspace behavior) are described
in [The BBS Directory](bbs-directory.md).

## Connect

Double-click the board in the directory (or select it and choose **File → Connect**).
A terminal tab opens and the connection starts. The icon next to the board in the
directory and the status badge above the terminal both show the connection state.

To end a session, click **Disconnect** in the terminal's status banner, or close the tab.
Closing the application disconnects all sessions cleanly.

## Where to next

- [Terminal Sessions](terminal.md) — everything inside a session tab.
- [File Transfers](file-transfers.md) — downloading files from a board.
