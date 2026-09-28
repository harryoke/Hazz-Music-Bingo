namespace HazzMusicBingo.Models;

public sealed record GameSummary(long Id, string SessionCode, string CreatedUtc, string Status, int Played, int Cards)
{
    public string CreatedLocal => DateTime.TryParse(CreatedUtc, out var date) ? date.ToLocalTime().ToString("g") : CreatedUtc;
    public string Progress => $"{Played} / 60";
}
