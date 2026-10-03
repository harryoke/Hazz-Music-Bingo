namespace HazzMusicBingo.Models;

public enum AudienceTextRole
{
    Rule, Win, Header, Ready, Title, Artist, PlayedHeader, PlayedTitle, PlayedArtist, Page, Empty
}

public sealed class AudienceTextStyle
{
    public string FontFamilyName { get; set; } = "Arial";
    public double Size { get; set; } = 34;
    public string Color { get; set; } = "#FFFFFF";
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Outline { get; set; }
    public string OutlineColor { get; set; } = "#000000";
    public double OutlineWidth { get; set; } = 2;
    public AudienceTextStyle Clone() => (AudienceTextStyle)MemberwiseClone();
}

public sealed partial class AudienceDesignSettings
{
    // Optional additions preserve legacy JSON themes and button snapshots.
    public Dictionary<string, AudienceTextStyle> TextStyles { get; set; } = new();

    public AudienceTextStyle StyleFor(AudienceTextRole role)
    {
        if (TextStyles is not null && TextStyles.TryGetValue(role.ToString(), out var style) && style is not null)
            return style.Clone();
        var result = new AudienceTextStyle
        {
            FontFamilyName = FontFamilyName, Size = PlayedFontSize, Color = PlayedTextColor,
            Outline = UseTextOutline, OutlineColor = TextOutlineColor, OutlineWidth = TextOutlineWidth,
            Bold = true
        };
        switch (role)
        {
            case AudienceTextRole.Rule:
            case AudienceTextRole.Win:
                result.FontFamilyName = WinningFontFamilyName ?? FontFamilyName;
                result.Size = WinningFontSize; result.Color = WinningTextColor ?? TitleColor;
                result.Bold = WinningBold; result.Italic = WinningItalic;
                result.Outline = false; break;
            case AudienceTextRole.Header:
            case AudienceTextRole.PlayedHeader:
                result.Size = HeaderFontSize; result.Color = HeaderColor; break;
            case AudienceTextRole.Ready:
            case AudienceTextRole.Title:
                result.Size = TitleFontSize; result.Color = TitleColor; result.Bold = BoldTitle; break;
            case AudienceTextRole.Artist:
                result.Size = ArtistFontSize; result.Color = ArtistColor; result.Bold = false; break;
            case AudienceTextRole.Page:
                result.Size = Math.Clamp(PlayedFontSize, 24, 38); break;
            default:
                result.Size = Math.Clamp(PlayedFontSize, 28, 40); break;
        }
        return result;
    }
}
