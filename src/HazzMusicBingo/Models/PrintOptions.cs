namespace HazzMusicBingo.Models;

public sealed class PrintOptions
{
    public int FirstCard { get; set; } = 1;
    public int LastCard { get; set; } = 20;
    public int CardsPerPage { get; set; } = 1;
    public bool Landscape { get; set; }
    public bool LetterPaper { get; set; }
    public bool InkSaver { get; set; }
    public double MarginMm { get; set; } = 10;
    public (double Width, double Height) PageSize
    {
        get
        {
            var w = LetterPaper ? 816 : 210 * 96 / 25.4;
            var h = LetterPaper ? 1056 : 297 * 96 / 25.4;
            return Landscape ? (h, w) : (w, h);
        }
    }
    public void Validate()
    {
        if (FirstCard < 1 || LastCard > 60 || FirstCard > LastCard)
            throw new ArgumentException("Choose a card range between 1 and 60, with the first no higher than the last.");
        if (CardsPerPage is not (1 or 2 or 4))
            throw new ArgumentException("Choose 1, 2 or 4 cards per page.");
        if (!double.IsFinite(MarginMm) || MarginMm < 5 || MarginMm > 30)
            throw new ArgumentException("Page margin must be between 5 and 30 mm.");
    }
}
