using HazzMusicBingo.Models;
using System.IO;
using System.Text.Json;

namespace HazzMusicBingo.Services;

public sealed class CardDesignSettingsService
{
    private readonly string _settingsPath;

    public CardDesignSettingsService()
    {
        var folder = AppStorage.Folder;

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "card-design.json");
    }

    public CardDesignSettings Load() => LoadFromFile(_settingsPath);

    public void Save(CardDesignSettings settings) =>
        SaveToFile(settings, _settingsPath);

    public CardDesignSettings LoadFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new CardDesignSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CardDesignSettings>(json)
                   ?? new CardDesignSettings();
        }
        catch
        {
            return new CardDesignSettings();
        }
    }

    public void SaveToFile(CardDesignSettings settings, string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(settings, options));
    }
}
