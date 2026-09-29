# Changelog

## Unreleased — repeat playback handover

- Fixed a silent next song when Play Next Song interrupts Repeat Last. Fades and stop cleanup now adjust only the clip's samples, leaving shared audio-device volume untouched.
- Added regression coverage for immediate handover, interruption during fade, sample amplitude, start notification, Stop and natural completion. Windows Release build passed with zero warnings/errors; all 366 regression checks passed. Physical speaker verification remains required.

## v.0.1 — first public release

The public version name is intentionally reset from the supplied internal v0.2.15 source.

- Fixed immediate font selection and unified card-designer/print rendering.
- Added four themes, custom footer, row shading, line width, padding and text alignment.
- Added card-range selection, 1/2/4 cards per sheet, A4/Letter, orientation, margins, ink saver and full print preview.
- Added printer page-range support, hard-margin fitting and Microsoft Print to PDF workflow.
- Added persistent game codes to cards, saved games and the host status.
- Added library/current-game missing-file checks, safe song relinking and unavailable-song exclusion during game generation.
- Added a card-number winner checker for horizontal or vertical lines, four corners and full house.
- Added game history, exact-state reopening and online database backups.
- Retained playback-start accounting, background scans, strict archive validation and transactional game/card import fixes.
- Updated Microsoft.Data.Sqlite and SQLitePCLRaw dependencies following an audit.
- Added regression/rendering checks, Windows CI, user/developer documentation and illustrated examples.

Hardware-dependent audio, physical printing and multi-monitor rehearsal remain required. See the testing guide for the validation boundary.

## v.0.2 — live winner tracking

- Added sold-card ranges, persistent rule selection and automatic host/audience winner messages.
- Added winner acknowledgement and Line → Four Corners → Full House progression with played progress retained.
- Added Previous/Next saved-card navigation; Line now uses horizontal and vertical lines only.
- Added an additive GameWinningSettings table and optional version-1 archive Winning object; old saves remain supported.

## v.0.3 — themed games, folder selection and MP3 tags

- Added the supplied Hazz Music Bingo logo to the host header and documentation, plus a multi-size Windows executable icon.
- Added eight configurable game buttons with custom labels, colours and audience-theme snapshots.
- Assignments own copies of game saves and artwork; switching resumes local progress and applies the assigned theme.
- Added button persistence, validation and regression coverage without changing existing saved-game schemas.
- Added independent audience winning-message font, size, colour, bold and italic controls with live preview and theme persistence.
- Added folder-only game generation with optional subfolders. A successful scan selects that folder for new games; All music restores whole-library selection. Insufficient selections never pull in songs from elsewhere.
- Added read-only MP3 title/artist tags (ID3v1 and ID3v2), per-field filename fallback, Unicode/multiple-artist support and rescan regression checks.
