using System.IO;

namespace HazzMusicBingo.Helpers;

public static class FilenameMetadata
{
    public static (string Artist, string Title) Parse(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath).Trim();

        // Common DJ naming: Artist - Title
        var parts = name.Split(new[] { " - " }, 2, StringSplitOptions.None);
        if (parts.Length == 2)
            return (Clean(parts[0]), Clean(parts[1]));

        return ("", Clean(name));
    }

    private static string Clean(string value) =>
        value.Replace('_', ' ').Trim();
}
