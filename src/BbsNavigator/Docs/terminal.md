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
after the delay configured in [Options](options.md). With **Clear Screen on Reconnect**
enabled (the default), the terminal is reset before each reconnect so the new session starts
on a clean screen.

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

## Numeric keypad

Some games move with the numeric keypad (8 = up, 2 = down, 7 = up-left, and so on). Click
**Keypad Numbers** in the session status bar to switch to **Keypad Arrows**, or choose
**Terminal → Toggle Keypad Arrows**. While it is on, the keypad sends arrow, Home, End,
Page Up/Down, Insert, and Delete keys as if Num Lock were off, whatever the keyboard's
Num Lock light shows. Keypad 5 still types `5`.

The starting state comes from the profile's *Start sessions with the numeric keypad as
arrow keys* option, and toggling it during a session updates the profile. With keypad
arrows off, the keypad follows the keyboard's own Num Lock state.

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

Enable **Run the selected login method after Telnet connects** in the BBS editor to run
it after every successful connection, including a reconnect. Leave automatic login off
when using the legacy macro on a board that shows changing questions, requires a pause, or uses a different
prompt order.

The **Terminal** menu can send the complete login macro, only the saved username, or only
the saved password. Quick-send avoids putting a password on the Windows clipboard. The
app may ask for the credential-encryption passphrase the first time the credentials are
used after startup.

> Telnet itself is unencrypted. Encryption protects credentials while they are stored on
> disk, but text sent to a Telnet BBS—including an automatic password—travels over the
> network without SSH-style encryption.

### Prompt-aware login steps

In **Edit BBS**, enable **Use prompt-aware steps** and choose **Edit steps…**.
The step editor starts with a username/password example; adjust its prompt text to
match your board. Actions run in order:

- **WaitForText** waits for literal prompt text, without case sensitivity. ANSI color
  codes are ignored, and prompts may span multiple network packets. **Seconds** is
  the timeout (1–300 seconds).
- **SendUsername** and **SendPassword** use saved encrypted credentials.
- **SendText** sends the literal text in that row. Use a separate **Enter** step to
  submit it. Do not put passwords into a SendText row.
- **Delay** waits for the specified number of seconds.

Use **Send Login Macro** to run the selected method manually, or enable automatic
login to run it after a Telnet connection opens. Existing profiles keep using their
original macro until prompt-aware steps are enabled. A timeout stops the sequence
before subsequent responses are sent. **Stop sending** cancels a wait or send, and
disconnecting or starting a file transfer also stops it.

## Reviewing and stopping pasted text

Multiline clipboard text opens a review window by default. Edit the preview, check
the destination board, and set character and extra end-of-line delays before choosing
**Send text**. Text that cannot be represented by the session encoding must be edited
or sent using a compatible encoding; it is not silently replaced with question marks.

Select **Remember these delays for this board** to override the global character delay.
The initial extra line delay is 100 ms. **Options → Preview Multiline Paste** can turn
off routine multiline previews; encoding problems still open the review window.
The session banner shows send progress and **Stop sending**. The same stop command
is available on the Terminal menu. Already transmitted text cannot be recalled.
Normal typing is paused while a login, paste, or composed-message send is active.

## Composing messages locally

Choose **Terminal → Compose Message…** for the active session, or select a saved board
in the directory when no terminal is active. The composer works while disconnected.
Drafts are saved as local text files under the application data folder's `Drafts`
directory, separately for each board, shortly after editing and when closing.

The editor supports spellchecking and **Quote selection**. Choose a wrapping width
(72 columns initially; 0 leaves lines unchanged), then **Preview and send…**. Wrapping
affects the send preview, not the saved draft. Connect to that board and open its BBS
message editor before sending. Your draft remains saved after sending, cancellation,
or a disconnect, and reappears when you reopen the composer. Drafts are ordinary local
text, not encrypted credential records.

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

The **Capture** toggle (also **Terminal → Toggle Session Capture** in the menu) records
everything the board sends to a plain-text log file. ANSI escape sequences are stripped
so the log stays readable. The red dot on the button, and the check mark on the menu item,
show that capture is active; click the toggle again to stop.

Capture is remembered per BBS: turning it on or off updates the board's profile, and later
sessions with that board start capturing automatically. It can also be set with
**Capture sessions to a log file** in the **Edit BBS** dialog.

Logs are written to a `Logs` subfolder of your download folder, named after the board and
the current date and time. Use **Transfer → Open Download Folder** to jump there.
