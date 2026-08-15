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

## Classic ANSI and responsive displays

Each BBS profile chooses one of two display modes:

- **Classic ANSI 80 × 25** keeps the screen at the dimensions used by most DOS BBSes.
  Its **Use bundled CP437 font** profile option selects PxPlus IBM VGA8 so ANSI art, boxes,
  and menus resemble their original DOS presentation. Turn that option off to keep the
  80 × 25 grid while using the font selected in **Options** (Cascadia Mono by default).
  The CP437 font option is off by default.
- **Responsive to window** changes the row and column count to use the available space.
  This is useful for modern shells, MUDs, and BBS software that adapts to Telnet window
  size reports.

Use **Terminal → Full Screen**, or press `Alt+Enter`, to give the active session the whole
display. The title bar, menu, and BBS Directory are hidden. Press `Alt+Enter` again to
restore the normal window.

## Terminal emulation and TTYPE

*Terminal emulation* tells BBS Navigator how to interpret control codes sent by a remote
system. It is different from *text encoding*, which maps individual byte values to
characters.

| Emulation | Use it for |
| --------- | ---------- |
| ANSI-BBS (classic PC) | Most DOS and modern hobby BBSes. This is the default. |
| VT100 | Systems that specifically request a DEC VT100-compatible terminal. |
| xterm-256color | Modern Unix-like services and software that uses extended colors. |
| VT52 | Older systems that use the earlier DEC VT52 command set. |
| TTY / plain text | Systems that send plain text and should not have escape sequences interpreted. |

During Telnet negotiation the client reports a terminal name, called **TTYPE**, so the
remote system can choose suitable output. Leaving *Telnet TTYPE* blank in the profile
uses the normal value for the selected emulation (`ANSI`, `VT100`, `xterm-256color`,
`VT52`, or `DUMB`). Enter an override only when a board documents a particular value.

## DoorWay mode

Some DOS door games read IBM PC keyboard scan codes instead of ordinary terminal key
sequences. Examples include games or utilities that make heavy use of `Alt`, function,
arrow, Home/End, or Insert/Delete keys.

Turn **DoorWay Off** to **DoorWay On** in the session status bar, or choose
**Terminal → Toggle DoorWay Mode**. While it is on, BBS Navigator sends those extended
keys in the classic DoorWay form: a NUL byte followed by the IBM PC scan code. Ordinary
typing continues normally.

DoorWay mode is normally left off. If a door ignores a function or Alt key, or the BBS
explicitly asks for DoorWay mode, turn it on for that session. Its state is remembered on
the BBS profile.

## Scrollback, lock, and search

Lines that move off the top of the screen are retained in a scrollback buffer. The number
of retained lines is set in [Options](options.md).

When **Live Output** is shown, new BBS output keeps the terminal at the newest line. Scroll
up with the mouse wheel and the session switches to **Scroll Locked**, so incoming text no
longer pulls you away from what you are reading. Click **Scroll Locked**, or use
**Terminal → Toggle Scrollback Lock**, to return to live output.

Choose **Terminal → Search Screen and Scrollback…** to find text in the current screen and
retained history. Opening search automatically locks the scrollback position.

## Copy, paste, and paste pacing

Select terminal text and right-click to **Copy**. Right-click and choose **Paste** to send
clipboard text to the BBS. Clipboard line endings are converted to terminal Enter keys.

**Paste pacing** is a short wait inserted between each pasted character. A modern computer
can otherwise send an entire paragraph almost instantly, while older BBS input routines
were designed for a person typing over a modem and may drop characters when their input
buffer fills. The default is 5 milliseconds per character. Increase it if pasted text is
missing or scrambled; set it to `0` for immediate paste. Normal typing and file transfers
are not slowed. Login macros use the same pacing setting.

## Saved login and quick-send commands

A Telnet profile can use its encrypted saved credentials in a login macro. The default
macro is:

```text
{USERNAME}{ENTER}{PASSWORD}{ENTER}
```

The available tokens are:

- `{USERNAME}` — the saved BBS username.
- `{PASSWORD}` — the saved BBS password.
- `{ENTER}` — one terminal Enter key.

Enable **Send this macro automatically after Telnet connects** in the BBS editor to run
it after every successful connection, including a reconnect. Leave automatic login off
when a board shows changing questions, requires a pause before login, or uses a different
prompt order.

The **Terminal** menu can send the complete login macro, only the saved username, or only
the saved password. Quick-send avoids putting a password on the Windows clipboard. The
app may ask for the credential-encryption passphrase the first time the credentials are
used after startup.

> Telnet itself is unencrypted. Encryption protects credentials while they are stored on
> disk, but text sent to a Telnet BBS—including an automatic password—travels over the
> network without SSH-style encryption.

## Zoom and status information

- **Zoom:** hold `Ctrl` and scroll the mouse wheel to change the font size (8–32 pt).
  The size you choose is remembered as the default for new sessions. In classic mode this
  enlarges the fixed 80 × 25 screen; it does not add columns.
- **Encoding:** the profile's text encoding is shown in the status bar. If art or
  box-drawing characters look wrong, edit the profile and switch the encoding.
- **Keepalive:** after a configurable idle period the client sends a Telnet NOP so
  routers do not silently drop quiet sessions ([Options](options.md)).
- **Statistics:** the status bar shows the active encoding, bytes received/sent, and how
  long the session has been connected.

## Session capture

The **Capture** toggle (also **Transfer → Toggle Session Capture** in the menu) records
everything the board sends to a plain-text log file. ANSI escape sequences are stripped
so the log stays readable. The red dot on the button shows that capture is active; click
the toggle again to stop.

Logs are written to a `Logs` subfolder of your download folder, named after the board and
the current date and time. Use **Transfer → Open Download Folder** to jump there.
