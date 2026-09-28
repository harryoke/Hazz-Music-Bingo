using HazzMusicBingo.Models;
using HazzMusicBingo.Controls;
using HazzMusicBingo.Services;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WpfButton = System.Windows.Controls.Button;
using System.Windows.Media;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfVerticalAlignment = System.Windows.VerticalAlignment;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfFontFamily = System.Windows.Media.FontFamily;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace HazzMusicBingo.Views;

public partial class CardDesignWindow : Window
{
    private CardDesignSettings _working;
    private bool _loading = true;
    private readonly CardDesignSettingsService _designFileService = new();

    public CardDesignSettings Settings => _working;

    public CardDesignWindow(CardDesignSettings current)
    {
        InitializeComponent();
        _working = current.Clone();

        FontBox.ItemsSource = Fonts.SystemFontFamilies
            .OrderBy(f => f.Source)
            .Select(f => f.Source)
            .ToList();


        LoadControls();
        _loading = false;
        UpdatePreview();
    }

    private void LoadControls()
    {
        FooterBox.Text = _working.FooterText;
        GridWidthBox.Text = _working.GridLineWidth.ToString("0.##");
        PaddingBox.Text = _working.CellPadding.ToString("0.##");
        AlternateRowsBox.IsChecked = _working.AlternateRows;
        LeftAlignBox.IsChecked = _working.LeftAlignSongs;
        TitleBox.Text = _working.Title;
        HeaderBox.Text = _working.HeaderText;
        FontBox.SelectedItem = _working.FontFamilyName;
        if (FontBox.SelectedItem is null)
            FontBox.Text = _working.FontFamilyName;

        TitleSizeBox.Text = _working.TitleFontSize.ToString("0");
        HeaderSizeBox.Text = _working.HeaderFontSize.ToString("0");
        CellSizeBox.Text = _working.CellFontSize.ToString("0");

        ShowBingoLettersBox.IsChecked = _working.ShowBingoLetters;
        ShowArtistBox.IsChecked = _working.ShowArtist;
        BoldSongsBox.IsChecked = _working.BoldSongText;
        OutlineTextBox.IsChecked = _working.UseTextOutline;
        OutlineWidthBox.Text = _working.TextOutlineWidth.ToString("0.0");

        CellOpacitySlider.Value = _working.CellBackgroundOpacity;
        ImageOpacitySlider.Value = _working.BackgroundImageOpacity;
        BackgroundImageBox.Text = _working.BackgroundImagePath;

        RefreshColourButtons();
    }

    private void ReadControls()
    {
        _working.Title = string.IsNullOrWhiteSpace(TitleBox.Text)
            ? "MUSIC BINGO"
            : TitleBox.Text.Trim();

        _working.HeaderText = HeaderBox.Text.Trim();

        // SelectionChanged can fire before ComboBox.Text has caught up.
        if (FontBox.SelectedItem is string selectedFont)
            _working.FontFamilyName = selectedFont;
        _working.FooterText = FooterBox.Text.Trim();
        _working.AlternateRows = AlternateRowsBox.IsChecked == true;
        _working.LeftAlignSongs = LeftAlignBox.IsChecked == true;
        if (double.TryParse(GridWidthBox.Text, out var gridWidth) && double.IsFinite(gridWidth))
            _working.GridLineWidth = Math.Clamp(gridWidth, .25, 4);
        if (double.TryParse(PaddingBox.Text, out var padding) && double.IsFinite(padding))
            _working.CellPadding = Math.Clamp(padding, 2, 16);

        if (double.TryParse(TitleSizeBox.Text, out var titleSize) && double.IsFinite(titleSize))
            _working.TitleFontSize = Math.Clamp(titleSize, 12, 72);

        if (double.TryParse(HeaderSizeBox.Text, out var headerSize) && double.IsFinite(headerSize))
            _working.HeaderFontSize = Math.Clamp(headerSize, 10, 48);

        if (double.TryParse(CellSizeBox.Text, out var cellSize) && double.IsFinite(cellSize))
            _working.CellFontSize = Math.Clamp(cellSize, 8, 32);

        _working.ShowBingoLetters = ShowBingoLettersBox.IsChecked == true;
        _working.ShowArtist = ShowArtistBox.IsChecked == true;
        _working.BoldSongText = BoldSongsBox.IsChecked == true;
        _working.UseTextOutline = OutlineTextBox.IsChecked == true;

        if (double.TryParse(OutlineWidthBox.Text, out var outlineWidth) && double.IsFinite(outlineWidth))
            _working.TextOutlineWidth = Math.Clamp(outlineWidth, 0, 12);

        _working.CellBackgroundOpacity = CellOpacitySlider.Value;
        _working.BackgroundImageOpacity = ImageOpacitySlider.Value;
    }

    private void DesignChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
            return;

