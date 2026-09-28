using HazzMusicBingo.Models;
using System.IO;
using System.Text.Json;

namespace HazzMusicBingo.Services;

public sealed class AudienceDesignSettingsService
{
    private readonly string _settingsPath;

    public AudienceDesignSettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HazzMusicBingo");

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "audience-design.json");
    }

    public AudienceDesignSettings Load() => LoadFromFile(_settingsPath);

    public void Save(AudienceDesignSettings settings) =>
        SaveToFile(settings, _settingsPath);

    public AudienceDesignSettings LoadFromFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AudienceDesignSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AudienceDesignSettings>(json)
                   ?? new AudienceDesignSettings();
        }
        catch
        {
            return new AudienceDesignSettings();
        }
    }

    public void SaveToFile(AudienceDesignSettings settings, string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(settings, options));
    }
}
