using System.IO;
using System.Text.Json;

namespace HazzMusicBingo.Services;

public sealed class PlaybackSettingsService
{
    private readonly string _path = Path.Combine(AppStorage.Folder, "playback-settings.json");
    public int StartSeconds { get; set; }
    public static bool IsValid(int seconds) => seconds >= 0 && seconds <= 86400 && seconds % 30 == 0;
    public static TimeSpan ResolveStart(TimeSpan requested, TimeSpan length) =>
        requested > TimeSpan.Zero && requested < length ? requested : TimeSpan.Zero;
    public int Load()
    {
        try
        {
            var value = JsonSerializer.Deserialize<PlaybackSettingsService>(File.ReadAllText(_path))?.StartSeconds ?? 0;
            return IsValid(value) ? value : 0;
        }
        catch { return 0; }
    }
    public void Save(int seconds)
    {
        if (!IsValid(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
        Directory.CreateDirectory(AppStorage.Folder);
        StartSeconds = seconds;
        File.WriteAllText(_path, JsonSerializer.Serialize(this));
    }
}
