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
| Terminal Font Size | The default font size for new session tabs. Zooming a terminal with `Ctrl` + mouse wheel updates this value. |

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

Choose the **Light**, **Dark**, or Mosaic **Blue** theme from **Setup → Theme**. The ◐
button in the title bar remains a quick light/dark toggle; from Blue it switches to
Light. The selected theme is remembered between runs.
