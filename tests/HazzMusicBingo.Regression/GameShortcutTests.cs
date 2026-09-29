using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.IO;
using System.Text.Json;

internal static class GameShortcutTests
{
    public static async Task<int> Run(string folder)
    {
        var checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        var db = new AppDatabase(Path.Combine(folder, "shortcuts.db"));
        await db.InitializeAsync();
        for (var i = 0; i < 60; i++) await db.UpsertTrackAsync(Path.Combine(folder, $"slot-{i}.wav"), $"Song {i}", "Artist", 30, 0);
        var game = await db.CreateGameAsync(await db.GetRandomTracksAsync(60));
        await new CardGenerator(db).EnsureStrictCardsExistAsync(game);
        var save = Path.Combine(folder, "shortcut-source.hmbgame");
        await new GameFileService(db).SaveAsync(game, save);
        var artwork = Path.Combine(folder, "shortcut-artwork.png");
        await File.WriteAllBytesAsync(artwork, [1, 2, 3]); // Copy fixture, not rendered.
        var root = Path.Combine(folder, "shortcut-profile");
        var service = new GameShortcutService(root);
        var slots = service.Load();
        Check(slots.Count == 8 && slots.All(s => s is null), "Eight empty slots on first start");
        var theme = new AudienceDesignSettings { FontFamilyName = "Georgia", BackgroundColor = "#552211", BackgroundImagePath = artwork };
        slots[0] = await service.PrepareAsync(save, "1960s", "#FFAA00", theme);
        slots[1] = await service.PrepareAsync(save, "1970s", "#663399", new AudienceDesignSettings { FontFamilyName = "Arial", BackgroundColor = "#221144" });
        theme.FontFamilyName = "Changed";
        Check(slots[0]!.Audience.FontFamilyName == "Georgia", "Slot owns theme snapshot");
        await service.SaveAsync(slots);
        File.Delete(artwork); File.Delete(save);
        slots = new GameShortcutService(root).Load();
        Check(slots[0]!.Label == "1960s" && slots[1]!.Color == "#663399", "Labels and colours survive restart");
        Check(File.ReadAllBytes(slots[0]!.Audience.BackgroundImagePath).SequenceEqual(new byte[] { 1, 2, 3 }), "Owned artwork survives original removal");
        var first = await service.ActivateAsync(db, slots[0]!);
        var firstCards = await db.GetCardsAsync(first.GameId, 60);
        await db.MarkTrackPlayedAsync(first.GameId, firstCards[0].Squares[0].Id);
        await db.SaveWinningSettingsAsync(first.GameId, new WinningSettings { FirstCard = 1, LastCard = 42, Pattern = WinningPattern.FourCorners });
        var snapshot = JsonSerializer.Serialize(await db.GetGameTrackStatesAsync(first.GameId));
        var second = await service.ActivateAsync(db, slots[1]!);
        Check(second.GameId != first.GameId && await db.GetPlayedCountAsync(second.GameId) == 0, "Slots have independent games");
        await service.SaveAsync(slots);
        slots = new GameShortcutService(root).Load();
        var resumed = await service.ActivateAsync(db, slots[0]!);
        Check(resumed.GameId == first.GameId && JsonSerializer.Serialize(await db.GetGameTrackStatesAsync(first.GameId)) == snapshot, "Switch back preserves exact progress/order after restart");
        Check((await db.GetWinningSettingsAsync(resumed.GameId)).Pattern == WinningPattern.FourCorners, "Switch preserves rule and sold range");
        Check((await db.GetCardsAsync(resumed.GameId, 60))[0].Id == firstCards[0].Id, "Switch retains saved card identity");
        var historyCount = (await db.GetGameHistoryAsync()).Count;
        await service.ActivateAsync(db, slots[0]!);
        Check((await db.GetGameHistoryAsync()).Count == historyCount, "Repeated click does not import duplicate games");
        var active = await db.GetActiveGameIdAsync();
        var bad = Path.Combine(folder, "bad-shortcut.hmbgame"); await File.WriteAllTextAsync(bad, "{}");
        try { await service.PrepareAsync(bad, "Bad", "#000000", new()); throw new Exception("Invalid file accepted"); }
        catch (InvalidDataException) { checks++; }
        Check(await db.GetActiveGameIdAsync() == active, "Invalid assignment leaves active game untouched");
        var before = await File.ReadAllTextAsync(Path.Combine(root, "game-shortcuts.json"));
        try { await service.SaveAsync(new List<GameShortcut?>()); throw new Exception("Wrong slot count accepted"); }
        catch (ArgumentException) { checks++; }
        Check(await File.ReadAllTextAsync(Path.Combine(root, "game-shortcuts.json")) == before, "Invalid settings cannot overwrite slots");
        slots[0] = null; await service.SaveAsync(slots);
        Check(await db.GetPlayedCountAsync(first.GameId) == 1, "Clearing a button retains history and progress");
        await File.WriteAllTextAsync(Path.Combine(root, "game-shortcuts.json"), "invalid");
        try { service.Load(); throw new Exception("Corrupt settings silently ignored"); }
        catch (JsonException) { checks++; }
        return checks;
    }
}
