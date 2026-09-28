using HazzMusicBingo.Models;
using System.IO;

namespace HazzMusicBingo.Services;

public static class GameArchiveValidator
{
    public static void Validate(GameArchive archive)
    {
        if (archive.FormatVersion != 1)
            throw new InvalidDataException("This saved-game format is not supported.");
        if (archive.Tracks is null || archive.Tracks.Count != 60)
            throw new InvalidDataException("A saved game must contain exactly 60 songs.");

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var track in archive.Tracks)
        {
            if (track is null || string.IsNullOrWhiteSpace(track.FilePath)
                || !Path.IsPathFullyQualified(track.FilePath)
                || !paths.Add(track.FilePath)
                || !double.IsFinite(track.DurationSeconds) || track.DurationSeconds < 0)
                throw new InvalidDataException("The saved game contains an invalid or duplicate song.");
        }

        if (archive.Cards is null || (archive.Cards.Count != 0 && archive.Cards.Count != 60))
            throw new InvalidDataException("A saved game must contain either no cards or all 60 cards. Partial sets cannot safely be regenerated.");

        var numbers = new HashSet<int>();
        var sets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var positions = Enumerable.Range(0, 25)
            .Select(_ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).ToArray();
        foreach (var card in archive.Cards)
        {
            if (card is null || card.CardNumber < 1 || card.CardNumber > 60
                || !numbers.Add(card.CardNumber) || card.SquareFilePaths is null
                || card.SquareFilePaths.Count != 25)
                throw new InvalidDataException("The saved game contains an invalid card.");

            var songs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < 25; i++)
            {
                var path = card.SquareFilePaths[i];
                if (path is null || !paths.Contains(path) || !songs.Add(path) || !positions[i].Add(path))
                    throw new InvalidDataException("The saved cards violate the unique-song or grid-position rules.");
            }
            // JSON preserves path boundaries when building the set identity.
            var key = System.Text.Json.JsonSerializer.Serialize(songs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
            if (!sets.Add(key))
                throw new InvalidDataException("Two saved cards contain the same set of songs.");
        }
    }
}
