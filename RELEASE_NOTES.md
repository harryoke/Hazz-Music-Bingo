# Hazz Music Bingo v.0.2 — live winner tracking

Host a complete Line → Four Corners → Full House game with automatic winner detection and clear audience messages.

## Downloads and quick start

Download **Hazz-Music-Bingo-v0.2-Windows-x64.zip**, extract it, and run **HazzMusicBingo.exe**. Windows x64 is required; .NET is included. The executable is unsigned. Music is not included.

1. Scan at least 60 playable songs and check for missing files.
2. Generate a game, design cards, then preview/print the required cards.
3. Enter the first and last sold card numbers under PLAYING FOR and press Apply.
4. Select Line, show the audience screen, save the game and start playing clips.
5. Verify and acknowledge winning cards, then select Four Corners and later Full House. Do not reset songs between prizes.

[Quick-start guide](https://github.com/harryoke/Hazz-Music-Bingo/blob/v.0.2/docs/QUICK_START.md) · [Full user guide](https://github.com/harryoke/Hazz-Music-Bingo/blob/v.0.2/docs/USER_GUIDE.md) · [Development guide](https://github.com/harryoke/Hazz-Music-Bingo/blob/v.0.2/docs/DEVELOPMENT.md)

The Windows ZIP includes the full documentation, quick start, illustrated examples, licence and third-party notices. The source ZIP contains the corresponding complete source and tests. SHA256SUMS.txt verifies both packages.

## Changes

- Previous Card / Next Card in Check Winning Card, with saved-number navigation and end boundaries.
- Line means any complete horizontal OR vertical line; diagonal wins are removed. Four Corners and Full House remain.
- Validated inclusive sold-card ranges and a visible count of cards in play.
- Automatic detection of every qualifying card in the range as played state changes.
- Persistent acknowledgement prevents repeated attention alerts for the same card/rule. New winners still appear.
- Prominent selected-rule buttons and audience messages such as WE ARE PLAYING FOR A LINE and LINE WON!.
- Progress is preserved when switching rules; current-song and played-list audience views remain available.

## Saved games and upgrades

Close v.0.1 before launching v.0.2. The normal local library and game history are retained. Back up the database before an event. An additive GameWinningSettings table stores the rule, sold range and acknowledgements. Version-1 portable saves gain an optional Winning object; older saves retain their cards and progress and default to Line with tracking off until a range is applied. Reset clears played songs and acknowledgements while retaining the rule and range. Loading a portable file still reshuffles playback order by design; history reopening preserves it.

## Validation

Windows Release build passed with zero warnings/errors. All **311 regression checks** passed, including 20 generated card sets, old saves, live winner filtering, both diagonal exclusions, progress-preserving rule switching and actual WPF navigation/acknowledgement button events. The self-contained executable passed SQLite loading, card generation, print rendering, archive round trip and Windows WAV decoding. See the included validation record for scope. Physical speakers, printer drivers and multiple displays still require an event-machine rehearsal.

## Copyright and licence

Copyright © 2026 Hazz Karaoke. Free use includes paid and commercial shows. Unchanged copies may be shared free with branding and notices intact. Redistribution of modified, renamed or rebranded versions requires written permission. Third-party licences remain applicable. See LICENSE.txt and THIRD_PARTY_NOTICES.md.
