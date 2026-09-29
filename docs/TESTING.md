# Release testing

The release suite checks database behaviour and real WPF controls/rendering using isolated test profiles. It does not use the owner's music library or normal database.

## Automated coverage

- 20 generated 60-card sets: track uniqueness, grid-position uniqueness, unique song sets and stable reprinting.
- Exact card-layout save/load round trips, progress preservation and reset behaviour.
- Malformed archives, missing files, transaction rollback after forced card-insertion failure, and atomic save replacement.
- Playback-start callback not invoked for missing audio.
- All 10 horizontal/vertical line wins and near misses; four corners, full house, empty progress and unrelated played IDs.
- Both diagonals rejected; inclusive sold-card filtering, multiple live winners, acknowledgement persistence, rule progression, legacy saves and Previous/Next navigation (including actual WPF button events).
- History reopening preserves order, played state and layouts; failed recovery preserves the active game.
- Database backup can reopen with cards and active state.
- Missing-file detection, availability filtering, relinking preserving identities, and rejection of duplicate target paths.
- Game code save/load and repeatable migration of a pre-code database.
- Font selection changes immediately, consecutive changes do not lag, all preview text uses the selection, and cancelled working settings remain isolated.
- Design preset round trip; 24 combinations of sheet density, orientation, paper and ink saver; correct pagination, page dimensions and card range.
- Print viewer pages load; all new windows can lay out; PNG render evidence is generated.

The console runner prints the assertion count and throws on any failure. Run it with the commands in [DEVELOPMENT.md](DEVELOPMENT.md). A GitHub Actions run adds a clean Windows-runner build and test record.

## Manual event-machine acceptance

1. Play a known good file through the event speakers. Test each clip length, Stop, Repeat, Repeat→Next and missing/unsupported audio. Confirm the played count follows actual startup.
2. Scan a large real library and an inaccessible folder. Check host responsiveness and the reported result. Disconnect a music drive, run Check music, reconnect/relink and retry.
3. Switch fonts repeatedly. Save/reload a preset, restart the app, and compare the print preview. Test long song names, artwork, large text and the ink-saver option.
4. Print a one-card sample and a four-card page on the actual printer. Test page ranges, copies, A4/Letter and both orientations. Create and inspect a PDF through Microsoft Print to PDF.
5. Compare a paper card with the winner grid for each winning rule and verify the displayed game code. Try a non-winner and an invalid card number.
6. Close a game, create another, reopen the first from history and compare cards/progress/order. Save, load and back up. Verify a backup in a separate profile.
7. Exercise the audience screen with one display, two displays and mixed scaling. Page the played list and return to the current song.

These hardware/interactive steps are not claimed as completed by the automated suite. Rendered PNGs verify layout without a physical print job. Availability tests do not continuously re-decode every library file. Tests cover no automatic monitor hot-plug recovery or midstream audio-device rollback because those features are not implemented.

## v.0.3 additions

The 357-check suite also covers eight-slot settings, owned save/artwork copies, resuming independent games, corrupt settings, folder boundaries/case/subfolders, insufficient selection preserving the active game, ID3v1/v2 Unicode metadata, multiple artists, missing-field fallback, read-only tag access and rescan identity preservation. UI tests cover banner style editing/preview/persistence, branded icon loading and eight buttons at minimum window size. Packaged self-test additionally reads MP3 tags through the external DLL.
