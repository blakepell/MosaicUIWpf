# The BBS Directory

[← Back to contents](index.md)

The BBS Directory panel on the left side of the main window holds every system you have
saved. Double-click an entry to connect; the entry's icon lights up while its session is
open, and hovering over an entry shows when you last connected to it. Click the star below
the icon, or use **Toggle Favorite** on the entry's context menu, to mark a favorite.
Each successful connection is counted on the saved profile; failed connection attempts do
not increase that count.

Use the search box above the list to filter the directory as you type.

## Connecting

Double-clicking an entry — like **Connect** on its context menu, and **File → Connect** —
opens a Telnet session on the profile's Telnet port. **Connect** is available whenever the
profile has a host name and a Telnet port. A system that offers SSH only — its Telnet port
is `0` — opens over SSH on a double-click instead.

**Connect with SSH** opens the same session over SSH instead, and is available only when
the profile has a host name and an SSH port. SSH always needs a login: if the system has
[saved credentials](#saved-credentials), they are unlocked and used; otherwise you are
asked for a username and password for that session only. An SSH session tab is labeled
`(SSH)`, and a system can have a Telnet and an SSH session open at the same time.

A system that offers **both** Telnet and SSH shows its host and port badge in blue rather
than green, so dual-transport boards stand out in the list. A board that offers SSH only
shows its SSH port on that badge.

Everything else works the same on both transports: text encodings, session capture,
automatic reconnection, and ZMODEM/YMODEM/XMODEM file transfers. The keepalive interval in
[Options](options.md) applies to Telnet; SSH sessions use the SSH protocol's own keepalive.

### SSH certificates

An SSH session can authenticate with a private key instead of — or in addition to — a
password. Set **SSH certificate** in the system's details to the path of an OpenSSH or PEM
private key; the path is saved with the profile, and the key itself is never copied into
BBS Navigator. The SSH login prompt shows the same field, so a certificate can also be
chosen while connecting, and the choice is written back to the profile.

When a certificate is chosen, the password becomes optional: public key authentication is
offered first and the password, if one is present, is used as a fallback. If the key is
protected by a passphrase, you are asked for it at connect time. The key passphrase is
used for that session only and is never saved.

Use **Sort** on the directory toolbar to build a multi-level ordering by display name,
favorite status, last connection, connection count, or host. Each level can be ascending
or descending, and levels are applied from top to bottom. The resulting order is stored
with the profile list.

## Adding and editing systems

Use the **Add**, **Edit**, and **Remove** buttons in the panel's toolbar, or the same
commands on the **Directory** menu. You can also right-click a system and choose **Edit BBS
Details…**. A connection profile has these settings:

| Setting | Meaning |
| ------- | ------- |
| Display name | The name shown in the directory and on the session tab. |
| Host name | The telnet host, e.g. `bbs.example.com`. |
| Telnet port | Usually `23`; some boards use a custom port. Set it to `0` when the board offers SSH only. |
| SSH port | The port the board listens on for SSH. Leave it at `0` when the board does not offer SSH. |
| SSH certificate | Optional private key file used to authenticate SSH sessions. Leave it blank to authenticate with a password. |
| Description | Free-form notes about the system. |
| Text encoding | How received bytes are turned into text — see below. |
| Terminal emulation | How escape sequences are interpreted: ANSI-BBS, VT100, xterm-256color, VT52, or plain TTY. |
| Display mode | Fixed classic 80 × 25, or a responsive grid that follows the window. |
| Use bundled CP437 font | In classic mode, use IBM VGA8 for a DOS-like presentation. It is off by default so the terminal uses the font selected in Options. |
| Telnet TTYPE | Optional terminal name reported to the BBS. Leave blank to use the selected emulation's normal value. |
| Reconnect automatically | Re-establishes the session after a dropped connection, after the delay set in [Options](options.md). |
| Show typed characters locally | Local echo. Enable for MUDs and boards that do not echo your keystrokes back; leave off if you see doubled characters. |
| Send DEL (0x7F) for Backspace | Most classic boards expect Ctrl-H/BS (0x08), so leave this off unless Backspace misbehaves on a particular system. |
| Start sessions in DoorWay mode | Sends DOS scan codes for extended keys used by some door games. Usually leave this off. |
| Login macro | Tokenized text that can send the encrypted username and password. See [Saved login and quick-send commands](terminal.md#saved-login-and-quick-send-commands). |
| Log in automatically | Sends the login macro after every successful Telnet connection. |

### Text encodings

- **IBM PC / DOS (CP437)** — the DOS-era code page classic boards use for box-drawing
  characters and ANSI art. The right default for almost every BBS.
- **UTF-8** — for modern telnet services, MUDs, and boards updated for Unicode.
- **Latin-1 (ISO 8859-1)** — common on European systems from the early internet era.

Editing a profile's host or port while its session is open closes that session tab (the
old connection would no longer match the profile).

## Saved credentials

Right-click a system and choose **Edit Credentials…** to save its username and password.
The first time you do this, BBS Navigator asks you to set one app-wide encryption
passphrase. That passphrase is reused for every system, so you do not need to create a
different key for each connection. During later app sessions, the first credential view or
edit asks you to unlock the key once; further credential actions reuse it until BBS
Navigator closes.

BBS Navigator does not save the plaintext passphrase. Instead, `AppSettings.json` contains
an encrypted verifier used to confirm the key you enter. Usernames and passwords are
encrypted with AES-256-GCM using a key derived from that passphrase, and each encrypted
record has its own random salt and nonce.

**View Credentials…** is enabled only when the selected system has a saved credential
record. You can select and copy the revealed values into the terminal as needed. **Edit
Credentials…** shows the decrypted username and password after the app-wide passphrase is
unlocked. Use **Edit Credentials… → Remove Credentials** to delete them.

The encrypted settings are portable: copy the settings to another computer and use the
same app-wide passphrase to decrypt them there. If you forget the passphrase, the
credentials cannot be recovered.

> Credential encryption protects the saved values at rest. Classic Telnet is not an
> encrypted network protocol. If automatic login or a Terminal quick-send command is used,
> the decrypted text travels across the network without SSH-style encryption.

## Importing a BBS list

**Directory → Import BBS List…** imports systems in bulk from a CSV file in the *bblist*
format published by the [Telnet BBS Guide](https://www.telnetbbsguide.com/). The file
must contain `bbsName`, `TelnetAddress`, and `bbsPort` columns; the optional `sshPort`
column is read when present. A row that lists neither port is imported on the standard
Telnet port `23`. A row that lists only an SSH port is imported with a Telnet port of `0`,
so it connects over SSH alone. Rows without a host are skipped and counted in the summary
shown after the import.

A system already in your directory — matched on host and port — is not added twice.
Instead, its imported fields (display name and SSH port) are refreshed when the CSV
carries different values, and the summary reports how many entries were updated.
Re-importing the same file is a no-op: an endpoint that appears more than once within
one file keeps its first row, so repeated imports do not shuffle those entries.

Imported systems default to CP437 encoding, which suits the vast majority of listed
boards. You can edit any entry afterward to adjust its settings.

## Removing a system

Select the entry and choose **Remove** (you will be asked to confirm). If the system has
an open session, its tab is closed as part of the removal.
