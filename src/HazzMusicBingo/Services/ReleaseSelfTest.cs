using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using Microsoft.Data.Sqlite;
using NAudio.Wave;
using System.IO;
using System.Text.Json;

namespace HazzMusicBingo.Services;

internal static class ReleaseSelfTest
{
    public static async Task<int> RunAsync(string? reportPath)
    {
        var folder = Path.Combine(Path.GetTempPath(), "HazzMusicBingo-release-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        reportPath ??= Path.Combine(folder, "result.json");
        try
        {
            var db = new AppDatabase(Path.Combine(folder, "test.db"));
            await db.InitializeAsync();
            for (var i = 0; i < 60; i++) await db.UpsertTrackAsync(Path.Combine(folder, $"track{i}.wav"), $"Song {i}", "Test", 1, 0);
            var game = await db.CreateGameAsync(await db.GetRandomTracksAsync(60));
            await new CardGenerator(db).EnsureStrictCardsExistAsync(game);
            var cards = await db.GetCardsAsync(game, 60);
            var document = PrintService.BuildDocument(cards, new CardDesignSettings(), new PrintOptions { LastCard = 60, CardsPerPage = 4 }, await db.GetSessionCodeAsync(game));
            if (document.Pages.Count != 15 || document.DocumentPaginator.GetPage(0).Visual is null)
                throw new InvalidOperationException("Print rendering failed.");
            var path = Path.Combine(folder, "game.hmbgame");
            var archives = new GameFileService(db);
            await archives.SaveAsync(game, path);
            var restored = await archives.LoadAsync(path);
            if (restored.RestoredCards != 60) throw new InvalidOperationException("Archive round trip failed.");
            var wave = Path.Combine(folder, "silence.wav");
            using (var writer = new WaveFileWriter(wave, new WaveFormat(8000, 16, 1))) writer.Write(new byte[16000], 0, 16000);
            using (var reader = new MediaFoundationReader(wave))
                if (reader.TotalTime.TotalMilliseconds < 900) throw new InvalidOperationException("Audio decoder failed.");
            using var cn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = db.DatabasePath }.ToString());
            cn.Open(); using var cmd = cn.CreateCommand(); cmd.CommandText = "SELECT sqlite_version();";
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new
            {
                result = "PASS", version = "0.2.0", sqliteVersion = (string?)cmd.ExecuteScalar(),
                runtimeVersion = Environment.Version.ToString(), cards = cards.Count, sheets = document.Pages.Count,
                checks = "SQLite native loading, card generation, WPF page rendering, archive round trip, Media Foundation WAV decoding"
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex)
        {
            try { await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { result = "FAIL", error = ex.ToString() })); }
            catch { }
            return 1;
        }
    }
}
