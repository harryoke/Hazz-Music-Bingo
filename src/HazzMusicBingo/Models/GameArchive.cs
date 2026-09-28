namespace HazzMusicBingo.Models;

public sealed class GameArchive
{
    public int FormatVersion { get; set; } = 1;
    public WinningSettings Winning { get; set; } = new();
    public string? SessionCode { get; set; }
    public DateTime SavedUtc { get; set; } = DateTime.UtcNow;
    public List<GameArchiveTrack> Tracks { get; set; } = new();
    public List<GameArchiveCard> Cards { get; set; } = new();
}

public sealed class GameArchiveTrack
{
    public string FilePath { get; set; } = "";
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public double DurationSeconds { get; set; }
    // Retained for file-format compatibility only. It is ignored on load;
    // loaded games always receive a freshly randomized playback order.
    public int PlayOrder { get; set; }
    public bool IsPlayed { get; set; }
    public string? PlayedUtc { get; set; }
}

public sealed class GameArchiveCard
{
    public int CardNumber { get; set; }
    public List<string> SquareFilePaths { get; set; } = new();
}

public sealed class GameLoadResult
{
    public long GameId { get; set; }
    public int MissingFiles { get; set; }
    public int RestoredCards { get; set; }
}
