using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.IO;
using System.Text.Json;

internal static class LiveWinnerTests
{
    public static async Task<int> Run(string folder)
    {
        var count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
        var db = new AppDatabase(Path.Combine(folder, "live.db"));
        await db.InitializeAsync();
        for (var i = 1; i <= 60; i++)
            await db.UpsertTrackAsync(Path.Combine(folder, $"live{i}.wav"), $"Song {i}", "Artist", 30, 0);
        var game = await db.CreateGameAsync(await db.GetRandomTracksAsync(60));
        Check((await db.GetWinningSettingsAsync(game)).FirstCard is null, "Old and new games default to tracking off");
        await new CardGenerator(db).EnsureStrictCardsExistAsync(game);
        var cards = await db.GetCardsAsync(game, 1000);
        var settings = new WinningSettings { FirstCard = 1, LastCard = 1 };
        foreach (var invalid in new[] {
            new WinningSettings { FirstCard = 0, LastCard = 1 },
            new WinningSettings { FirstCard = 2, LastCard = 1 },
            new WinningSettings { FirstCard = 1, LastCard = 61 },
            new WinningSettings { FirstCard = 1 },
            new WinningSettings { Pattern = (WinningPattern)99 },
            new WinningSettings { FirstCard = 1, LastCard = int.MaxValue } })
        {
            try { await db.SaveWinningSettingsAsync(game, invalid); throw new Exception("Invalid range accepted"); }
            catch (ArgumentException) { count++; }
        }
        Check(LiveWinnerService.Detect(cards, [], settings).Count == 0, "No winner before songs play");
        foreach (var track in cards[0].Squares.Take(5)) await db.MarkTrackPlayedAsync(game, track.Id);
        var played = (await db.GetPlayedTracksAlphabeticalAsync(game)).Select(t => t.Id).ToArray();
        Check(LiveWinnerService.Detect(cards, played, settings).SequenceEqual(new[] { 1 }), "Live row win after fifth song");
        var all = cards.SelectMany(c => c.Squares).Select(t => t.Id).Distinct().ToArray();
        settings.FirstCard = 2; settings.LastCard = 42;
        Check(LiveWinnerService.Detect(cards, all, settings).SequenceEqual(Enumerable.Range(2, 41)), "Only sold cards win, inclusive bounds and simultaneous winners");
        settings.FirstCard = 1; settings.LastCard = 1;
        var winners = LiveWinnerService.Detect(cards, played, settings);
        Check(LiveWinnerService.Unacknowledged(winners, settings).Count == 1, "New winner requires acknowledgement");
        settings.AcknowledgedWinners.Add(LiveWinnerService.Key(settings.Pattern, 1));
        Check(LiveWinnerService.Unacknowledged(winners, settings).Count == 0, "Repeated state does not alert again");
        await db.SaveWinningSettingsAsync(game, settings);
        var snapshot = JsonSerializer.Serialize(await db.GetGameTrackStatesAsync(game));
        foreach (var rule in new[] { WinningPattern.FourCorners, WinningPattern.FullHouse, WinningPattern.AnyLine })
        {
            settings.Pattern = rule;
            await db.SaveWinningSettingsAsync(game, settings);
            Check(JsonSerializer.Serialize(await db.GetGameTrackStatesAsync(game)) == snapshot, "Rule switches preserve progress and order");
            Check((await db.GetWinningSettingsAsync(game)).Pattern == rule, "Selected rule persists");
        }
        Check(LiveWinnerService.Unacknowledged(winners, await db.GetWinningSettingsAsync(game)).Count == 0, "Switching back preserves acknowledgement");
        settings.Pattern = WinningPattern.FullHouse;
        await db.SaveWinningSettingsAsync(game, settings);
        Check(LiveWinnerService.Detect(cards, played, settings).Count == 0, "Line progress is not a full house");
        foreach (var track in cards[0].Squares) await db.MarkTrackPlayedAsync(game, track.Id);
        Check(LiveWinnerService.Detect(cards, (await db.GetPlayedTracksAlphabeticalAsync(game)).Select(t => t.Id), settings).SequenceEqual(new[] { 1 }), "Continuing game reaches full house");
        var path = Path.Combine(folder, "live.hmbgame");
        var files = new GameFileService(db);
        await files.SaveAsync(game, path);
        var loaded = await files.LoadAsync(path);
        Check(JsonSerializer.Serialize(await db.GetWinningSettingsAsync(loaded.GameId)) == JsonSerializer.Serialize(settings), "Portable settings and acknowledgements round trip");
        await db.ReopenGameAsync(game);
        Check((await db.GetWinningSettingsAsync(game)).LastCard == 1, "History retains range");
        await db.ResetGameProgressAsync(game);
        Check((await db.GetWinningSettingsAsync(game)).AcknowledgedWinners.Count == 0, "Reset clears acknowledgements");
        Check((await db.GetWinningSettingsAsync(game)).Pattern == WinningPattern.FullHouse && await db.GetPlayedCountAsync(game) == 0, "Reset retains rule and clears progress");
        var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        json.AsObject().Remove("Winning");
        await File.WriteAllTextAsync(path, json.ToJsonString());
        var legacy = await files.LoadAsync(path);
        Check((await db.GetWinningSettingsAsync(legacy.GameId)).FirstCard is null && await db.GetPlayedCountAsync(legacy.GameId) == 25, "Legacy version-1 file retains progress with safe defaults");
        foreach (var pair in new[] { (1, true, (int?)3), (3, false, (int?)1), (8, true, (int?)null), (1, false, (int?)null) })
            Check(LiveWinnerService.Adjacent(new[] { 8, 1, 3 }, pair.Item1, pair.Item2) == pair.Item3, "Navigation follows saved numbers and stops at boundaries");
        Check(LiveWinnerService.AudienceMessage(WinningPattern.AnyLine, true) == "LINE WON!", "Audience winner wording");
        Check(LiveWinnerService.AudienceMessage(WinningPattern.FourCorners, false) == "WE ARE PLAYING FOR FOUR CORNERS", "Audience progression wording");
        return count;
    }
}
