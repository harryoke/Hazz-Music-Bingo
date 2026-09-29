# Development and maintenance

## Requirements and commands

Windows x64 with a .NET 8 SDK or newer SDK capable of targeting .NET 8; Visual Studio's .NET desktop development workload is useful. The solution contains the WPF app and a console regression runner. WPF rendering and Media Foundation checks require Windows.

```powershell
dotnet restore HazzMusicBingo.sln
dotnet build HazzMusicBingo.sln -c Release --no-restore
dotnet run --project tests/HazzMusicBingo.Regression -c Release --no-build -- artifacts/screenshots
dotnet list src/HazzMusicBingo/HazzMusicBingo.csproj package --vulnerable --include-transitive
dotnet publish src/HazzMusicBingo/HazzMusicBingo.csproj -c Release -r win-x64 --self-contained true -p:DebugType=None -p:DebugSymbols=false -o artifacts/windows
```

The regression runner throws on a failed assertion and returns nonzero. It is deliberately a console runner, not a `dotnet test` project. It uses unique temporary databases and sets `HAZZ_MUSIC_BINGO_DATA_DIR` before constructing any settings service. Screenshot output is optional; by default it stays in the test directory. The print viewer is briefly loaded off-screen to exercise real WPF page realization; no print job is submitted. Test directories are retained for diagnosis.

The release is a self-contained Windows x64 package. The compressed executable includes the runtime; TagLibSharp.dll stays alongside it as a replaceable LGPL dependency. Native libraries are extracted at runtime. Trimming is disabled for WPF/SQLite/NAudio compatibility. The original `BUILD_STANDALONE_EXE.bat` publishes to `STANDALONE_EXE`; the Visual Studio `StandaloneWin64` profile is also available.

## Architecture

| Area | Responsibility |
| --- | --- |
| `Data/AppDatabase.cs` | Tracks, games, played state and card persistence; explicit foreign keys and parameterised SQL. |
| `Data/AppDatabase.Recovery.cs` | Additive game-code migration, history/reopen, file relinking and online SQLite backup. |
| `Services/GameService.cs` | Chooses 60 available tracks; coordinates simple game operations. |
| `Services/CardGenerator.cs` | Rotated permutation/offset construction of 60 × 25 layouts; rejects duplicate song sets. |
| `Services/PrintService.cs` | One card renderer for designer and print; sheet composition; printer-area fitting and page-range paginator. |
| `Services/WinnerService.cs` | Pure 5×5 pattern evaluation based on played track IDs. |
| `Services/MusicHealthService.cs` | File existence and saved decoding-status checks. |
| `Services/GameFileService.cs` | JSON export/import, pre-validation, atomic file replacement. |
| `Services/AudioClipPlayer.cs` | Per-playback resource ownership, cancellation, fade and successful-start callback. |
| `Views/` | WPF host, designer, print preview, health, history, winner and audience windows. |

The font defect arose because `SelectionChanged` could precede the update of `ComboBox.Text`. The designer now reads `SelectedItem`, and tests verify both consecutive selections and every rendered text element's font. The old separate preview rendering implementation was removed.

Print output uses a fixed logical 680×920 card scaled into the selected sheet slot. Song text wraps without a line-count cap and scales down to preserve complete labels. Headers/footers have a two-line cap. The printer paginator scales complete pages to `PageImageableArea` rather than assuming borderless output. Requested sheet ranges are applied after card-range selection.

## Data and compatibility

Default folder: `%LOCALAPPDATA%\HazzMusicBingo`. Files: `hazzmusicbingo.db`, `card-design.json`, `audience-design.json`, plus SQLite-managed WAL/SHM files when present. The data-directory environment override applies to the default database and both design services. Passing a path to `AppDatabase` isolates only that database.

`Tracks.Id` is the stable song identity. Relinking changes path/duration/timestamp, preserving card and played-state relationships. Reject a path already assigned to another track; merging IDs could introduce duplicate songs within cards or a pool. The library currently allows separate copies of the same recording, and metadata is shared across games. A future migration should snapshot printed labels if immutable historical text is needed.

`Games.SessionCode` is an additive text column. Existing games receive a persistent 12-hex-character code on initialization. New games generate a new code; imported archives retain their code. It is an identifier, not a unique database key or security signature. Importing the same archive twice creates two local game rows with the same code. The UI identifies rows by database ID, date and progress.

