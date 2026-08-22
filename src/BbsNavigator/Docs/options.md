# Options

[← Back to contents](index.md)

Open the options window with **Setup → Options…**, or [open it now](app:options).
Settings are grouped by category, take effect immediately, and are saved when BBS
Navigator closes.

## Appearance

| Setting | Meaning |
| ------- | ------- |
| User Guide Font Size | The saved base font size for this guide. Point at the guide and use `Ctrl` + mouse wheel to change it. |

## Terminal

| Setting | Meaning |
| ------- | ------- |
| Terminal Font | The normal terminal font. Classic 80 × 25 profiles can either override it with the bundled IBM VGA8 font or continue using this selection. |
| Terminal Font Weight | The font weight used by responsive terminal sessions. |
| Terminal Font Size | The default font size for new session tabs. Zooming a terminal with `Ctrl` + mouse wheel updates this value. |
| Scrollback Lines | How many lines that have moved off the top remain available for review and search. The default is 5,000. |
| Paste Pacing (milliseconds) | The wait between pasted characters and login-macro characters. Older BBS software can lose text when a whole paste arrives instantly. The default is 5 ms; `0` disables pacing. |
| Copy Selected Text Automatically | Copies selected terminal text directly to the clipboard. This is disabled by default. |

## Connections

| Setting | Meaning |
| ------- | ------- |
| Reconnect Delay (seconds) | How long to wait before an automatic reconnection attempt (for profiles with auto-reconnect enabled). |
| Connect Timeout (seconds) | How long a connection attempt may take before it is abandoned. |
| Keepalive Interval (seconds) | Sends a telnet NOP after this much idle time so routers do not drop quiet sessions. Set to `0` to disable keepalives. |

## File Transfers

| Setting | Meaning |
| ------- | ------- |
| Download Folder | Where downloaded files and session capture logs are saved. Defaults to `Downloads\BBS Navigator`. |
| Default Protocol | The transfer protocol preselected in new session tabs — see [File Transfers](file-transfers.md). |
| Auto-start ZMODEM Downloads | Starts receiving automatically when the remote system begins a ZMODEM send. |

## File System

| Setting | Meaning |
| ------- | ------- |
| Application Data Folder | Where BBS Navigator stores its settings and your BBS directory (read-only, shown for reference). |

## Theme

New installations start with the Mosaic **Blue** theme. Choose **Light**, **Dark**, or
**Blue** from **Setup → Theme**. The ◐
button in the title bar remains a quick light/dark toggle; from Blue it switches to
Light. The selected theme is remembered between runs.
