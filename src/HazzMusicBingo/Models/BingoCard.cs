namespace HazzMusicBingo.Models;

public sealed class BingoCard
{
    public long Id { get; set; }
    public long GameId { get; set; }
    public int CardNumber { get; set; }
    public List<Track> Squares { get; set; } = new();
}
