# Hazz Music Bingo v.0.1

A Windows music-bingo host with custom 5×5 cards, print previews, an audience screen, missing-music checks and winner verification.

[Download Windows x64](https://github.com/harryoke/Hazz-Music-Bingo/releases/tag/v.0.1) · [User guide](docs/USER_GUIDE.md) · [Build and development](docs/DEVELOPMENT.md) · [Testing](docs/TESTING.md) · [Changes](CHANGELOG.md)

## Copyright and licence

Copyright © 2026 Hazz Karaoke. Free use includes paid shows and business use. Unchanged copies may be shared free with all branding and notices intact. Redistribution of renamed, rebranded or modified versions requires written permission. Third-party licences and statutory rights remain unaffected. See [full licence terms](LICENSE.txt). This is source-available software, not an open-source licence.

![Host console](docs/images/host-console.png)

## What it does

- Locks a 60-song game and saves 60 distinct 25-song card layouts.
- Plays short clips with fade-out, Repeat and Stop; shows played songs on a separate audience display.
- Designs cards with installed fonts, themes, artwork, outlines, custom footers and spacing.
- Previews and prints card ranges at 1, 2 or 4 per sheet on A4 or Letter, portrait or landscape. PDF output uses Microsoft Print to PDF.
- Detects missing or previously unreadable music, relinks moved files and excludes unavailable songs from new games.
- Checks a card number for any row/column/diagonal, four corners or full house.
- Reopens game history without changing cards, progress or playback order; creates database backups.

![Card designer](docs/images/card-designer.png)

## Start playing

1. Extract the Windows release ZIP and run **HazzMusicBingo.exe**.
2. Scan a music folder and use **CHECK MUSIC / LOCATE FILES**.
3. Generate a game from at least 60 available songs.
4. Design, preview and print cards; save the game.
5. Choose the winning rule, open the audience screen and play clips.
6. Verify claimed winners by game code and card number.

Music is not included. The app uses your files and Windows audio decoding. Windows x64 is required; the release bundles .NET. Data stays in `%LOCALAPPDATA%\HazzMusicBingo` unless an explicit profile override is set.

## Build and test

```powershell
dotnet build HazzMusicBingo.sln -c Release
dotnet run --project tests/HazzMusicBingo.Regression -c Release --no-build -- artifacts/screenshots
```

Open the solution in Visual Studio with the .NET desktop development workload, or use a .NET 8-compatible SDK on Windows. Use `BUILD_STANDALONE_EXE.bat` or the publish command in the development guide for a standalone executable.

The automated suite uses temporary profiles and covers persistence, card rules, missing files, winner patterns, font changes and WPF print rendering. Physical audio, printer-driver and multi-monitor checks still need a rehearsal on the event machine.

## Compatibility notes

The name **v.0.1** is the first public release label; earlier source archives used an internal v0.2.15 label. Existing local databases receive an additive game-code column. Existing version-1 saved games remain supported when structurally valid. Loading a portable saved game intentionally reshuffles playback order; reopening history preserves it.

Game files store song paths and layouts, not audio or artwork. The app currently targets .NET 8; see the development guide for support and upgrade planning. Third-party components are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
