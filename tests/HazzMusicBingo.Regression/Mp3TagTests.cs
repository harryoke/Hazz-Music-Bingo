using HazzMusicBingo.Data;
using HazzMusicBingo.Services;
using System.IO;
using System.Text;

internal static class Mp3TagTests
{
    public static async Task<int> Run(string folder)
    {
        var checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        var dir = Path.Combine(folder, "mp3-tags"); Directory.CreateDirectory(dir);
        byte[] Audio() => Enumerable.Range(0, 4).SelectMany(_ => new byte[] { 255, 251, 144, 0 }.Concat(new byte[413])).ToArray();
        byte[] Size(int n, bool sync) => sync ? new[] { (byte)(n >> 21 & 127), (byte)(n >> 14 & 127), (byte)(n >> 7 & 127), (byte)(n & 127) }
            : new[] { (byte)(n >> 24), (byte)(n >> 16), (byte)(n >> 8), (byte)n };
        byte[] Frame(string id, string text, bool v4)
        {
            var data = v4 ? new byte[] { 3 }.Concat(Encoding.UTF8.GetBytes(text)).ToArray()
                : new byte[] { 1, 255, 254 }.Concat(Encoding.Unicode.GetBytes(text)).ToArray();
            return Encoding.ASCII.GetBytes(id).Concat(Size(data.Length, v4)).Concat(new byte[2]).Concat(data).ToArray();
        }
        string Write(string filename, string? title, string? artist, bool v4)
        {
            var body = (title is null ? Array.Empty<byte>() : Frame("TIT2", title, v4))
                .Concat(artist is null ? Array.Empty<byte>() : Frame("TPE1", artist, v4)).ToArray();
            var path = Path.Combine(dir, filename);
            File.WriteAllBytes(path, new byte[] { 73, 68, 51, (byte)(v4 ? 4 : 3), 0, 0 }.Concat(Size(body.Length, true)).Concat(body).Concat(Audio()).ToArray());
            return path;
        }
        var unicode = Write("Wrong Artist - Wrong Title.mp3", "Été 日本", "Björk", false);
        var original = File.ReadAllBytes(unicode);
        Check(TrackMetadataReader.Read(unicode) == ("Björk", "Été 日本"), "ID3v2.3 Unicode tags override filenames");
        Check(File.ReadAllBytes(unicode).SequenceEqual(original), "Tag reading never modifies music");
        var modern = Write("fallback - modern.MP3", "Tagged title", "Artist A\0Artist B", true);
        Check(TrackMetadataReader.Read(modern) == ("Artist A / Artist B", "Tagged title"), "ID3v2.4 UTF-8 multi-artist and uppercase extension");
        var partial = Write("Filename Artist - Filename Title.mp3", "Tag title", null, true);
        Check(TrackMetadataReader.Read(partial) == ("Filename Artist", "Tag title"), "Missing artist falls back independently");
        var missingTitle = Write("Backup Artist - Backup Title.mp3", "  ", "Tag artist", true);
        Check(TrackMetadataReader.Read(missingTitle) == ("Tag artist", "Backup Title"), "Blank title falls back independently");
        var legacy = Path.Combine(dir, "legacy.mp3");
        var tag = new byte[128]; Encoding.ASCII.GetBytes("TAG").CopyTo(tag, 0);
        Encoding.Latin1.GetBytes("Legacy Title").CopyTo(tag, 3); Encoding.Latin1.GetBytes("Legacy Artist").CopyTo(tag, 33);
        File.WriteAllBytes(legacy, Audio().Concat(tag).ToArray());
        Check(TrackMetadataReader.Read(legacy) == ("Legacy Artist", "Legacy Title"), "ID3v1 tags supported");
        var broken = Path.Combine(dir, "Safe Artist - Safe Title.mp3"); File.WriteAllBytes(broken, [73, 68, 51, 4]);
        Check(TrackMetadataReader.Read(broken) == ("Safe Artist", "Safe Title"), "Corrupt MP3 falls back safely");
        Check(TrackMetadataReader.Read(Path.Combine(dir, "Other Artist - Other Title.wav")) == ("Other Artist", "Other Title"), "Other formats keep filename behaviour");
        var db = new AppDatabase(Path.Combine(folder, "tag-scan.db")); await db.InitializeAsync();
        await new LibraryScanner(db).ScanAsync(dir, null, CancellationToken.None);
        var indexed = await db.GetTrackByFilePathAsync(unicode);
        Check(indexed?.Title == "Été 日本" && indexed.Artist == "Björk", "Scanner stores embedded tags in database");
        var id = indexed!.Id;
        Write("Wrong Artist - Wrong Title.mp3", "Updated title", "Updated artist", true);
        await new LibraryScanner(db).ScanAsync(dir, null, CancellationToken.None);
        indexed = await db.GetTrackByFilePathAsync(unicode);
        Check(indexed!.Id == id && indexed.Title == "Updated title", "Rescan refreshes metadata without changing track identity");
        return checks;
    }
}
