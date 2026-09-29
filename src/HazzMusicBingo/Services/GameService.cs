using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using System.IO;

namespace HazzMusicBingo.Services;

public sealed class GameService
{
    private readonly AppDatabase _db;

    public GameService(AppDatabase db) => _db = db;

    public async Task<long> GenerateGameAsync(int poolSize = 60, string? sourceFolder = null, bool includeSubfolders = true)
    {
        if (sourceFolder is not null && !Directory.Exists(sourceFolder))
            throw new DirectoryNotFoundException("The selected music folder is unavailable. Reconnect its drive or choose another folder.");
        var library = await _db.GetAllTracksAsync();
        if (sourceFolder is not null)
            library = library.Where(t => IsInFolder(t.FilePath, sourceFolder, includeSubfolders)).ToList();
        var available = await Task.Run(() => library.Where(MusicHealthService.IsAvailable).ToList());
        if (available.Count < poolSize)
            throw new InvalidOperationException(
                $"At least {poolSize} available, successfully scanned songs are required {(sourceFolder is null ? "in the library" : "in the selected folder" )}. Found {available.Count} of {library.Count} indexed songs in this selection. Scan the folder or use CHECK MUSIC to locate missing or unreadable files. No songs from other folders will be added to this selection.");

        var tracks = available.OrderBy(_ => Random.Shared.Next()).Take(poolSize).ToList();
        if (tracks.Count != poolSize)
            throw new InvalidOperationException("The requested game pool could not be created.");

        return await _db.CreateGameAsync(tracks);
    }

    public static bool IsInFolder(string filePath, string folder, bool includeSubfolders)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        if (string.Equals(directory, root, StringComparison.OrdinalIgnoreCase)) return true;
        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        return includeSubfolders && directory is not null && directory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
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
