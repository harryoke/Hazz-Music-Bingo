using HazzMusicBingo.Services;

namespace HazzMusicBingo.Models;

public sealed class WinningSettings
{
    public WinningPattern Pattern { get; set; } = WinningPattern.AnyLine;
    public int? FirstCard { get; set; }
    public int? LastCard { get; set; }
    public List<string> AcknowledgedWinners { get; set; } = new();
}
