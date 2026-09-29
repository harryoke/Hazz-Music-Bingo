# Hazz Music Bingo v.0.3 — themed game buttons and MP3 tags

Prepare decade-specific music bingo games, switch between eight saved games and audience themes, and read song titles/artists directly from MP3 tags.

## Download and start

Download **Hazz-Music-Bingo-v0.3-Windows-x64.zip**. Extract the entire ZIP and run **HazzMusicBingo.exe**. Keep **TagLibSharp.dll** beside the executable. Windows x64 is required; .NET is included. The executable is unsigned. Music is not included.

1. Scan your music folder. MP3 title and artist tags are read automatically; missing fields fall back to filenames.
2. Under GAME, check MUSIC SOURCE FOR NEW GAME. A successful scan selects that folder; choose whether to include subfolders. At least 60 playable scanned songs are required in the selection.
3. Generate the game, design/print its cards, apply the sold-card range and save it.
4. Click an empty header button to assign the save, a custom label/colour and its audience theme.
5. Play for Line, then Four Corners, then Full House. Acknowledge winners and switch rules without resetting songs.

[Quick start](https://github.com/harryoke/Hazz-Music-Bingo/blob/v.0.3/docs/QUICK_START.md) · [Full user guide](https://github.com/harryoke/Hazz-Music-Bingo/blob/v.0.3/docs/USER_GUIDE.md) · [Development](https://github.com/harryoke/Hazz-Music-Bingo/blob/v.0.3/docs/DEVELOPMENT.md)

## New in v.0.3

- Hazz Music Bingo logo in the main GUI and documentation, plus a branded Windows icon.
- Eight saved-game buttons with user-defined labels and colours. Each retains an audience theme and owned copies of its save/background image. Returning to a button resumes its local game progress.
- Separate font, size, colour, bold and italic controls for WE ARE PLAYING FOR… and … WON! messages, with live preview and per-button theme persistence.
- Folder-only game generation, with optional subfolders and an All music choice. Insufficient selections never pull songs from other folders.
- Read-only MP3 title/artist tags: ID3v1 and ID3v2, Unicode, multiple performers and independent filename fallback. Rescan existing folders to refresh labels.
- Updated illustrated documentation, quick start, regression coverage and third-party notices/source.

## Upgrading and compatibility

Close the old app and extract this package into a new folder. The same Windows account retains its normal library and history. Existing version-1 game saves remain supported. Back up your profile before an event.

Button assignments live in game-shortcuts.json, with owned saves/artwork under game-buttons/ in the app data folder. Include these alongside the database and design settings in full-profile backups. Database-only backups and portable game saves do not include button assignments. Music is referenced by path, not copied.

The generation source starts at All scanned music after restarting: check it before generating. Existing games retain their locked song pools. Rescanning updates shared song labels, including existing game displays; do this before printing event cards. Changing the original assigned save does not change a button automatically: reassign it for a new snapshot.

## Validation and package contents

Windows Release build: zero warnings/errors. **357 regression checks passed**, including 20 independently generated card sets. Packaged executable self-test passed SQLite loading, card generation, WPF print rendering, archive round trip, Windows WAV decoding and MP3 tag reading. Dependency audit reported no known vulnerable packages. Physical speakers, printers and multiple displays still require an event-machine rehearsal.

The Windows ZIP contains the EXE, required tag-reader DLL, full documentation, quick start, licence, third-party notices and library source. The Source ZIP contains this release's application source and tests. SHA256SUMS.txt provides hashes for both archives.

## Copyright and licence

Copyright © 2026 Hazz Karaoke. Free use includes paid and commercial shows. Unchanged copies may be shared free with branding/notices intact. Redistribution of modified, renamed or rebranded app versions requires written permission. Third-party rights remain applicable; TagLibSharp is separately licensed under LGPL-2.1 and remains replaceable. See LICENSE.txt and THIRD_PARTY_NOTICES.md.
