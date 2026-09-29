![Hazz Music Bingo](images/hazz-music-bingo.png)

# Hazz Music Bingo v.0.2 — quick start

## Install

Download **Hazz-Music-Bingo-v0.2-Windows-x64.zip** from the [v.0.2 release](https://github.com/harryoke/Hazz-Music-Bingo/releases/tag/v.0.2). Extract the entire ZIP into a folder, then run **HazzMusicBingo.exe**. Windows x64 is required; .NET is included. Music is not included.

Upgrading from v.0.1? Close the old app and extract this version into a new folder. The same Windows account automatically retains its library and games. Back up the database through **GAME HISTORY / RECOVERY** before your first event with the update. Old saves open with live tracking off until you apply a sold-card range.

## Prepare the event

1. **SCAN MUSIC FOLDER**: index at least 60 playable songs. Use filenames such as `Artist - Song Title.mp3`.
2. **CHECK MUSIC / LOCATE FILES**: resolve missing or unreadable music.
3. **GENERATE 60-SONG GAME**: lock the song pool for this event.
4. **BINGO CARD DESIGN**: choose the design, then **Use Design**.
5. **PREVIEW / PRINT CARDS**: this creates and saves all 60 layouts. Select the cards to print and test one page. Choose Microsoft Print to PDF for PDF cards.
6. Under **PLAYING FOR**, enter the first and last sold card numbers and press **Apply**. For example, 1–42 means 42 cards in play. The printed range and sold range are separate controls. Live tracking covers one contiguous range; do not include unsold numbers within it.
7. Select **LINE**, then **SAVE GAME**. Keep the game file with the printed cards.
8. Connect the audience display, select **SHOW AUDIENCE**, check your speaker output and choose a clip length.

## Run the game

| Stage | Winning pattern | What the host does |
| --- | --- | --- |
| Line | Five played songs in any horizontal or vertical line | Play clips until winning card numbers appear. Verify the claim and game code, then acknowledge. |
| Four Corners | All four corner songs played | Select **FOUR CORNERS** and continue the same game. Verify and acknowledge the winner(s). |
| Full House | All 25 songs played | Select **FULL HOUSE** and continue until a full house is reached. |

**PLAY NEXT SONG** marks a song when playback starts successfully. **REPEAT LAST** does not add progress. **STOP** does not undo a song already started.

The host panel automatically shows every winning card in the applied range. **ACKNOWLEDGE WINNER(S)** removes the attention highlight for those card/rule pairs; new winning cards still appear. The audience banner says “WE ARE PLAYING FOR…” and changes to “… WON!” without hiding the current song or played list. Acknowledging keeps the win banner until you select another rule.

Changing rule keeps all played songs. **Do not use RESET GAME TO 0 between prizes.** If the newly selected rule is already complete, its winners appear immediately.

## Check a claim

Open **CHECK WINNING CARD**, compare the printed game code, then enter the card number or use **Previous Card / Next Card**. Green squares are played songs. The checker opens on the host's selected rule, but can inspect any saved card, including an unsold one. Automatic detection is limited to the applied sold range.

## Finish and save

Save a `.hmbgame` file for a portable copy. **NEW / CLOSE GAME** keeps the session in local history. **GAME HISTORY / RECOVERY** reopens its exact progress and playback order. Loading a portable file keeps progress and layouts but deliberately reshuffles remaining playback order. Music files and artwork must be copied separately when moving computers.

## Before guests arrive

- Play a clip through the event speakers and check the audience display.
- Compare a printed sample with the saved card and game code.
- Confirm the applied sold range and selected rule.
- Save the prepared game and make a database backup.

For installation, music relinking, all design/printing options, recovery and troubleshooting, see the [full user guide](USER_GUIDE.md). For licence terms see [LICENSE.txt](../LICENSE.txt).

## Set up one-click themed games

1. Prepare and save each game as a `.hmbgame` file.
2. Click an empty numbered button in the header.
3. Choose the saved game, enter a label such as “1960s”, and choose a button colour.
4. Open **Audience theme…** to choose its font, colours and background artwork, then save the button.
5. Repeat for up to eight games. Click a button to switch; switching back resumes progress.

Right-click a button to edit it or store your current audience style. Changes to a button's theme do not reset songs. Use your own period artwork and installed fonts for decade games.
