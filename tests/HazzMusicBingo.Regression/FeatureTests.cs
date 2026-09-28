using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.IO;
using System.Text.Json;

internal static class FeatureTests
{
    public static async Task<int> Run(string folder)
    {
        var count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
        var db = new AppDatabase(Path.Combine(folder, "features.db"));
        await db.InitializeAsync();
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db.DatabasePath}"))
        {
            connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "SELECT sqlite_version();";
            Check(Version.Parse((string)command.ExecuteScalar()!) >= new Version(3, 50, 2), "Native SQLite includes the audited fix");
        }
        for (var i = 0; i < 60; i++)
        {
            var path = Path.Combine(folder, $"available-{i}.wav");
            await File.WriteAllBytesAsync(path, [0]); // Availability fixture, not used to decode audio.
            await db.UpsertTrackAsync(path, $"Song {i}", "Artist", 30, 0);
        }
        var service = new GameService(db);
        var game = await service.GenerateGameAsync();
        var code = await db.GetSessionCodeAsync(game);
        Check(code.Length == 12, "Session code");
        await new CardGenerator(db).EnsureStrictCardsExistAsync(game);
        var card = (await db.GetCardsAsync(game, 60))[0];
        var ids = card.Squares.Select(t => t.Id).ToArray();
        var patterns = Enumerable.Range(0, 5).Select(r => Enumerable.Range(0, 5).Select(c => r * 5 + c).ToArray())
            .Concat(Enumerable.Range(0, 5).Select(c => Enumerable.Range(0, 5).Select(r => r * 5 + c).ToArray()))
            .Concat(new[] { new[] { 0, 6, 12, 18, 24 }, new[] { 4, 8, 12, 16, 20 } });
        foreach (var pattern in patterns)
        {
            Check(WinnerService.Check(card, pattern.Select(i => ids[i]), WinningPattern.AnyLine).IsWinner, "Every winning line");
            Check(!WinnerService.Check(card, pattern.Take(4).Select(i => ids[i]), WinningPattern.AnyLine).IsWinner, "Near miss is not a line");
        }
        Check(!WinnerService.Check(card, [], WinningPattern.AnyLine).IsWinner, "Empty progress");
        Check(WinnerService.Check(card, new[] { ids[0], ids[4], ids[20], ids[24] }, WinningPattern.FourCorners).IsWinner, "Four corners");
        Check(!WinnerService.Check(card, ids.Take(24), WinningPattern.FullHouse).IsWinner, "Full house needs every song");
        Check(WinnerService.Check(card, ids, WinningPattern.FullHouse).IsWinner, "Full house");
        Check(WinnerService.Check(card, [long.MaxValue], WinningPattern.FullHouse).MatchedCount == 0, "Ignore unrelated songs");
        await db.MarkTrackPlayedAsync(game, ids[0]);
        var snapshot = JsonSerializer.Serialize(await db.GetGameTrackStatesAsync(game));
        var layouts = JsonSerializer.Serialize(await db.GetCardsAsync(game, 60));
        var second = await service.GenerateGameAsync();
        await db.ReopenGameAsync(game);
        Check(await db.GetActiveGameIdAsync() == game, "Recovery activates selected game");
        Check(JsonSerializer.Serialize(await db.GetGameTrackStatesAsync(game)) == snapshot, "Recovery preserves exact progress and order");
        Check(JsonSerializer.Serialize(await db.GetCardsAsync(game, 60)) == layouts, "Recovery preserves card IDs and layouts");
        Check((await db.GetGameHistoryAsync()).Single(g => g.Id == second).Status == "Closed", "Recovery closes previous game");
        try { await db.ReopenGameAsync(long.MaxValue); throw new Exception("Invalid recovery succeeded"); }
        catch (InvalidOperationException) { count++; }
        Check(await db.GetActiveGameIdAsync() == game, "Invalid recovery is non-destructive");
        var backup = Path.Combine(folder, "backup.db");
        await db.BackupAsync(backup);
        var backupDb = new AppDatabase(backup);
        await backupDb.InitializeAsync();
        Check(await backupDb.GetActiveGameIdAsync() == game && await backupDb.GetCardCountAsync(game) == 60, "Live SQLite backup contains cards and active state");
        var pool = await db.GetGamePoolAsync(game);
        Check(MusicHealthService.Check(pool).Count == 0, "Available pool");
        var missing = pool[0];
        File.Delete(missing.FilePath);
        Check(MusicHealthService.Check(pool).Single().Track.Id == missing.Id, "Missing music detection");
        try { await service.GenerateGameAsync(); throw new Exception("Insufficient available pool accepted"); }
        catch (InvalidOperationException) { count++; }
        Check(await db.GetActiveGameIdAsync() == game, "Failed generation preserves game");
        var replacement = Path.Combine(folder, "relocated.wav");
        await File.WriteAllBytesAsync(replacement, [0]);
        await db.RelinkTrackAsync(missing.Id, replacement, 30);
        Check(MusicHealthService.Check(await db.GetGamePoolAsync(game)).Count == 0, "Relink resolves issue");
        Check((await db.GetCardsAsync(game, 60)).SelectMany(c => c.Squares).Where(t => t.Id == missing.Id).All(t => t.FilePath == replacement), "Relink keeps all card identities");
        Check(await db.GetPlayedCountAsync(game) == 1, "Relink preserves played progress");
        try { await db.RelinkTrackAsync(missing.Id, pool[1].FilePath, 30); throw new Exception("Relink collision accepted"); }
        catch (InvalidOperationException) { count++; }
        var save = Path.Combine(folder, "features.hmbgame");
        await new GameFileService(db).SaveAsync(game, save);
        var restored = await new GameFileService(db).LoadAsync(save);
        Check(await db.GetSessionCodeAsync(restored.GameId) == code, "Portable file retains game code");
        // Verify migration is additive for pre-release databases.
        var legacyPath = Path.Combine(folder, "legacy.db");
        using (var cn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={legacyPath}"))
        {
            cn.Open(); using var cmd = cn.CreateCommand();
            cmd.CommandText = "CREATE TABLE Games(Id INTEGER PRIMARY KEY,CreatedUtc TEXT NOT NULL,Status TEXT NOT NULL,PoolSize INTEGER NOT NULL); INSERT INTO Games VALUES(1,'2026-01-01','Closed',60);";
            cmd.ExecuteNonQuery();
        }
        var legacy = new AppDatabase(legacyPath);
        await legacy.InitializeAsync(); var legacyCode = await legacy.GetSessionCodeAsync(1);
        await legacy.InitializeAsync();
        Check(legacyCode.Length == 12 && await legacy.GetSessionCodeAsync(1) == legacyCode, "Stable legacy migration");
        return count;
    }
}
