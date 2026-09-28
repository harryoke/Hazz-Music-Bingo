# Hazz Music Bingo v0.2.15

Initial Windows desktop build for Visual Studio 2026 / .NET 8.

## Included in this build

- Scan a selected HDD/SSD music folder and all subfolders.
- Index MP3, WAV, WMA, M4A, AAC, FLAC and OGG file paths.
- SQLite library stored under:
  `%LOCALAPPDATA%\HazzMusicBingo\hazzmusicbingo.db`
- Generate a locked random 60-song game pool.
- Random game playback order.
- Play Next Song.
- Repeat Last Song without adding it twice to the played list.
- Configurable 20 / 25 / 30 second clips.
- Separate audience window sent to display 2 when available.
- Current song shown on the audience display.
- Check Played Songs on both host and audience displays.
- Played songs sorted alphabetically.
- 5x5 cards, 25 songs per card.
- Cards use ONLY the locked 60-song game pool.
- Strict card rule for up to 60 cards:
  the same song is never used in the same grid position on two different cards.
- All 60 card layouts are saved the first time cards are generated, so reprinting
  does not silently generate replacements.
- One full-size bingo card per portrait A4 page.
- Card numbering.
- Current active game is restored after restarting the app.

## Build

1. Open `HazzMusicBingo.sln` in Visual Studio 2026.
2. Allow NuGet restore.
3. Build the solution.
4. Run the `HazzMusicBingo` project.

NuGet packages used:

- Microsoft.Data.Sqlite 8.0.10
- NAudio 2.2.1

## Self-contained publish

From a Visual Studio Developer Command Prompt in the project folder:

