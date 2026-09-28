using HazzMusicBingo.Models;
using System.IO;

namespace HazzMusicBingo.Services;

public sealed record MusicIssue(Track Track, string Problem)
{
    public string Title => Track.Title;
    public string Artist => Track.Artist;
    public string FilePath => Track.FilePath;
}
public static class MusicHealthService
{
    public static List<MusicIssue> Check(IEnumerable<Track> tracks) => tracks
        .Select(t => !File.Exists(t.FilePath) ? new MusicIssue(t, "Missing or inaccessible")
            : t.DurationSeconds <= 0 ? new MusicIssue(t, "Could not decode when scanned; rescan or relink") : null)
        .Where(issue => issue is not null).Cast<MusicIssue>().ToList();
    public static bool IsAvailable(Track track) => File.Exists(track.FilePath) && track.DurationSeconds > 0;
}
