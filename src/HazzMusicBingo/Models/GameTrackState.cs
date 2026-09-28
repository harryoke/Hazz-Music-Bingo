namespace HazzMusicBingo.Models;

public sealed class GameTrackState
{
    public Track Track { get; set; } = new();
    public int PlayOrder { get; set; }
    public bool IsPlayed { get; set; }
    public string? PlayedUtc { get; set; }
}
