using System.IO;
using HazzMusicBingo.Data;
using HazzMusicBingo.Helpers;
using NAudio.Wave;

namespace HazzMusicBingo.Services;

public sealed record ScanProgress(int Processed, int Total, string CurrentFile);

public sealed class LibraryScanner
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".wma", ".m4a", ".aac", ".flac", ".ogg"
        };

    private readonly AppDatabase _db;

    public LibraryScanner(AppDatabase db) => _db = db;

    public async Task<int> ScanAsync(
        string rootFolder,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var files = new List<string>();
        foreach (var path in Directory.EnumerateFiles(rootFolder, "*.*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SupportedExtensions.Contains(Path.GetExtension(path)))
                files.Add(path);
        }

        var processed = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (artist, title) = TrackMetadataReader.Read(file);
            var duration = TryGetDuration(file);
            var ticks = File.GetLastWriteTimeUtc(file).Ticks;

            await _db.UpsertTrackAsync(file, title, artist, duration, ticks);

            processed++;
            progress?.Report(new ScanProgress(processed, files.Count, file));
        }

        return processed;
    }

    private static double TryGetDuration(string file)
    {
        try
        {
            using var reader = new MediaFoundationReader(file);
            return reader.TotalTime.TotalSeconds;
        }
        catch
        {
            // The file still gets indexed. Playback will report a clear error if
            // Windows Media Foundation cannot decode it on this PC.
            return 0;
        }
    }
}
