using HazzMusicBingo.Data;
using HazzMusicBingo.Services;
using System.IO;

internal static class FolderSelectionTests
{
    public static async Task<int> Run(string folder)
    {
        var checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        var db = new AppDatabase(Path.Combine(folder, "folder-selection.db"));
        await db.InitializeAsync();
        var root = Path.Combine(folder, "70s");
        var nested = Path.Combine(root, "Disco");
        var sibling = Path.Combine(folder, "70s-other");
        foreach (var dir in new[] { root, nested, sibling })
        {
            Directory.CreateDirectory(dir);
            for (var i = 0; i < 60; i++)
            {
                var path = Path.Combine(dir, $"song-{i}.wav");
                await File.WriteAllBytesAsync(path, [0]);
                await db.UpsertTrackAsync(path, $"Song {i}", "Artist", 30, 0);
            }
        }
        var service = new GameService(db);
        var direct = await service.GenerateGameAsync(60, root, false);
        Check((await db.GetGamePoolAsync(direct)).All(t => Path.GetDirectoryName(t.FilePath) == root), "Only direct folder songs selected");
        var recursive = await service.GenerateGameAsync(60, root, true);
        Check((await db.GetGamePoolAsync(recursive)).All(t => GameService.IsInFolder(t.FilePath, root, true)), "Recursive game excludes other folders");
        Check(GameService.IsInFolder(Path.Combine(nested, "song.wav"), root.ToUpperInvariant() + Path.DirectorySeparatorChar, true), "Case and trailing separator supported");
        Check(!GameService.IsInFolder(Path.Combine(sibling, "song.wav"), root, true), "Similar folder prefix excluded");
        Check(!GameService.IsInFolder(Path.Combine(nested, "song.wav"), root, false), "Subfolder excluded when option off");
        Check(GameService.IsInFolder(Path.Combine(root, "song.wav"), Path.GetPathRoot(root)!, true), "Drive root selection works");
        File.Delete(Path.Combine(root, "song-0.wav"));
        try { await service.GenerateGameAsync(60, root, false); throw new Exception("Insufficient selection accepted"); }
        catch (InvalidOperationException) { checks++; }
        Check(await db.GetActiveGameIdAsync() == recursive, "Insufficient selection preserves current game");
        try { await service.GenerateGameAsync(60, Path.Combine(folder, "absent")); throw new Exception("Missing folder accepted"); }
        catch (DirectoryNotFoundException) { checks++; }
        Check(await db.GetActiveGameIdAsync() == recursive, "Missing folder preserves current game");
        var empty = Path.Combine(folder, "unscanned"); Directory.CreateDirectory(empty);
        try { await service.GenerateGameAsync(60, empty); throw new Exception("Unscanned folder accepted"); }
        catch (InvalidOperationException) { checks++; }
        var all = await service.GenerateGameAsync();
        Check((await db.GetGamePoolAsync(all)).Count == 60 && await db.GetTrackCountAsync() == 180, "All-library generation still works without deleting tracks");
        return checks;
    }
}
