using HazzMusicBingo.Data;
using HazzMusicBingo.Models;

namespace HazzMusicBingo.Services;

public sealed class GameService
{
    private readonly AppDatabase _db;

    public GameService(AppDatabase db) => _db = db;

    public async Task<long> GenerateGameAsync(int poolSize = 60)
    {
        var total = await _db.GetTrackCountAsync();
        if (total < poolSize)
            throw new InvalidOperationException(
                $"At least {poolSize} indexed songs are required. Currently indexed: {total}.");

        var tracks = await _db.GetRandomTracksAsync(poolSize);
        if (tracks.Count != poolSize)
            throw new InvalidOperationException("The requested game pool could not be created.");

        return await _db.CreateGameAsync(tracks);
    }

    public Task<Track?> GetNextTrackAsync(long gameId) =>
        _db.GetNextUnplayedTrackAsync(gameId);

    public Task MarkPlayedAsync(long gameId, Track track) =>
        _db.MarkTrackPlayedAsync(gameId, track.Id);

    public Task ResetGameProgressAsync(long gameId) =>
        _db.ResetGameProgressAsync(gameId);

    public Task<List<Track>> GetPlayedAlphabeticalAsync(long gameId) =>
        _db.GetPlayedTracksAlphabeticalAsync(gameId);
}
