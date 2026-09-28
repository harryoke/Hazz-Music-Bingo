using HazzMusicBingo.Models;

namespace HazzMusicBingo.Services;

public static class LiveWinnerService
{
    public static void Validate(WinningSettings settings, IEnumerable<int> cardNumbers)
    {
        if (!Enum.IsDefined(settings.Pattern) || settings.AcknowledgedWinners is null)
            throw new ArgumentException("Invalid winning rule or acknowledgement state.");
        if (settings.FirstCard is null && settings.LastCard is null) return;
        var numbers = cardNumbers.ToHashSet();
        if (settings.FirstCard is not int first || settings.LastCard is not int last || first < 1 || last < first
            || (long)last - first + 1 > numbers.Count
            || !Enumerable.Range(first, last - first + 1).All(numbers.Contains))
            throw new ArgumentException("Enter a first and last card number from the saved card set, with first no greater than last. Generate cards through Print cards first.");
    }

    public static List<int> Detect(IReadOnlyList<BingoCard> cards, IEnumerable<long> playedIds, WinningSettings settings)
    {
        Validate(settings, cards.Select(c => c.CardNumber));
        if (settings.FirstCard is null) return new();
        var played = playedIds.ToHashSet();
        return cards.Where(c => c.CardNumber >= settings.FirstCard && c.CardNumber <= settings.LastCard
            && WinnerService.Check(c, played, settings.Pattern).IsWinner).Select(c => c.CardNumber).Order().ToList();
    }

    public static string Key(WinningPattern pattern, int card) => $"{(int)pattern}:{card}";
    public static List<int> Unacknowledged(IEnumerable<int> winners, WinningSettings settings) =>
        winners.Where(n => !settings.AcknowledgedWinners.Contains(Key(settings.Pattern, n))).ToList();
    public static string Label(WinningPattern pattern) => pattern switch
    {
        WinningPattern.AnyLine => "LINE", WinningPattern.FourCorners => "FOUR CORNERS", _ => "FULL HOUSE"
    };
    public static string AudienceMessage(WinningPattern pattern, bool won) => won
        ? $"{Label(pattern)} WON!" : $"WE ARE PLAYING FOR {(pattern == WinningPattern.FourCorners ? "" : "A ")}{Label(pattern)}";
    public static int? Adjacent(IEnumerable<int> numbers, int current, bool next) => next
        ? numbers.Where(n => n > current).Order().Cast<int?>().FirstOrDefault()
        : numbers.Where(n => n < current).OrderDescending().Cast<int?>().FirstOrDefault();
}
