namespace HazzMusicBingo.Models;

public sealed partial class AudienceDesignSettings
{
    public string HeaderText { get; set; } = "NOW PLAYING";
    public string ReadyText { get; set; } = "READY";
    public string PlayedHeaderText { get; set; } = "PLAYED SONGS";
    public string FontFamilyName { get; set; } = "Arial";

    public string? WinningFontFamilyName { get; set; }
    public string? WinningTextColor { get; set; }
    public double WinningFontSize { get; set; } = 34;
    public bool WinningBold { get; set; } = true;
    public bool WinningItalic { get; set; }

    public double HeaderFontSize { get; set; } = 34;
    public double TitleFontSize { get; set; } = 72;
    public double ArtistFontSize { get; set; } = 40;
    public double PlayedFontSize { get; set; } = 25;

    public string HeaderColor { get; set; } = "#A8B3C5";
    public string TitleColor { get; set; } = "#FFFFFF";
    public string ArtistColor { get; set; } = "#D4DCEA";
    public string PlayedTextColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#111722";

    public string BackgroundImagePath { get; set; } = "";
    public double BackgroundImageOpacity { get; set; } = 0.35;

    public bool ShowArtist { get; set; } = true;
    public bool BoldTitle { get; set; } = true;

    public bool UseTextOutline { get; set; } = false;
    public string TextOutlineColor { get; set; } = "#000000";
    public double TextOutlineWidth { get; set; } = 2.0;

    public AudienceDesignSettings Clone() => new()
    {
        TextStyles = (TextStyles ?? new()).Where(p => p.Value is not null).ToDictionary(p => p.Key, p => p.Value.Clone()),
        WinningFontFamilyName = WinningFontFamilyName,
        WinningTextColor = WinningTextColor,
        WinningFontSize = WinningFontSize,
        WinningBold = WinningBold,
        WinningItalic = WinningItalic,
        HeaderText = HeaderText,
        ReadyText = ReadyText,
        PlayedHeaderText = PlayedHeaderText,
        FontFamilyName = FontFamilyName,
        HeaderFontSize = HeaderFontSize,
        TitleFontSize = TitleFontSize,
        ArtistFontSize = ArtistFontSize,
        PlayedFontSize = PlayedFontSize,
        HeaderColor = HeaderColor,
        TitleColor = TitleColor,
        ArtistColor = ArtistColor,
        PlayedTextColor = PlayedTextColor,
        BackgroundColor = BackgroundColor,
        BackgroundImagePath = BackgroundImagePath,
        BackgroundImageOpacity = BackgroundImageOpacity,
        ShowArtist = ShowArtist,
        BoldTitle = BoldTitle,
        UseTextOutline = UseTextOutline,
        TextOutlineColor = TextOutlineColor,
        TextOutlineWidth = TextOutlineWidth
    };
}