```bat
dotnet publish src\HazzMusicBingo\HazzMusicBingo.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Published files will normally appear under:

`src\HazzMusicBingo\bin\Release\net8.0-windows\win-x64\publish\`

## Music naming in v0.1.0

This first build reads Artist/Title from the filename.

Preferred format:

`Artist - Title.mp3`

Example:

`a-ha - Take On Me.mp3`

Files without `Artist - Title` are still indexed; the whole filename becomes the title.

A later build can add embedded ID3/FLAC tag reading and a cue-point editor.

## Audio note

Playback uses Windows Media Foundation through NAudio. Most standard Windows audio
formats work immediately. A file can still be indexed even if the local Windows
codec stack cannot decode it; if that happens the app displays a playback error
for that file rather than crashing.

## Strict 60-card rule

With a 60-song pool there are only 60 possible different songs for any single
grid position. Therefore the absolute maximum number of cards that can satisfy
"no two cards have the same song in the same position" is 60.

This build deliberately caps strict cards at 60.


## VS2026 project-load fix in v0.1.1

The original v0.1.0 solution accidentally used the legacy WPF project-type GUID in the `.sln`.
Visual Studio could therefore display the project as **(unloaded)**.

v0.1.1 uses the modern SDK-style C# project GUID.

If Visual Studio still shows the project as unloaded:

1. Close Visual Studio.
2. Run `OPEN_PROJECT_DIRECTLY.bat`, or open:
   `src\HazzMusicBingo\HazzMusicBingo.csproj`
3. Visual Studio should load the SDK-style project directly.
4. If Visual Studio reports that a workload is missing, install the
   **.NET desktop development** workload from Visual Studio Installer.



## v0.1.2 compile fix

Fixed WPF/Windows Forms type-name clashes introduced by using both frameworks in the
same project:

- `Application` is explicitly `System.Windows.Application`
- `MessageBox` is explicitly the WPF `System.Windows.MessageBox`
- `PrintDialog` is explicitly the WPF `System.Windows.Controls.PrintDialog`

This fixes CS0104 on `Application` and prevents the same ambiguity from appearing
next for the other shared type names.


## v0.1.3 compile fixes

Fixed every compiler error reported from v0.1.2:

- Added explicit `System.IO` imports for `Path`, `Directory`, `File` and `SearchOption`.
- Explicitly bound printing `Size` to `System.Windows.Size`.
- Explicitly bound `Brushes` to `System.Windows.Media.Brushes`.
- Explicitly bound horizontal and vertical alignment values to the WPF types.

These conflicts occur because the project deliberately uses WPF for the UI while
also using Windows Forms only for a small amount of Windows integration such as
the folder picker and monitor enumeration.


## v0.1.4 test-feedback fixes

- Printing changed to two PORTRAIT bingo cards side-by-side on one LANDSCAPE A4 sheet.
- The cut line is now vertical down the centre of the landscape page.
- Playing the next song no longer opens a full-screen audience window on the primary display when no second display exists.
- If display 2 exists, the audience output still opens full-screen on display 2.
- If no second display exists, pressing **Show Audience Screen** opens a normal floating/resizable audience preview on the primary display.
- Played Songs respects the same audience-window behaviour.


## v0.1.5 printing and card designer

Printing has been redesigned from the previous two-card landscape layout.

- One bingo card per A4 sheet.
- A4 prints in portrait orientation.
- The 5×5 card expands to use the full printable area of the sheet.
- Added **CARD DESIGN / PRINT SETTINGS** to the host screen.
- User-selectable card title.
- User-selectable header/subtitle.
- User-selectable installed Windows font.
- Adjustable title, header and song font sizes.
- Song text colour.
- Title/BINGO-letter colour.
- Page background colour.
- Square/cell background colour and opacity.
- Grid line colour.
- Optional JPG/PNG/BMP full-page background artwork.
- Adjustable background-image opacity.
- Show/hide B I N G O letters.
- Show title only or title + artist.
- Normal/bold song text.
- Live card preview.
- Card-design settings are saved under the user's local Hazz Music Bingo data
  folder and restored next time the program is opened.

Note: "full A4" means the full **printable area** reported by the selected printer.
Most physical printers have a small hardware non-printable edge unless the printer
supports borderless A4 printing.


## v0.1.6 compile fixes

Fixed the two compiler errors reported from v0.1.5:

- `Button` in the card designer is explicitly the WPF `System.Windows.Controls.Button`.
- `FontFamily` in the print service is explicitly the WPF `System.Windows.Media.FontFamily`.



## v0.1.7 compile fixes

Fixed all compiler errors reported from v0.1.6 by explicitly binding remaining
shared WPF/Windows Forms/System.Drawing type names:

- `Image` -> `System.Windows.Controls.Image`
- `Color` -> `System.Windows.Media.Color`
- `ColorConverter` -> `System.Windows.Media.ColorConverter`
- `FontFamily` -> `System.Windows.Media.FontFamily`
- `HorizontalAlignment` / `VerticalAlignment` -> WPF types
- `OpenFileDialog` -> `Microsoft.Win32.OpenFileDialog`



## v0.2.0 - print fix, stop control, audience designer, presets and saved games

### Printing
- Fixed background artwork being cropped on the right.
- The print engine now normalises the logical page to portrait dimensions.
- Background images use **Fit / Uniform** rather than crop-to-fill, so the whole
  supplied image remains visible.
- One full bingo card per portrait A4 printable area remains the default.

### Playback
- Added a large dedicated **STOP** button.
- Stop immediately cancels the current 20/25/30 second clip.
- Manually stopping a clip no longer changes the song's played status.

### Audience screen design
- Added a dedicated **Audience Screen Design** editor.
- Custom "Now Playing" header.
- Custom ready text.
- Custom "Played Songs" header.
- Installed Windows font selection.
- Independent header, title, artist and played-list sizes.
- Header, title, artist, played-list and background colours.
- Optional audience background JPG/PNG/BMP.
- Background opacity.
- Show/hide artist.
- Bold/normal song title.
- Live preview.
- Changes apply immediately to an already-open audience window.

### Load / save designs
- Bingo card design presets can be saved as `.hmbdesign` and loaded later.
- Audience screen design presets can be saved as `.hmbaudience` and loaded later.
- The currently selected card/audience designs are also remembered automatically.

### Saved games
- Added **Save Game** and **Load Game**.
- Portable `.hmbgame` files save the exact 60-song pool.
- Playback order is NOT preserved.
- Played/unplayed progress is preserved.
- Generated bingo cards are preserved when they already exist.
- Loading a saved game recreates it as the active game.
- Audio itself is not copied into a `.hmbgame`; the source music file paths still
  need to exist on the computer when the game is loaded.

### GUI
- Reworked the main screen into separate Music Library / Host Playback /
  Design & Output areas.
- Larger Play Next and Stop controls.
- Clearer game status, library status and playback status.


## v0.2.1 - saved game playback order rule

Saved games now follow this rule:

- The exact 60-song pool is restored.
- Played/unplayed progress is restored.
- Generated bingo cards are restored.
- **The previous playback order is never restored.**
- Every time a `.hmbgame` file is loaded, all 60 songs receive a fresh random
  playback order.
- Tracks already marked as played stay marked as played, so playback continues
  from the remaining songs in the newly randomized order.
- Loading the same saved game twice can therefore produce a different playback
  order each time.


## v0.2.2 compile fix

Fixed CS0246 in `CardDesignWindow.xaml.cs` by adding the missing
`using HazzMusicBingo.Services;` namespace import required for
`CardDesignSettingsService`.



## v0.2.3 - Played Songs audience display fix

- Pressing **CHECK PLAYED SONGS** now always opens/uses the audience output.
- With a second display connected, the alphabetical played-song list appears
  full-screen on display 2.
- With only one display, it appears in the floating audience preview window.
- Reworked the audience played-song layout into a clear three-column grid.
- Played-song font, size and colour now inherit directly from the Audience Screen
  Design settings.
- Repeated presses force-refresh the played-song list.
- The audience screen returns to the current-song display after the host closes
  the played-songs window.


## v0.2.4 - Played Songs crash fix

The audience Played Songs display was rewritten to remove the fragile XAML
ItemsControl/DataTemplate introduced in v0.2.3.

- Removed the malformed translucent colour value from the v0.2.3 template.
- Removed the ancestor bindings used by the played-song DataTemplate.
- Played songs are now rendered directly into a simple three-column WPF Grid.
- Audience font, font size and colour are still taken from Audience Screen Design.
- Empty played-song lists show a friendly "No songs have been played yet" message.
- Added defensive error handling so a display problem shows an error dialog instead
  of closing the whole host application.


## v0.2.5 compile fix

Fixed the four compiler errors in `MainWindow.xaml.cs` around lines 413-415.
A newline had accidentally been written directly inside the Played Songs error-message
string. It now correctly uses the escaped C# sequence `\n\n`.



## v0.2.6 compile fixes

Fixed all four compiler errors reported from v0.2.5 in `AudienceWindow.xaml.cs`:

- `Color.FromArgb(...)` now explicitly uses the WPF colour type.
- `HorizontalAlignment.Center` now explicitly uses the WPF alignment type.
- Replaced invalid two-argument `Thickness(6, 3)` with `Thickness(6, 3, 6, 3)`.
- Replaced invalid two-argument `Thickness(8, 6)` with `Thickness(8, 6, 8, 6)`.



## v0.2.7 - text stroke / outline support

Added reusable outlined text rendering to the bingo-card and audience outputs.

### Bingo card design
- New **Use text outline / stroke** option.
- User-selectable outline colour.
- User-selectable outline width from 0 to 12 pixels.
- Outline applies to:
  - card title
  - custom header/subtitle
  - B I N G O letters
  - card number
  - every song/artist square
- Live card designer preview now renders the outline too.
- Saved `.hmbdesign` presets preserve the outline settings.

### Audience screen design
- New **Use text outline / stroke** option.
- User-selectable outline colour.
- User-selectable outline width from 0 to 12 pixels.
- Outline applies to:
  - Now Playing header
  - song title
  - artist
  - Played Songs header
  - every entry in the Played Songs list
  - the no-songs-played message
- Live audience designer preview renders the outline.
- Saved `.hmbaudience` presets preserve the outline settings.

The feature uses a custom WPF vector-text control so the outline is rendered as a
real stroke around the glyph shapes rather than a drop shadow.


## v0.2.8 - compile repair for outlined text build

- Fixed the large cascade of syntax errors in `CardDesignWindow.xaml.cs`.
- The sample card preview strings had accidentally been generated with physical
  line breaks inside ordinary C# quoted strings.
- They now use legal escaped `\n` line breaks.
- Replaced the preview em-dash separators with ASCII hyphens to keep the sample
  source simple and compiler-safe.
- Hardened the custom `OutlinedTextBlock` by explicitly binding common ambiguous
  types such as `FontFamily`, `Brush`, `Brushes`, `Pen`, `Point` and `Size` to
  their WPF implementations.


## v0.2.9 compile fix

Fixed CS0176 in `OutlinedTextBlock.cs` caused by the class's inherited/property
names shadowing WPF enum/type names.

The control now explicitly aliases and uses:

- `System.Windows.FlowDirection`
- `System.Windows.TextAlignment`
- `System.Windows.TextWrapping`
- `System.Windows.FontWeight`

This fixes `FlowDirection.LeftToRight` and also prevents the same shadowing problem
from appearing next for `TextWrapping.NoWrap` or `TextAlignment.Left`.



## v0.2.10 - smooth fade-out and more clip lengths

### Playback fade
- Normal song clips no longer stop abruptly.
- The final **2 seconds** of each clip now fade smoothly from full volume to silence.
- If a track ends before the selected clip duration, the fade is applied to the
  actual remaining track time where possible.
- Very short clips automatically use a shorter proportional fade.
- The dedicated **STOP** button remains immediate so the host can stop sound
  straight away when required.

### Playback duration choices
The host can now choose:

- 10 seconds
- 15 seconds
- 20 seconds
- 25 seconds
- 30 seconds
- 35 seconds
- 40 seconds
- 45 seconds
- 60 seconds

25 seconds remains the default.


## v0.2.11 - Repeat Last -> Next Song crash fix

Fixed a playback race introduced by overlapping asynchronous audio operations.

### What was happening
`AudioClipPlayer` used shared `_output` and `_reader` fields. If **Repeat Last**
was still active and the host pressed **Next Song**, the new song replaced those
shared fields. The cancelled Repeat task could then reach its `finally` block and
dispose the **new** song's audio output.

### Fix
- Every `PlayAsync` call now owns a private playback session.
- A cancelled/older task can only stop and dispose its own NAudio reader/output.
- Starting a new song safely cancels and stops the previous session.
- The old async continuation cannot clear or overwrite a newer session.
- Host playback uses an operation ID so an interrupted Repeat callback cannot
  overwrite the UI state for the new song.
- **Next Song intentionally remains available during Repeat Last**, so pressing
  Next while the repeat is still playing cleanly interrupts the repeat and starts
  the next bingo song.
- STOP also invalidates old async UI callbacks before stopping the audio.


## v0.2.12 - paged Played Songs audience display

Played Songs no longer tries to squeeze every played track onto one audience
screen.

- Played songs are sorted alphabetically by song title, then artist.
- Audience display shows **12 songs per page** in a fixed **3 × 4** grid.
- More played songs create additional pages instead of reducing the font size.
- Audience played-song font is clamped to a clear **26–42 px** range.
- Added a large **PAGE X OF Y** indicator on the audience output.
- The host Played Songs window now has **Previous Page** and **Next Page** buttons.
- Pressing those buttons immediately changes the page on the audience screen.
- Host window shows the current page, total songs played and the visible song
  number range.
- The audience screen returns to the current-song display when the host closes
  the Played Songs window.


## v0.2.13 - clearer Played Songs pages + Reset Game to 0

### Played Songs audience display
The previous 12-song 3×4 layout could still overlap when song titles and artists
wrapped onto several lines.

- Reduced the audience page to **6 songs**.
- New fixed layout is **2 columns × 3 rows**.
- Each song gets substantially more width and height.
- Played-song text is kept in a clear **28–40 px** range.
- Up to four wrapped lines are allowed per entry.
- More songs simply create additional pages.
- A full 60-song game therefore uses up to **10 pages**.
- Previous / Next on the host Played Songs window continues to control the
  audience page.

### Reset Game to 0
Added a **RESET GAME TO 0** button to the host Game section.

Resetting:
- keeps the exact same 60-song pool;
- keeps all generated bingo cards;
- clears every played/unplayed flag back to unplayed;
- clears the previous played timestamps;
- sets the played counter back to **0 / 60**;
- clears Repeat Last/current-song state;
- creates a **fresh random playback order** for those same 60 songs.

A confirmation prompt is shown before the reset is performed.


## v0.2.14 - clearer active-game status badge

The top-right badge previously showed values such as **Active game #8**.
That number was only the internal SQLite database record ID for the game; it was
not the number of songs played and was never intended to reset to zero.

The badge now shows useful game progress instead:

- `0 / 60 PLAYED` at the start of a game;
- updates after every played song;
- returns to `0 / 60 PLAYED` when **RESET GAME TO 0** is used;
- restored correctly when a saved game is loaded;
- shows `No active game` when there is no current game.

The internal database game ID is still retained behind the scenes, but is no
longer shown to the host.


## v0.2.15 - standalone EXE publishing

Added a ready-to-use standalone Windows publishing workflow.

- `BUILD_STANDALONE_EXE.bat` builds a Release, win-x64, self-contained single-file EXE.
- The script automatically looks for `dotnet.exe` in PATH and standard Program Files locations.
- Output is placed in the root `STANDALONE_EXE` folder.
- Added Visual Studio publish profile `StandaloneWin64`.
- Added `HOW_TO_BUILD_STANDALONE.txt`.
- .NET 8 runtime is bundled into the published application.
- Single-file compression is enabled.
- Native libraries are included for self-extraction.
- Trimming is deliberately disabled for WPF / SQLite / NAudio compatibility.
