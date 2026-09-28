using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.Text.Json;
using System.IO;

var folder = Path.Combine(Path.GetTempPath(), "HazzMusicBingo-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(folder);
Environment.SetEnvironmentVariable("HAZZ_MUSIC_BINGO_DATA_DIR", Path.Combine(folder, "profile"));
var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
var db = new AppDatabase(Path.Combine(folder, "test.db"));
await db.InitializeAsync();
for (var i = 0; i < 60; i++)
    await db.UpsertTrackAsync(Path.Combine(folder, $"song{i}.wav"), $"Song {i}", "Artist", 30, 0);
var games = new GameService(db);
var generator = new CardGenerator(db);
var files = new GameFileService(db);
var save = Path.Combine(folder, "test.hmbgame");
long game = 0;
for (var iteration = 0; iteration < 20; iteration++)
{
    game = await db.CreateGameAsync(await db.GetRandomTracksAsync(60));
    await generator.EnsureStrictCardsExistAsync(game);
    var cards = await db.GetCardsAsync(game, 60);
    Check(cards.Count == 60, "Card count");
    Check(cards.All(c => c.Squares.Select(t => t.Id).Distinct().Count() == 25), "Unique songs per card");
    Check(Enumerable.Range(0, 25).All(p => cards.Select(c => c.Squares[p].Id).Distinct().Count() == 60), "Unique grid positions");
    Check(cards.Select(c => string.Join(",", c.Squares.Select(t => t.Id).Order())).Distinct().Count() == 60, "Unique song sets");
    await generator.EnsureStrictCardsExistAsync(game);
    Check(await db.GetCardCountAsync(game) == 60, "Reprinting must not regenerate");
}
var next = await games.GetNextTrackAsync(game) ?? throw new Exception("Missing track");
await games.MarkPlayedAsync(game, next);
await files.SaveAsync(game, save);
var original = await File.ReadAllTextAsync(save);
var loaded = await files.LoadAsync(save);
Check(loaded.RestoredCards == 60 && loaded.MissingFiles == 60, "Load counts");
Check(await db.GetPlayedCountAsync(loaded.GameId) == 1, "Progress restored");
await files.SaveAsync(loaded.GameId, save);
var roundtrip = JsonSerializer.Deserialize<GameArchive>(await File.ReadAllTextAsync(save))!;
var archive = JsonSerializer.Deserialize<GameArchive>(original)!;
Check(JsonSerializer.Serialize(archive.Cards) == JsonSerializer.Serialize(roundtrip.Cards), "Exact card layout restored");
await games.ResetGameProgressAsync(loaded.GameId);
Check(await db.GetPlayedCountAsync(loaded.GameId) == 0 && await db.GetCardCountAsync(loaded.GameId) == 60, "Reset preserves cards");

var mutations = new Action<GameArchive>[]
{
    a => a.FormatVersion = 99,
    a => a.Tracks = null!,
    a => a.Tracks[0] = null!,
    a => a.Tracks[0].FilePath = a.Tracks[1].FilePath.ToUpperInvariant(),
    a => a.Tracks[0].FilePath = "relative.wav",
    a => a.Tracks[0].DurationSeconds = -1,
    a => a.Cards = null!,
    a => a.Cards.RemoveAt(0),
    a => a.Cards[0] = null!,
    a => a.Cards[0].CardNumber = a.Cards[1].CardNumber,
    a => a.Cards[0].SquareFilePaths = null!,
    a => a.Cards[0].SquareFilePaths[0] = "unknown.wav",
    a => a.Cards[0].SquareFilePaths[0] = a.Cards[0].SquareFilePaths[1],
    a => a.Cards[0].SquareFilePaths[0] = a.Cards[1].SquareFilePaths[0]
};
foreach (var mutate in mutations)
{
    var invalid = JsonSerializer.Deserialize<GameArchive>(original)!;
    mutate(invalid);
    await File.WriteAllTextAsync(save, JsonSerializer.Serialize(invalid));
    try { await files.LoadAsync(save); throw new Exception("Invalid archive accepted"); }
    catch (InvalidDataException) { checks++; }
    Check(await db.GetActiveGameIdAsync() == loaded.GameId, "Invalid load changed active game");
}
// Force a database error after inserting the new game to prove rollback includes cards.
var states = await db.GetGameTrackStatesAsync(loaded.GameId);
var duplicate = new BingoCard { CardNumber = 1, Squares = states.Take(25).Select(s => s.Track).ToList() };
try
{
    await db.CreateGameWithStateAsync(states, new[] { duplicate, duplicate });
    throw new Exception("Expected card insert failure");
}
catch (Microsoft.Data.Sqlite.SqliteException) { checks++; }
Check(await db.GetActiveGameIdAsync() == loaded.GameId, "Failed transaction replaced active game");

archive.Cards.Clear();
await File.WriteAllTextAsync(save, JsonSerializer.Serialize(archive));
var withoutCards = await files.LoadAsync(save);
await generator.EnsureStrictCardsExistAsync(withoutCards.GameId);
Check(await db.GetCardCountAsync(withoutCards.GameId) == 60, "No-card archive can generate cards");
await AtomicFile.WriteAllTextAsync(save, "old");
await AtomicFile.WriteAllTextAsync(save, "new");
Check(await File.ReadAllTextAsync(save) == "new", "Atomic overwrite");
Check(!Directory.EnumerateFiles(folder, "*.tmp").Any(), "Temporary save cleanup");
using (var player = new AudioClipPlayer())
{
    var started = false;
    try
    {
        await player.PlayAsync(Path.Combine(folder, "missing.wav"), TimeSpan.Zero, TimeSpan.FromSeconds(1),
            onStarted: () => { started = true; return Task.CompletedTask; });
        throw new Exception("Missing audio unexpectedly played");
    }
    catch (Exception ex) when (ex.Message != "Missing audio unexpectedly played") { checks++; }
    Check(!started && !player.IsPlaying, "Failed audio must not notify started");
}
checks += await FeatureTests.Run(folder);
checks += await LiveWinnerTests.Run(folder);
checks += await UiTests.Run(args.FirstOrDefault() ?? Path.Combine(folder, "screenshots"));
Console.WriteLine($"PASS: {checks} checks; 20 generated card sets; isolated database at {folder}");
