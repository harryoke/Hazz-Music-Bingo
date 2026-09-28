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

        BuildPreviewCells();
        LoadControls();
        _loading = false;
        UpdatePreview();
    }

    private void BuildPreviewCells()
    {
        var samples = new[]
        {
            "Take On Me\n- a-ha",
            "Africa\n- Toto",
            "Billie Jean\n- Michael Jackson",
            "Purple Rain\n- Prince",
            "Jump\n- Van Halen",
            "Beat It\n- Michael Jackson",
            "Like a Virgin\n- Madonna",
            "The Final Countdown\n- Europe",
            "Tainted Love\n- Soft Cell",
            "Under Pressure\n- Queen & David Bowie",
            "Careless Whisper\n- George Michael",
            "Thriller\n- Michael Jackson",
            "Sweet Child o' Mine\n- Guns N' Roses",
            "Eye of the Tiger\n- Survivor",
            "Summer of '69\n- Bryan Adams",
            "Every Breath You Take\n- The Police",
            "When Doves Cry\n- Prince",
            "Don't Stop Believin'\n- Journey",
            "Girls Just Want to Have Fun\n- Cyndi Lauper",
            "Flashdance\n- Irene Cara",
            "Sweet Dreams\n- Eurythmics",
            "With or Without You\n- U2",
            "Livin' on a Prayer\n- Bon Jovi",
            "Walk This Way\n- Run-D.M.C.",
            "I Wanna Dance with Somebody\n- Whitney Houston"
        };

        foreach (var sample in samples)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(0.6),
                Padding = new Thickness(4)
            };

            border.Child = new OutlinedTextBlock
            {
                Text = sample,
                Tag = sample,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = WpfHorizontalAlignment.Center,
                VerticalAlignment = WpfVerticalAlignment.Center,
                MaxLines = 4
            };

            PreviewGrid.Children.Add(border);
        }
    }

    private void LoadControls()
    {
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

        if (!string.IsNullOrWhiteSpace(FontBox.Text))
            _working.FontFamilyName = FontBox.Text;

        if (double.TryParse(TitleSizeBox.Text, out var titleSize))
            _working.TitleFontSize = Math.Clamp(titleSize, 12, 72);

        if (double.TryParse(HeaderSizeBox.Text, out var headerSize))
            _working.HeaderFontSize = Math.Clamp(headerSize, 10, 48);

        if (double.TryParse(CellSizeBox.Text, out var cellSize))
            _working.CellFontSize = Math.Clamp(cellSize, 8, 32);

        _working.ShowBingoLetters = ShowBingoLettersBox.IsChecked == true;
        _working.ShowArtist = ShowArtistBox.IsChecked == true;
        _working.BoldSongText = BoldSongsBox.IsChecked == true;
        _working.UseTextOutline = OutlineTextBox.IsChecked == true;

        if (double.TryParse(OutlineWidthBox.Text, out var outlineWidth))
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
        var font = new WpfFontFamily(_working.FontFamilyName);

        PreviewTitle.Text = _working.Title;
        PreviewTitle.FontFamily = font;
        PreviewTitle.FontSize = _working.TitleFontSize;
        PreviewTitle.Foreground = BrushFromHex(_working.TitleColor);
        PreviewTitle.Stroke = BrushFromHex(_working.TextOutlineColor);
        PreviewTitle.StrokeThickness =
            _working.UseTextOutline ? _working.TextOutlineWidth : 0;

        PreviewHeader.Text = _working.HeaderText;
        PreviewHeader.FontFamily = font;
        PreviewHeader.FontSize = _working.HeaderFontSize;
        PreviewHeader.Foreground = BrushFromHex(_working.TextColor);
        PreviewHeader.Stroke = BrushFromHex(_working.TextOutlineColor);
        PreviewHeader.StrokeThickness =
            _working.UseTextOutline ? _working.TextOutlineWidth : 0;
        PreviewHeader.Visibility =
            string.IsNullOrWhiteSpace(_working.HeaderText)
                ? Visibility.Collapsed
                : Visibility.Visible;

        PreviewLetters.Visibility =
            _working.ShowBingoLetters ? Visibility.Visible : Visibility.Collapsed;

        foreach (OutlinedTextBlock letter in PreviewLetters.Children)
        {
            letter.FontFamily = font;
            letter.Foreground = BrushFromHex(_working.TitleColor);
            letter.Stroke = BrushFromHex(_working.TextOutlineColor);
            letter.StrokeThickness =
                _working.UseTextOutline ? _working.TextOutlineWidth : 0;
        }

        PreviewPage.Background = BrushFromHex(_working.PageBackgroundColor);

        if (!string.IsNullOrWhiteSpace(_working.BackgroundImagePath)
            && File.Exists(_working.BackgroundImagePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(_working.BackgroundImagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                PreviewPage.Background = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.Uniform,
                    Opacity = _working.BackgroundImageOpacity
                };
            }
            catch
            {
                PreviewPage.Background = BrushFromHex(_working.PageBackgroundColor);
            }
        }

        foreach (Border border in PreviewGrid.Children)
        {
            border.BorderBrush = BrushFromHex(_working.GridLineColor);

            var cellBrush = BrushFromHex(_working.CellBackgroundColor);
            cellBrush.Opacity = _working.CellBackgroundOpacity;
            border.Background = cellBrush;

            if (border.Child is OutlinedTextBlock text)
            {
                var fullSample = text.Tag?.ToString() ?? text.Text;
                text.Text = _working.ShowArtist
                    ? fullSample
                    : fullSample.Split('\n')[0];

                text.FontFamily = font;
                text.FontSize = _working.CellFontSize;
                text.Foreground = BrushFromHex(_working.TextColor);
                text.FontWeight =
                    _working.BoldSongText ? FontWeights.Bold : FontWeights.Normal;
                text.Stroke = BrushFromHex(_working.TextOutlineColor);
                text.StrokeThickness =
                    _working.UseTextOutline
                        ? _working.TextOutlineWidth
                        : 0;
            }
        }

        RefreshColourButtons();
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
