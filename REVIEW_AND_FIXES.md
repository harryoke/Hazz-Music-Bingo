# Hazz Music Bingo — code review and fixes

Reviewed source: v0.2.15. The supplied ZIP was left unchanged. The accompanying reviewed-source ZIP contains the fixes and a repeatable regression runner. Existing game-file version 1 and the intentional reshuffle-on-load behaviour are retained.

## Fixed

| Issue | Change |
| --- | --- |
| A missing file, decoder failure or audio-output initialization failure could count a song as played before playback even began. | Played progress and current-song displays are updated after the audio output successfully starts. A song stopped by the host after starting still counts as played. |
| Scanning a large library could freeze the host window. | File discovery, decoding and database indexing run in a background task. The scan button prevents overlapping scans, and cancellation is checked while discovering files. |
| Invalid save files could silently discard bad cards and create a partial set that could not subsequently be printed. | Validate version, 60 distinct song paths, durations, card counts/numbers, membership, and all strict card uniqueness rules before importing. Invalid and partial sets produce an error instead of silently changing layouts. |
| Card restoration could fail after the old game had already been closed. | New game, played progress and restored cards commit in one database transaction. A card insertion failure rolls these changes back. Track-library upserts still precede this transaction. |
| Saving directly over a game file could truncate the previous save if writing failed. | Write a temporary file in the same folder, then replace the destination. This improves ordinary write-failure safety; it is not a power-loss durability guarantee. |
| SQLite connection construction treated a file path as connection-string syntax. | Use the connection-string builder; explicitly enable foreign-key checks. An optional database path makes isolated regression testing possible. |
| Assuming screen array index 1 was secondary could choose the primary monitor. | Choose a non-primary display explicitly. |
| Cancelling playback during a failed load/generation could leave playback controls disabled. New games could retain the old title on the host. | Restore playback controls when invalidating playback; clear the displayed title/artist after a successful new game or load. |

## Verification

- Original source and revised source both compile in Release configuration; revised build has zero warnings and errors.
- Regression runner passed **139 assertions**, including **20 independently generated 60-card sets**.
- Checked 25 distinct songs per card, 60 unique song sets, and no repeated song in a grid position across cards.
- Checked exact card-layout save/load round trip, restored progress, reset retaining cards, and reprint not generating extra cards.
- Checked 14 malformed archive cases, preserving the active game after rejection.
- Forced a database failure during card insertion and verified the previous active game remained active.
- Checked loading an archive without cards, missing-file counts, atomic save overwrite and temporary-file cleanup.
- Checked that missing audio does not trigger the playback-start callback.
- Tests use a separate temporary database. Your live app database was not opened or changed.

The app was not exercised interactively with your music library, audio devices, printer or second screen. Actual sound, device disconnection during playback, print margins, mixed-DPI monitors and visual layouts need a practical check before an event. This is a focused reliability review, not a claim that every defect has been eliminated.

## Highest-value improvements next

1. **Add a pre-event library check and “locate missing music” action.** The scanner currently indexes undecodable files with duration zero, and game generation can select missing files from old database entries. Show missing/unplayable/duplicate tracks before locking a game. Let the host replace a root folder when music moves between drives. Decide explicitly how to skip an unavailable song without falsely calling it played.
2. **Add a card-number winner checker.** Select a printed card number and show its marked squares against played tracks. Make the winning pattern explicit: row, column, diagonal or full house. This would reduce disputes during a live event.
3. **Add cue points, volume normalization and an audio-device selector.** Clips currently begin at the start of each song, which can mean silence or long intros. Store a preferred excerpt start per track. Handle audio-output failure during playback with a clear retry/recovery action; successful output startup alone cannot guarantee listeners heard the clip.
4. **Improve recovery and game identity.** Provide a game-history/reopen screen, automatic backup and a visible game/session code on each printed card. Card numbers repeat across games, so a session code would help avoid checking cards from the wrong game. Preserve printed song labels as game snapshots: currently shared library metadata can change when files are rescanned or another archive is imported.
5. **Separate game operations from window event handlers.** MainWindow currently owns playback coordination, persistence and display updates. Move these into a small controller/view-model, serialize game-changing operations, add injectable audio output, and test Stop/Repeat/Next plus Load/Reset transitions. Catch and report failures in currently unguarded close-game/settings-save handlers. Add logging instead of silently swallowing every settings/cleanup exception.
6. **Speed up large-library work.** Batch database inserts in a transaction, skip unchanged files using the stored modification timestamp, and show unreadable-folder/file results. Microsoft.Data.Sqlite's async methods execute synchronously, which explains why simply using `await` did not keep scanning responsive. [Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async).
7. **Plan a runtime and dependency update.** This app targets .NET 8, whose support ends on **10 November 2026**. Plan a tested move to .NET 10 LTS and review SQLite/NAudio dependencies and bundled runtime patches. Dependency versions were left unchanged in this repair to avoid combining a platform migration with behavioural fixes. [Microsoft support notice](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/).

## Build and test the reviewed source

Extract the reviewed ZIP into a new folder and open `HazzMusicBingo.sln` in Visual Studio. Build Release. The original standalone build script remains available.

From a terminal in the extracted source folder:

```powershell
dotnet build HazzMusicBingo.sln -c Release
dotnet run --project tests/HazzMusicBingo.Regression -c Release
```

Before an event, use a test game to check: working audio, missing audio, Repeat then Next, Stop then Next, reset, save/load, a cancelled load, a large scan while moving the window, the audience screen on each monitor arrangement, and a sample printed page. Keep a copy of existing game saves before using the revised app.
