namespace HazzMusicBingo.Models;

public sealed class Track
{
    public long Id { get; set; }
    public string FilePath { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public double DurationSeconds { get; set; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Artist) ? Title : $"{Title} — {Artist}";
}
