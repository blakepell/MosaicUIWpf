# File Transfers

[← Back to contents](index.md)

BBS Navigator supports the classic BBS transfer protocols in both directions. The
controls live in the status bar at the bottom of every session tab; the same commands are
on the **Transfer** menu and act on the active session.

## Protocols

Pick the protocol in the status-bar dropdown to match what the BBS offers:

| Protocol | Notes |
| -------- | ----- |
| ZMODEM | The best choice when available: streaming, CRC-32, batch transfers, and crash recovery. The de facto standard on late-era boards. |
| YMODEM (batch, 1K) | Batch transfers with 1K blocks; the file name and size travel with the file. |
| XMODEM-1K | Single file, 1024-byte blocks, CRC-16. |
| XMODEM-CRC | Single file, 128-byte blocks, CRC-16. |
| XMODEM (checksum) | The original 1977 protocol; use only when nothing newer works. |

The protocol preselected for new sessions can be changed in [Options](options.md).

## Downloading

1. On the BBS, navigate to its file area and start the download there (the board
   typically asks which protocol you want — choose the same one selected in the client).
2. If the board sends with **ZMODEM**, the client detects the start of the transfer and
   begins receiving automatically — you don't need to do anything (this can be turned
   off in [Options](options.md)).
3. For the other protocols, click **Download** in the status bar after starting the send
   on the board.

A progress card appears over the terminal with the file name, byte counts, transfer rate,
and a **Cancel** button. Received files are saved to your download folder — by default
`Downloads\BBS Navigator` — which you can open at any time with
**Transfer → Open Download Folder**.

With XMODEM variants the sender does not transmit the file name, so you are asked where
to save the file before the transfer starts.

## Uploading

1. On the BBS, start the upload from its file area and choose the protocol.
2. Click **Upload** in the status bar (or **Transfer → Upload Files…**) and pick the
   file to send. ZMODEM and YMODEM allow selecting multiple files; the XMODEM variants
   send a single file.

## If a transfer stalls

- Make sure the protocol chosen in the client matches the one chosen on the board.
- Cancel and retry with a simpler protocol (YMODEM, then XMODEM-CRC) — some older
  boards have quirky ZMODEM implementations.
- Keyboard input is paused during a transfer; cancel the transfer to get the prompt back.