        ReadControls();
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var samples = new[] { "Take On Me", "Africa", "Billie Jean", "Purple Rain", "Jump",
            "Beat It", "Like a Virgin", "The Final Countdown", "Tainted Love", "Under Pressure",
            "Careless Whisper", "Thriller", "Sweet Child o' Mine", "Eye of the Tiger", "Summer of '69",
            "Every Breath You Take", "When Doves Cry", "Don't Stop Believin'", "Girls Just Want to Have Fun", "Flashdance",
            "Sweet Dreams", "With or Without You", "Livin' on a Prayer", "Walk This Way", "I Wanna Dance with Somebody" };
        var artists = new[] { "a-ha", "Toto", "Michael Jackson", "Prince", "Van Halen", "Michael Jackson", "Madonna",
            "Europe", "Soft Cell", "Queen & David Bowie", "George Michael", "Michael Jackson", "Guns N' Roses", "Survivor",
            "Bryan Adams", "The Police", "Prince", "Journey", "Cyndi Lauper", "Irene Cara", "Eurythmics", "U2", "Bon Jovi",
            "Run-D.M.C.", "Whitney Houston" };
        var card = new BingoCard { CardNumber = 1, Squares = samples.Select((title, i) =>
            new Track { Id = i + 1, Title = title, Artist = artists[i] }).ToList() };
        CardPreview.Content = PrintService.CreateCardVisual(card, _working);
        RefreshColourButtons();
    }

    private void ApplyTheme_Click(object sender, RoutedEventArgs e)
    {
        ReadControls();
        _working.PageBackgroundColor = "#FFFFFF";
        _working.CellBackgroundColor = "#FFFFFF";
        _working.TextColor = _working.TitleColor = _working.GridLineColor = "#172033";
        _working.AlternateRowColor = "#EEF2FF";
        _working.AlternateRows = true;
        _working.BackgroundImagePath = "";
        _working.UseTextOutline = false;
        if (ThemeBox.SelectedIndex == 1)
        {
            _working.PageBackgroundColor = "#172033";
            _working.TextColor = "#FFFFFF";
            _working.TitleColor = "#F6CE76";
            _working.CellBackgroundColor = "#23334D";
            _working.AlternateRowColor = "#30415B";
            _working.GridLineColor = "#F6CE76";
        }
        else if (ThemeBox.SelectedIndex == 2)
        {
            _working.PageBackgroundColor = "#FFF5FC";
            _working.TitleColor = "#6D28D9";
            _working.AlternateRowColor = "#F2E8FF";
            _working.GridLineColor = "#9C71CF";
        }
        else if (ThemeBox.SelectedIndex == 3)
        {
            _working.TextColor = _working.TitleColor = _working.GridLineColor = "#000000";
            _working.BoldSongText = true;
            _working.CellFontSize = 22;
            _working.AlternateRows = false;
            _working.GridLineWidth = 2;
        }
        _loading = true;
        LoadControls();
        _loading = false;
        UpdatePreview();
    }
    private void RefreshColourButtons()
    {
        SetButtonSwatch(TextColourButton, _working.TextColor);
        SetButtonSwatch(TitleColourButton, _working.TitleColor);
        SetButtonSwatch(OutlineColourButton, _working.TextOutlineColor);
        SetButtonSwatch(PageColourButton, _working.PageBackgroundColor);
        SetButtonSwatch(CellColourButton, _working.CellBackgroundColor);
        SetButtonSwatch(GridColourButton, _working.GridLineColor);
    }

    private static void SetButtonSwatch(WpfButton button, string hex)
    {
        button.BorderBrush = BrushFromHex(hex);
        button.BorderThickness = new Thickness(3);
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.Black);
        }
    }

    private string PickColour(string current)
    {
        using var dialog = new Forms.ColorDialog
        {
            FullOpen = true
        };

        try
        {
            var wpf = (WpfColor)WpfColorConverter.ConvertFromString(current);
            dialog.Color = DrawingColor.FromArgb(wpf.A, wpf.R, wpf.G, wpf.B);
        }
        catch
        {
            dialog.Color = DrawingColor.Black;
        }

        return dialog.ShowDialog() == Forms.DialogResult.OK
            ? $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"
            : current;
    }

    private void TextColour_Click(object sender, RoutedEventArgs e)
    {
        _working.TextColor = PickColour(_working.TextColor);
        UpdatePreview();
    }

    private void TitleColour_Click(object sender, RoutedEventArgs e)
    {
        _working.TitleColor = PickColour(_working.TitleColor);
        UpdatePreview();
    }

    private void OutlineColour_Click(object sender, RoutedEventArgs e)
    {
        _working.TextOutlineColor = PickColour(_working.TextOutlineColor);
        UpdatePreview();
    }

    private void PageColour_Click(object sender, RoutedEventArgs e)
    {
        _working.PageBackgroundColor = PickColour(_working.PageBackgroundColor);
        UpdatePreview();
    }

    private void CellColour_Click(object sender, RoutedEventArgs e)
    {
        _working.CellBackgroundColor = PickColour(_working.CellBackgroundColor);
        UpdatePreview();
    }

    private void GridColour_Click(object sender, RoutedEventArgs e)
    {
        _working.GridLineColor = PickColour(_working.GridLineColor);
        UpdatePreview();
    }

    private void ChooseBackground_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Choose Bingo Card Background",
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp|All files|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            _working.BackgroundImagePath = dialog.FileName;
            BackgroundImageBox.Text = dialog.FileName;
            UpdatePreview();
        }
    }

    private void ClearBackground_Click(object sender, RoutedEventArgs e)
    {
        _working.BackgroundImagePath = "";
        BackgroundImageBox.Text = "";
        UpdatePreview();
    }

    private void LoadDesignFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Load Bingo Card Design",
            Filter = "Hazz Bingo Design|*.hmbdesign|JSON files|*.json|All files|*.*"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        _working = _designFileService.LoadFromFile(dialog.FileName);

        _loading = true;
        LoadControls();
        _loading = false;
        UpdatePreview();
    }

    private void SaveDesignFile_Click(object sender, RoutedEventArgs e)
    {
        ReadControls();

        var dialog = new WpfSaveFileDialog
        {
            Title = "Save Bingo Card Design",
            Filter = "Hazz Bingo Design|*.hmbdesign",
            DefaultExt = ".hmbdesign",
            AddExtension = true,
            FileName = "Music Bingo Card Design"
        };

        if (dialog.ShowDialog(this) == true)
            _designFileService.SaveToFile(_working, dialog.FileName);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _working = new CardDesignSettings();
        _loading = true;
        LoadControls();
        _loading = false;
        UpdatePreview();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ReadControls();
        DialogResult = true;
    }
}
