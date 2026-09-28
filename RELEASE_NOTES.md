# Hazz Music Bingo v.0.1

First public release, based on the previously reviewed internal v0.2.15 source.

## Copyright and licence

Copyright © 2026 Hazz Karaoke. Free use includes paid shows and business use. Unchanged copies may be shared free with all Hazz Music Bingo branding, copyright notices, this licence and third-party notices intact. Redistribution of renamed, rebranded or modified versions requires prior written permission. This is source-available software, not an open-source licence. See `LICENSE.txt` included with the downloads and in the repository.

## New and improved

- Fixed card font changes taking effect one selection late; designer and printer now share the same renderer.
- Four card themes, footer text, row shading, grid thickness, padding and left-aligned labels.
- Print preview with card ranges, 1/2/4 cards per sheet, A4/Letter, orientation, margins, ink saver, printer page ranges and PDF printing through Windows.
- Missing-music checks and file relinking; new games exclude unavailable or previously undecodable files.
- Card-number winner checking for a horizontal or vertical line, four corners and full house.
- Persistent game codes, local game history, exact-state reopening and database backups.
- User guide, developer guide, illustrated examples, regression suite and Windows CI.
- Updated SQLite dependencies and bundled .NET runtime.

## Downloads

Choose **Hazz-Music-Bingo-v0.1-Windows-x64.zip** to run the app. Extract it and launch `HazzMusicBingo.exe`; .NET is included. Choose the source ZIP to build or modify it. Both packages include the Hazz Music Bingo licence and documentation; SHA256SUMS.txt lists package hashes.

Existing local databases and structurally valid version-1 game files remain supported. Music and artwork are not bundled. The executable is unsigned. The public version label intentionally changes from the older internal numbering to v.0.1.

## Validation

273 automated checks pass; the standalone EXE also passes native-library, database, print-rendering, save/load and WAV-decoding checks. No known vulnerable NuGet packages were reported by the release audit. Physical audio, printer drivers and actual multi-monitor behaviour need a rehearsal on the event computer.

See `docs/USER_GUIDE.md` for all workflows and `docs/RELEASE_VALIDATION.md` for the test boundary.
