# The BBS Directory

[← Back to contents](index.md)

The BBS Directory panel on the left side of the main window holds every system you have
saved. Double-click an entry to connect; the entry's icon lights up while its session is
open, and hovering over an entry shows when you last connected to it. Click the star below
the icon, or use **Toggle Favorite** on the entry's context menu, to mark a favorite.
Each successful connection is counted on the saved profile; failed connection attempts do
not increase that count.

Use the search box above the list to filter the directory as you type.

Use **Sort** on the directory toolbar to build a multi-level ordering by display name,
favorite status, last connection, or host. Each level can be ascending or descending, and
levels are applied from top to bottom. The resulting order is stored with the profile list.

## Adding and editing systems

Use the **Add**, **Edit**, and **Remove** buttons in the panel's toolbar, or the same
commands on the **Directory** menu. You can also right-click a system and choose **Edit BBS
Details…**. A connection profile has these settings:

| Setting | Meaning |
| ------- | ------- |
| Display name | The name shown in the directory and on the session tab. |
| Host name | The telnet host, e.g. `bbs.example.com`. |
| Telnet port | Usually `23`; some boards use a custom port. |
| Description | Free-form notes about the system. |
| Text encoding | How received bytes are turned into text — see below. |
| Reconnect automatically | Re-establishes the session after a dropped connection, after the delay set in [Options](options.md). |
| Show typed characters locally | Local echo. Enable for MUDs and boards that do not echo your keystrokes back; leave off if you see doubled characters. |
| Send DEL (0x7F) for Backspace | Most classic boards expect Ctrl-H/BS (0x08), so leave this off unless Backspace misbehaves on a particular system. |

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
> encrypted network protocol, and BBS Navigator does not automatically type or transmit
> stored credentials.

## Importing a BBS list

**Directory → Import BBS List…** imports systems in bulk from a CSV file in the *bblist*
format published by the [Telnet BBS Guide](https://www.telnetbbsguide.com/). The file
must contain `bbsName`, `TelnetAddress`, and `bbsPort` columns; rows without a valid
address and port are skipped and counted in the summary shown after the import.

Imported systems default to CP437 encoding, which suits the vast majority of listed
boards. You can edit any entry afterward to adjust its settings.

## Removing a system

Select the entry and choose **Remove** (you will be asked to confirm). If the system has
an open session, its tab is closed as part of the removal.
