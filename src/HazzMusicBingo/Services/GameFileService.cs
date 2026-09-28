using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using System.IO;
using System.Text.Json;

namespace HazzMusicBingo.Services;

public sealed class GameFileService
{
    private readonly AppDatabase _db;

    public GameFileService(AppDatabase db) => _db = db;

    public async Task SaveAsync(long gameId, string filePath)
    {
        var states = await _db.GetGameTrackStatesAsync(gameId);
        if (states.Count == 0)
            throw new InvalidOperationException("There is no game data to save.");

        var archive = new GameArchive
        {
            SessionCode = await _db.GetSessionCodeAsync(gameId),
            Winning = await _db.GetWinningSettingsAsync(gameId),
            SavedUtc = DateTime.UtcNow,
            Tracks = states.Select(s => new GameArchiveTrack
            {
                FilePath = s.Track.FilePath,
                Title = s.Track.Title,
                Artist = s.Track.Artist,
                DurationSeconds = s.Track.DurationSeconds,
                PlayOrder = s.PlayOrder,
                IsPlayed = s.IsPlayed,
                PlayedUtc = s.PlayedUtc
            }).ToList()
        };

        var cards = await _db.GetCardsAsync(gameId, 1000);
        archive.Cards = cards.Select(c => new GameArchiveCard
        {
            CardNumber = c.CardNumber,
            SquareFilePaths = c.Squares
                .Select(t => t.FilePath)
                .ToList()
        }).ToList();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await AtomicFile.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(archive, options));
    }

    public async Task<GameLoadResult> LoadAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var archive = JsonSerializer.Deserialize<GameArchive>(json)
            ?? throw new InvalidOperationException(
                "The selected game file could not be read.");

        GameArchiveValidator.Validate(archive);

        var states = new List<GameTrackState>();
        var trackByPath = new Dictionary<string, Track>(
            StringComparer.OrdinalIgnoreCase);

        var missing = 0;

        // IMPORTANT:
        // A saved game restores the exact 60-song pool and played/unplayed state,
        // but NEVER restores the old playback order. Every load gets a fresh
        // random playback order.
        var randomizedTracks = archive.Tracks
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        var newPlayOrder = 0;

        foreach (var saved in randomizedTracks)
        {
            if (string.IsNullOrWhiteSpace(saved.FilePath))
                throw new InvalidOperationException(
                    "The saved game contains a track with no file path.");

            if (!File.Exists(saved.FilePath))
                missing++;

            var ticks = File.Exists(saved.FilePath)
                ? File.GetLastWriteTimeUtc(saved.FilePath).Ticks
                : 0L;

            await _db.UpsertTrackAsync(
                saved.FilePath,
                string.IsNullOrWhiteSpace(saved.Title)
                    ? Path.GetFileNameWithoutExtension(saved.FilePath)
                    : saved.Title,
                saved.Artist ?? "",
                saved.DurationSeconds,
                ticks);

            var track = await _db.GetTrackByFilePathAsync(saved.FilePath)
                ?? throw new InvalidOperationException(
                    $"Could not restore track: {saved.FilePath}");

            trackByPath[saved.FilePath] = track;

            states.Add(new GameTrackState
            {
                Track = track,
                PlayOrder = newPlayOrder++,
                IsPlayed = saved.IsPlayed,
                PlayedUtc = saved.PlayedUtc
            });
        }

        var cards = archive.Cards.OrderBy(c => c.CardNumber).Select(c => new BingoCard
        {
            CardNumber = c.CardNumber,
            Squares = c.SquareFilePaths.Select(path => trackByPath[path]).ToList()
        }).ToList();

        // Commit the game, progress and cards together; a failed insert keeps the old game active.
        var gameId = await _db.CreateGameWithStateAsync(states, cards, archive.SessionCode, archive.Winning);
        return new GameLoadResult
        {
            GameId = gameId,
            MissingFiles = missing,
            RestoredCards = cards.Count
        };
    }
}
