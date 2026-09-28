using HazzMusicBingo.Data;
using HazzMusicBingo.Models;

namespace HazzMusicBingo.Services;

public sealed class CardGenerator
{
    private readonly AppDatabase _db;

    public CardGenerator(AppDatabase db) => _db = db;

    /// <summary>
    /// Generates and stores all 60 cards the first time cards are requested.
    ///
    /// Rule guaranteed for cards 1-60:
    /// - each card contains 25 different tracks from the game's 60-track pool
    /// - at any one grid position, a track is never repeated across the 60 cards
    /// - therefore no two cards can have the same track in the same position
    /// </summary>
    public async Task EnsureStrictCardsExistAsync(long gameId)
    {
        var existing = await _db.GetCardCountAsync(gameId);
        if (existing >= 60)
            return;

        if (existing != 0)
            throw new InvalidOperationException(
                "A partial card set already exists. Start a new game to regenerate the strict 60-card set.");

        var pool = await _db.GetGamePoolAsync(gameId);
        if (pool.Count != 60)
            throw new InvalidOperationException("Strict card generation requires a 60-song game pool.");

        List<BingoCard> cards;
        do
        {
            cards = BuildCardSet(pool);
        }
        while (HasDuplicateSongSets(cards));

        await _db.SaveCardsAsync(gameId, cards);
    }

    private static List<BingoCard> BuildCardSet(IReadOnlyList<Track> pool)
    {
        var permutation = pool.OrderBy(_ => Random.Shared.Next()).ToArray();

        // 25 unique offsets guarantee 25 unique tracks on each card.
        // Advancing the card number rotates all offsets through the 60-song
        // permutation, guaranteeing no same song in the same position.
        var offsets = Enumerable.Range(0, 60)
            .OrderBy(_ => Random.Shared.Next())
            .Take(25)
            .ToArray();

        var cards = new List<BingoCard>(60);

        for (var cardIndex = 0; cardIndex < 60; cardIndex++)
        {
            var card = new BingoCard
            {
                CardNumber = cardIndex + 1
            };

            for (var position = 0; position < 25; position++)
            {
                var poolIndex = (offsets[position] + cardIndex) % 60;
                card.Squares.Add(permutation[poolIndex]);
            }

            cards.Add(card);
        }

        return cards;
    }

    private static bool HasDuplicateSongSets(IReadOnlyList<BingoCard> cards)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var card in cards)
        {
            var key = string.Join(",",
                card.Squares.Select(t => t.Id).OrderBy(id => id));

            if (!seen.Add(key))
                return true;
        }

        return false;
    }
}
