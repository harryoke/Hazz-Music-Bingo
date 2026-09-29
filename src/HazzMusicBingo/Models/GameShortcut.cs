namespace HazzMusicBingo.Models;

public sealed class GameShortcut
{
    public string Label { get; set; } = "";
    public string Color { get; set; } = "#4F46E5";
    public string GameFile { get; set; } = "";
    public long? ResumeGameId { get; set; }
    public string? ResumeSessionCode { get; set; }
    public AudienceDesignSettings Audience { get; set; } = new();
}