Archive `FormatVersion` remains 1. `SessionCode` is optional for older files. Tracks require fully qualified distinct file paths, nonnegative finite durations and exactly 60 entries. Cards must be absent or a complete 60-card set; each card has 25 pool songs, unique numbers, unique positions across cards and a unique song set. Import validates before metadata writes. Game activation, played state and cards are one transaction, but track upserts are not part of that transaction. Loading intentionally randomizes the order. History recovery changes only active/closed statuses and preserves order and cards.

Played state is recorded after output startup, not after the entire clip finishes. A user Stop still counts the song. Midstream device errors do not roll progress back. The startup callback must not become a long-running operation; future playback work should track elapsed clip time independently of UI/database work.

## Dependencies and support

Microsoft.Data.Sqlite 8.0.31, SQLitePCLRaw.bundle_e_sqlite3 2.1.13 and NAudio 2.2.1 are explicit references. SQLitePCLRaw was updated from the original transitive 2.1.6 after the dependency audit flagged [CVE-2025-6965](https://github.com/advisories/GHSA-2m69-gcr7-jv3q). The final dependency audit should be rerun for each release; a clean audit is not a guarantee against unknown vulnerabilities.

The app still targets .NET 8. Microsoft lists its end of support as [10 November 2026](https://devblogs.microsoft.com/dotnet/dotnet-8-9-end-of-support/). Plan a .NET 10 LTS migration and re-test Windows audio, printing, single-file extraction and existing databases. Rebuild self-contained releases to deliver runtime patches. Microsoft.Data.Sqlite's async methods [execute synchronously](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async); move substantial indexing work off the UI thread and use batching where appropriate.

## CI and releases

`.github/workflows/build.yml` builds on Windows, runs the regression/rendering suite, audits packages and publishes a Windows executable as a workflow artifact. The dependency audit is informational; reviewers must inspect its result. It does not upload a public release automatically.

Release checklist: update project/assembly version and VERSION.txt; update CHANGELOG and guides; run the build, regression runner and dependency audit; inspect screenshots; rehearse audio/printing/displays; publish self-contained x64 output; package docs and third-party notices; calculate SHA-256 hashes; create the release tag on the tested commit and attach the ZIPs. Do not commit library databases, game saves, user presets or media.

The requested first public release is named **Hazz Music Bingo v.0.1**, tag **v.0.1**, assembly version **0.1.0.0**. This intentionally replaces the earlier internal v0.2.15 label; it is not an automatic update-version ordering scheme.

## v.0.2 winning-state persistence

`GameWinningSettings` is an additive table keyed by game ID with a JSON Settings column. `WinningSettings` stores Pattern (existing numeric enum values 0/1/2), nullable FirstCard/LastCard and AcknowledgedWinners rule/card keys. Missing settings mean Line with automatic tracking disabled. `LiveWinnerService` validates all range members against saved card numbers, detects qualifying cards and supplies acknowledgement keys and navigation helpers.

Version-1 archives add an optional Winning object. Validation precedes mutation; winning settings are inserted in the same transaction as game state and cards. Reset clears acknowledgements transactionally with played state. Rule changes only update settings. Older apps ignore the optional archive object and will lose those settings if they resave it.

The current release is **v.0.3**, tag **v.0.3**, application version **0.3.0**. The original public release remains at tag **v.0.1**. The legacy release-v0.1 workflow must not be used to publish newer versions: create their own versioned tag and assets after validation. Normal main-branch CI does not publish releases.

## v.0.3 source selection, shortcuts and tags

GameService optionally filters indexed paths to a selected folder before availability checks. Matching is case-insensitive with a directory boundary; subfolders are optional. Insufficient pools never fall back to the whole library. A successful scan updates the host's selection; the selector resets to the whole library at startup. Saved game pools are unchanged.

GameShortcutService stores eight slots in game-shortcuts.json and owns save/artwork copies in game-buttons/. Each slot includes label, colour, audience settings and a resumed game ID/session code. Initial activation imports its snapshot; later activation reopens the local record and preserves progress. Include the JSON, owned files and database in full-profile backups.

AudienceDesignSettings adds optional winning-message font/colour and size/bold/italic fields. Missing fields preserve prior styling defaults; Clone includes the new values for per-button themes.

TrackMetadataReader uses unmodified TagLibSharp 2.3.0 for read-only MP3 title/performer metadata. Missing fields fall back independently to FilenameMetadata; other formats retain filename metadata. Scanning still validates duration with Windows Media Foundation. TagLibSharp.dll is excluded from the single-file bundle in KeepTagReaderExternal and copied alongside the executable. Always distribute it, its licence/notices and the corresponding source archive in licenses/.
