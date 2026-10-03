# Hazz Music Bingo v1.0

Style every audience text element independently and choose where music clips begin. This release also includes the tested Repeat Last to Play Next Song audio fix.

## Download and quick start

Download **Hazz-Music-Bingo-v1.0-Windows-x64.zip**, extract the whole ZIP into a new folder, and run **HazzMusicBingo.exe**. Keep **TagLibSharp.dll** beside it. Windows x64 is required; .NET is bundled. The EXE is unsigned. Music is not included.

1. Scan your music and choose the folder to use for the new game (at least 60 playable indexed songs).
2. Generate the game, design/print cards, apply the sold-card range and save.
3. Open **AUDIENCE SCREEN DESIGN**, choose a text element and adjust its font, size, colour and outline. Select **Use design**.
4. Under **PLAYBACK**, choose **Start music at (seconds)**: 0, 30, 60, 90 or another multiple of 30. Next and Repeat use this position. If the track is too short, it starts at 0 with a host notice.
5. Play for Line, then Four Corners, then Full House, acknowledging winners and changing rules without resetting progress.

[Quick start](https://github.com/harryoke/Hazz-Music-Bingo/blob/v1.0/docs/QUICK_START.md) | [User guide](https://github.com/harryoke/Hazz-Music-Bingo/blob/v1.0/docs/USER_GUIDE.md) | [31-page PDF manual](https://github.com/harryoke/Hazz-Music-Bingo/releases/download/v1.0/Hazz_Music_Bingo_v1.0_Complete_Manual.pdf)

## New controls

- Eleven independent audience text styles: playing-for message, winner announcement, Now Playing heading, Ready, current song title, current artist, played-list heading, played titles, played artists, page number and empty-list message.
- Each style offers installed font, size 12-140, text colour, bold, italic, outline on/off, outline colour and outline width 0-12. The preview shows the relevant screen. Outlines render behind the fill for readability.
- Audience themes and all eight saved-game buttons retain the individual styles. Existing themes use their old settings until an element is edited.
- Start position dropdown offers 0-600 seconds in 30-second steps; larger steps can be typed up to 86400. Invalid values stop the request before audio starts. Short remaining audio plays only to the track end.
- The chosen start is remembered locally after Next or Repeat and applies across games; it is separate from clip length and game saves.
- Next can interrupt Repeat during steady playback or fade without the retiring clip muting the next song.

## Compatibility and upgrade

Existing databases, valid version-1 game saves, printed cards, live-winning settings and game-button assignments remain supported. No database or game-save schema migration is needed. The normal Windows profile retains your data when you extract the release into a new folder.

Audience theme JSON adds optional per-element TextStyles; missing entries fall back to the legacy fields. playback-settings.json stores the last used start offset. Include it with the database, designs and game-buttons folder in full-profile backups. Music is still referenced by path, not copied. Physical speakers, printers and displays should be rehearsed before an event.

## Validation

Windows Release build: zero warnings/errors. **410 regression checks passed**, including decoded WAV samples at 0, 30 and 60 seconds, short-track fallback, Repeat-to-Next handover, all eleven text styles, theme/button persistence and 20 generated card sets. Packaged executable self-test checks database loading, cards, printing, game archives, WAV decoding and MP3 tags. The dependency audit reported no known vulnerable packages. Hardware speaker output is not claimed by these automated checks.

The release includes Windows and source ZIPs, the updated illustrated PDF manual and SHA-256 checksums. The Windows ZIP also contains the guides, licences and third-party source/notices.

## Copyright and licence

Copyright 2026 Hazz Karaoke. Free use includes paid and commercial shows. Unchanged copies may be shared free with branding and notices intact. Modified or rebranded redistribution requires written permission. Third-party licences and statutory rights remain applicable. TagLibSharp remains separately replaceable under LGPL-2.1; its source and licence are included.
