using System.IO;

namespace HazzMusicBingo.Services;

public static class AppStorage
{
    // Useful for isolated testing or a deliberately separate event profile.
    public static string Folder => Environment.GetEnvironmentVariable("HAZZ_MUSIC_BINGO_DATA_DIR") is { Length: > 0 } custom
        ? Path.GetFullPath(custom)
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HazzMusicBingo");
}
