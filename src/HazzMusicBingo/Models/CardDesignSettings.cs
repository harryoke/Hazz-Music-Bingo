namespace HazzMusicBingo.Models;

public sealed class CardDesignSettings
{
    public string Title { get; set; } = "MUSIC BINGO";
    public string HeaderText { get; set; } = "";
    public string FontFamilyName { get; set; } = "Arial";

    public double TitleFontSize { get; set; } = 34;
    public double HeaderFontSize { get; set; } = 18;
    public double CellFontSize { get; set; } = 16;

    public string TextColor { get; set; } = "#20242C";
    public string TitleColor { get; set; } = "#20242C";
    public string PageBackgroundColor { get; set; } = "#FFFFFF";
    public string CellBackgroundColor { get; set; } = "#FFFFFF";
    public string GridLineColor { get; set; } = "#20242C";

    public double CellBackgroundOpacity { get; set; } = 0.90;
    public string BackgroundImagePath { get; set; } = "";
    public double BackgroundImageOpacity { get; set; } = 0.30;

    public bool ShowBingoLetters { get; set; } = true;
    public bool ShowArtist { get; set; } = true;
    public bool BoldSongText { get; set; } = false;

    public bool UseTextOutline { get; set; } = false;
    public string TextOutlineColor { get; set; } = "#000000";
    public double TextOutlineWidth { get; set; } = 2.0;
    public string FooterText { get; set; } = "Listen • Mark your songs • Call BINGO!";
    public double GridLineWidth { get; set; } = 1;
    public double CellPadding { get; set; } = 8;
    public bool AlternateRows { get; set; }
    public string AlternateRowColor { get; set; } = "#EEF2FF";
    public bool LeftAlignSongs { get; set; }

    public CardDesignSettings Clone() => new()
    {
        Title = Title,
        HeaderText = HeaderText,
        FontFamilyName = FontFamilyName,
        TitleFontSize = TitleFontSize,
        HeaderFontSize = HeaderFontSize,
        CellFontSize = CellFontSize,
        TextColor = TextColor,
        TitleColor = TitleColor,
        PageBackgroundColor = PageBackgroundColor,
        CellBackgroundColor = CellBackgroundColor,
        GridLineColor = GridLineColor,
        CellBackgroundOpacity = CellBackgroundOpacity,
        BackgroundImagePath = BackgroundImagePath,
        BackgroundImageOpacity = BackgroundImageOpacity,
        ShowBingoLetters = ShowBingoLetters,
        ShowArtist = ShowArtist,
        BoldSongText = BoldSongText,
        UseTextOutline = UseTextOutline,
        TextOutlineColor = TextOutlineColor,
        TextOutlineWidth = TextOutlineWidth,
        FooterText = FooterText,
        GridLineWidth = GridLineWidth,
        CellPadding = CellPadding,
        AlternateRows = AlternateRows,
        AlternateRowColor = AlternateRowColor,
        LeftAlignSongs = LeftAlignSongs
    };
}
