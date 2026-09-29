using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HazzMusicBingo.Services;

public sealed class GameShortcutService
{
    private readonly string _folder;
    private string SettingsPath => Path.Combine(_folder, "game-shortcuts.json");
    public GameShortcutService(string? folder = null) => _folder = folder ?? AppStorage.Folder;

    public List<GameShortcut?> Load()
    {
        if (!File.Exists(SettingsPath)) return Enumerable.Repeat<GameShortcut?>(null, 8).ToList();
        var slots = JsonSerializer.Deserialize<List<GameShortcut?>>(File.ReadAllText(SettingsPath))
            ?? throw new InvalidDataException("The game button settings could not be read.");
        if (slots.Count != 8) throw new InvalidDataException("The game button settings must contain eight slots.");
        foreach (var slot in slots.Where(s => s is not null)) Validate(slot!);
        return slots;
    }

    public async Task SaveAsync(IReadOnlyList<GameShortcut?> slots)
    {
        if (slots.Count != 8) throw new ArgumentException("Exactly eight game buttons are required.");
        foreach (var slot in slots.Where(s => s is not null)) Validate(slot!);
        Directory.CreateDirectory(_folder);
        await AtomicFile.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(slots, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Validate(GameShortcut slot)
    {
        if (string.IsNullOrWhiteSpace(slot.Label) || slot.Label.Length > 32
            || !Regex.IsMatch(slot.Color ?? "", "^#[0-9a-fA-F]{6}$") || slot.Audience is null
            || string.IsNullOrWhiteSpace(slot.GameFile) || !Path.IsPathFullyQualified(slot.GameFile))
            throw new InvalidDataException("Give the button a label (up to 32 characters), a #RRGGBB colour and a saved game.");
    }

    // Own copies keep a button usable after its original save/artwork is moved.
    public async Task<GameShortcut> PrepareAsync(string file, string label, string color, AudienceDesignSettings audience)
    {
        var json = await File.ReadAllTextAsync(file);
        var archive = JsonSerializer.Deserialize<GameArchive>(json) ?? throw new InvalidDataException("Invalid saved game.");
        GameArchiveValidator.Validate(archive);
        var slot = new GameShortcut { Label = label.Trim(), Color = color, GameFile = Path.GetFullPath(file), Audience = audience.Clone() };
        Validate(slot);
        var folder = Path.Combine(_folder, "game-buttons", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        if (!string.IsNullOrWhiteSpace(slot.Audience.BackgroundImagePath))
        {
            var image = Path.Combine(folder, "background" + Path.GetExtension(slot.Audience.BackgroundImagePath));
            File.Copy(slot.Audience.BackgroundImagePath, image);
            slot.Audience.BackgroundImagePath = image;
        }
        slot.GameFile = Path.Combine(folder, "game.hmbgame");
        await AtomicFile.WriteAllTextAsync(slot.GameFile, json);
        return slot;
    }

    public async Task<GameLoadResult> ActivateAsync(AppDatabase db, GameShortcut slot)
    {
        Validate(slot);
        if (slot.ResumeGameId is long id)
        {
            var game = (await db.GetGameHistoryAsync()).FirstOrDefault(g => g.Id == id);
            if (game is not null && await db.GetSessionCodeAsync(id) == slot.ResumeSessionCode)
            {
                await db.ReopenGameAsync(id);
                return new GameLoadResult { GameId = id, RestoredCards = await db.GetCardCountAsync(id),
                    MissingFiles = MusicHealthService.Check(await db.GetGamePoolAsync(id)).Count };
            }
        }
        var loaded = await new GameFileService(db).LoadAsync(slot.GameFile);
        slot.ResumeGameId = loaded.GameId;
        slot.ResumeSessionCode = await db.GetSessionCodeAsync(loaded.GameId);
        return loaded;
    }
}
