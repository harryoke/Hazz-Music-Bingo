# Changelog

## v.0.1 — first public release

The public version name is intentionally reset from the supplied internal v0.2.15 source.

- Fixed immediate font selection and unified card-designer/print rendering.
- Added four themes, custom footer, row shading, line width, padding and text alignment.
- Added card-range selection, 1/2/4 cards per sheet, A4/Letter, orientation, margins, ink saver and full print preview.
- Added printer page-range support, hard-margin fitting and Microsoft Print to PDF workflow.
- Added persistent game codes to cards, saved games and the host status.
- Added library/current-game missing-file checks, safe song relinking and unavailable-song exclusion during game generation.
- Added a card-number winner checker for rows/columns/diagonals, four corners and full house.
- Added game history, exact-state reopening and online database backups.
- Retained playback-start accounting, background scans, strict archive validation and transactional game/card import fixes.
- Updated Microsoft.Data.Sqlite and SQLitePCLRaw dependencies following an audit.
- Added regression/rendering checks, Windows CI, user/developer documentation and illustrated examples.

Hardware-dependent audio, physical printing and multi-monitor rehearsal remain required. See the testing guide for the validation boundary.
