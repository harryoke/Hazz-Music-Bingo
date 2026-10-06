# Hazz Music Bingo v1.1 - game-button dialogue fix

The Edit Game Button dialogue now keeps **Save button** and **Cancel** visible in a fixed footer. The form scrolls, and the window can be resized. Small windows or long theme/background descriptions no longer push these actions out of view.

## Download and upgrade

Download **Hazz-Music-Bingo-v1.1-Windows-x64.zip**, close the old app, extract the entire ZIP into a new folder and run **HazzMusicBingo.exe**. Keep **TagLibSharp.dll** beside the EXE. Windows x64 is required; .NET is included. Music is not included.

Existing games, cards, themes, button assignments and playback settings remain compatible. No data/schema migration is required. All v1.0 features remain included: individual audience text styles, 30-second playback start intervals and the Repeat-to-Next audio fix.

[Quick start](https://github.com/harryoke/Hazz-Music-Bingo/blob/v1.1/docs/QUICK_START.md) | [User guide](https://github.com/harryoke/Hazz-Music-Bingo/blob/v1.1/docs/USER_GUIDE.md) | [Illustrated manual](https://github.com/harryoke/Hazz-Music-Bingo/releases/download/v1.1/Hazz_Music_Bingo_v1.0_Complete_Manual.pdf)

The included 31-page v1.0 manual covers the same game features; its game-button editor screenshot predates this layout correction. The online guide and packaged screenshot show the corrected dialogue.

## Validation

Windows Release build: zero warnings/errors. **419 regression checks passed**, including nine layout assertions across three viewport sizes with a long theme description. The packaged executable self-test passed database loading, card generation, WPF printing, saved-game round trip, WAV decoding and MP3 tag reading. Hardware speaker, printer and display rehearsal remains recommended before an event.

Windows ZIP, source ZIP, illustrated manual and SHA-256 checksums are attached. Licences, third-party notices and dependency source are included in the packages. Copyright 2026 Hazz Karaoke; the existing licence terms continue to apply.
