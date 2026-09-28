using HazzMusicBingo.Models;

namespace HazzMusicBingo.Services;

public enum WinningPattern { AnyLine, FourCorners, FullHouse }
public sealed record WinnerResult(bool IsWinner, bool[] Marked, int MatchedCount, IReadOnlyList<string> CompletedLines);

public static class WinnerService
{
    public static WinnerResult Check(BingoCard card, IEnumerable<long> playedTrackIds, WinningPattern pattern)
    {
        if (card.Squares.Count != 25 || card.Squares.Select(t => t.Id).Distinct().Count() != 25)
            throw new ArgumentException("The saved card must contain 25 different songs.");
        var played = playedTrackIds.ToHashSet();
        var marked = card.Squares.Select(t => played.Contains(t.Id)).ToArray();
        var lines = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            if (Enumerable.Range(0, 5).All(c => marked[i * 5 + c])) lines.Add($"Row {i + 1}");
            if (Enumerable.Range(0, 5).All(r => marked[r * 5 + i])) lines.Add($"Column {i + 1}");
        }
        var winner = pattern switch
        {
            WinningPattern.AnyLine => lines.Count > 0,
            WinningPattern.FourCorners => new[] { 0, 4, 20, 24 }.All(i => marked[i]),
            WinningPattern.FullHouse => marked.All(m => m),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern))
        };
        return new(winner, marked, marked.Count(m => m), lines);
    }
}
