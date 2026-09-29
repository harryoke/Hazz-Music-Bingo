using HazzMusicBingo.Helpers;
using System.IO;

namespace HazzMusicBingo.Services;

public static class TrackMetadataReader
{
    public static (string Artist, string Title) Read(string path)
    {
        var fallback = FilenameMetadata.Parse(path);
        if (!string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase)) return fallback;
        try
        {
            // Read-only: never call Save or change the user's music files.
            using var file = TagLib.File.Create(path, TagLib.ReadStyle.None);
            var title = file.Tag.Title?.Trim();
            var artists = file.Tag.Performers.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim());
            var artist = string.Join(" / ", artists);
            return (string.IsNullOrWhiteSpace(artist) ? fallback.Artist : artist,
                string.IsNullOrWhiteSpace(title) ? fallback.Title : title);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or TagLib.CorruptFileException
            or TagLib.UnsupportedFormatException or ArgumentException)
        {
            return fallback;
        }
    }
}
