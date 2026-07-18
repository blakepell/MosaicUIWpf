# The BBS Directory

[← Back to contents](index.md)

The BBS Directory panel on the left side of the main window holds every system you have
saved. Double-click an entry to connect; the entry's icon lights up while its session is
open, and hovering over an entry shows when you last connected to it.

Use the search box above the list to filter the directory as you type.

## Adding and editing systems

Use the **Add**, **Edit**, and **Remove** buttons in the panel's toolbar, or the same
commands on the **Directory** menu. A connection profile has these settings:

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
Choose an encryption passphrase that you will remember; BBS Navigator does not save that
passphrase. The username and password are encrypted together in `AppSettings.json` using
AES-256-GCM with a key derived from the passphrase. Each credential record has its own
random salt and nonce.

**View Credentials…** is enabled only when the selected system has a saved credential
record. Enter the same passphrase to reveal the values. You can select and copy them into
the terminal as needed. Use **Edit Credentials… → Remove Credentials** to delete them.

The encrypted settings are portable: copy the settings to another computer and use the
same passphrase to decrypt them there. If you forget the passphrase, the credentials
cannot be recovered; replace or remove the encrypted record instead.

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
