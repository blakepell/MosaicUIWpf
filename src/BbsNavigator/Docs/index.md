# BBS Navigator User Guide

BBS Navigator is a modern telnet client for classic bulletin board systems. It keeps a
directory of your favorite boards, opens each session in its own dockable terminal tab,
and speaks the classic file transfer protocols (ZMODEM, YMODEM, and XMODEM) that boards
have used since the dial-up era.

## Contents

- [Getting Started](getting-started.md) — first launch, adding a board, and connecting.
- [The BBS Directory](bbs-directory.md) — managing saved systems and importing BBS lists.
- [Terminal Sessions](terminal.md) — the terminal tab, text encodings, zoom, and session capture.
- [File Transfers](file-transfers.md) — downloading and uploading with ZMODEM, YMODEM, and XMODEM.
- [Options](options.md) — application-wide settings.

## Quick reference

| Action | How |
| ------ | --- |
| Connect to a board | Double-click it in the BBS Directory |
| Add a board | **Directory → Add BBS…** or `Ctrl+N` |
| Save or view login details | Right-click a board in the BBS Directory |
| Download files | Start the download on the BBS, then **Transfer → Download Files…** |
| Zoom the terminal font | Hold `Ctrl` and scroll the mouse wheel |
| Zoom this guide | Point at the guide, hold `Ctrl`, and scroll the mouse wheel |
| Choose a theme | **Setup → Theme → Light, Dark, or Blue** |
| Quick-toggle light/dark | The ◐ button in the title bar |
| Open this guide | **Help → User Guide** |

The guide opens as the first document when BBS Navigator starts. Links in the contents
stay inside the guide; use **Back** and **Contents** above the page to move around. The
guide remembers the font size selected with `Ctrl` + mouse wheel between runs.

> **New to BBSing?** Public boards are still online and free. A good starting point is the
> [Telnet BBS Guide](https://www.telnetbbsguide.com/), which maintains a directory of live
> systems — its list can be imported directly, see
> [Importing a BBS list](bbs-directory.md).

BBS Navigator is built with the [Mosaic UI for WPF](https://github.com/blakepell/MosaicUIWpf)
control library.
