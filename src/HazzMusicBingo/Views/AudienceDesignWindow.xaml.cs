using HazzMusicBingo.Models;
using HazzMusicBingo.Controls;
using HazzMusicBingo.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using DrawingColor = System.Drawing.Color;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace HazzMusicBingo.Views;

public partial class AudienceDesignWindow : Window
{
    private AudienceDesignSettings _working;
    private bool _loading = true;
    private readonly AudienceDesignSettingsService _fileService = new();

    public AudienceDesignSettings Settings => _working;

    public AudienceDesignWindow(AudienceDesignSettings current)
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
        HeaderBox.Text = _working.HeaderText;
        ReadyBox.Text = _working.ReadyText;
        PlayedHeaderBox.Text = _working.PlayedHeaderText;

        FontBox.SelectedItem = _working.FontFamilyName;
        if (FontBox.SelectedItem is null)
            FontBox.Text = _working.FontFamilyName;

        HeaderSizeBox.Text = _working.HeaderFontSize.ToString("0");
        TitleSizeBox.Text = _working.TitleFontSize.ToString("0");
        ArtistSizeBox.Text = _working.ArtistFontSize.ToString("0");
        PlayedSizeBox.Text = _working.PlayedFontSize.ToString("0");

        ShowArtistBox.IsChecked = _working.ShowArtist;
        BoldTitleBox.IsChecked = _working.BoldTitle;
        OutlineTextBox.IsChecked = _working.UseTextOutline;
        OutlineWidthBox.Text = _working.TextOutlineWidth.ToString("0.0");

        BackgroundImageBox.Text = _working.BackgroundImagePath;
        ImageOpacitySlider.Value = _working.BackgroundImageOpacity;

        RefreshColourButtons();
    }

    private void ReadControls()
    {
        _working.HeaderText = HeaderBox.Text.Trim();
        _working.ReadyText = string.IsNullOrWhiteSpace(ReadyBox.Text)
            ? "READY"
            : ReadyBox.Text.Trim();
        _working.PlayedHeaderText = string.IsNullOrWhiteSpace(PlayedHeaderBox.Text)
            ? "PLAYED SONGS"
            : PlayedHeaderBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(FontBox.Text))
            _working.FontFamilyName = FontBox.Text;

        if (double.TryParse(HeaderSizeBox.Text, out var header))
            _working.HeaderFontSize = Math.Clamp(header, 12, 80);

        if (double.TryParse(TitleSizeBox.Text, out var title))
            _working.TitleFontSize = Math.Clamp(title, 20, 140);

        if (double.TryParse(ArtistSizeBox.Text, out var artist))
            _working.ArtistFontSize = Math.Clamp(artist, 14, 90);

        if (double.TryParse(PlayedSizeBox.Text, out var played))
            _working.PlayedFontSize = Math.Clamp(played, 12, 60);

        _working.ShowArtist = ShowArtistBox.IsChecked == true;
        _working.BoldTitle = BoldTitleBox.IsChecked == true;
        _working.UseTextOutline = OutlineTextBox.IsChecked == true;

        if (double.TryParse(OutlineWidthBox.Text, out var outlineWidth))
            _working.TextOutlineWidth = Math.Clamp(outlineWidth, 0, 12);

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
        var font = SafeFont(_working.FontFamilyName);

        PreviewRoot.Background = BrushFromHex(_working.BackgroundColor);

        PreviewHeader.Text = _working.HeaderText;
        PreviewHeader.FontFamily = font;
        PreviewHeader.FontSize = _working.HeaderFontSize;
        PreviewHeader.Foreground = BrushFromHex(_working.HeaderColor);
        PreviewHeader.Stroke = BrushFromHex(_working.TextOutlineColor);
        PreviewHeader.StrokeThickness =
            _working.UseTextOutline ? _working.TextOutlineWidth : 0;

        PreviewTitle.FontFamily = font;
        PreviewTitle.FontSize = _working.TitleFontSize;
        PreviewTitle.Foreground = BrushFromHex(_working.TitleColor);
        PreviewTitle.FontWeight =
            _working.BoldTitle ? FontWeights.Bold : FontWeights.Normal;
        PreviewTitle.Stroke = BrushFromHex(_working.TextOutlineColor);
        PreviewTitle.StrokeThickness =
            _working.UseTextOutline ? _working.TextOutlineWidth : 0;

        PreviewArtist.FontFamily = font;
        PreviewArtist.FontSize = _working.ArtistFontSize;
        PreviewArtist.Foreground = BrushFromHex(_working.ArtistColor);
        PreviewArtist.Stroke = BrushFromHex(_working.TextOutlineColor);
        PreviewArtist.StrokeThickness =
            _working.UseTextOutline ? _working.TextOutlineWidth : 0;
        PreviewArtist.Visibility =
            _working.ShowArtist ? Visibility.Visible : Visibility.Collapsed;

        PreviewBackgroundImage.Source = null;
        PreviewBackgroundImage.Opacity = _working.BackgroundImageOpacity;

        if (!string.IsNullOrWhiteSpace(_working.BackgroundImagePath)
            && File.Exists(_working.BackgroundImagePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource =
                    new Uri(_working.BackgroundImagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                PreviewBackgroundImage.Source = bitmap;
            }
            catch
            {
                PreviewBackgroundImage.Source = null;
            }
        }

        RefreshColourButtons();
    }

    private void RefreshColourButtons()
    {
        SetButtonSwatch(HeaderColourButton, _working.HeaderColor);
        SetButtonSwatch(TitleColourButton, _working.TitleColor);
        SetButtonSwatch(OutlineColourButton, _working.TextOutlineColor);
        SetButtonSwatch(ArtistColourButton, _working.ArtistColor);
        SetButtonSwatch(PlayedColourButton, _working.PlayedTextColor);
        SetButtonSwatch(BackgroundColourButton, _working.BackgroundColor);
    }

    private static void SetButtonSwatch(WpfButton button, string hex)
    {
        button.BorderBrush = BrushFromHex(hex);
        button.BorderThickness = new Thickness(3);
    }

    private string PickColour(string current)
    {
        using var dialog = new Forms.ColorDialog
        {
            FullOpen = true
        };

        try
        {
            var wpf =
                (WpfColor)WpfColorConverter.ConvertFromString(current);
            dialog.Color =
                DrawingColor.FromArgb(wpf.A, wpf.R, wpf.G, wpf.B);
        }
        catch
        {
            dialog.Color = DrawingColor.Black;
        }

        return dialog.ShowDialog() == Forms.DialogResult.OK
            ? $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"
            : current;
    }

    private void OutlineColour_Click(object sender, RoutedEventArgs e)
    {
        _working.TextOutlineColor = PickColour(_working.TextOutlineColor);
        UpdatePreview();
    }

    private void HeaderColour_Click(object sender, RoutedEventArgs e)
    {
        _working.HeaderColor = PickColour(_working.HeaderColor);
        UpdatePreview();
    }

    private void TitleColour_Click(object sender, RoutedEventArgs e)
    {
        _working.TitleColor = PickColour(_working.TitleColor);
        UpdatePreview();
    }

    private void ArtistColour_Click(object sender, RoutedEventArgs e)
    {
        _working.ArtistColor = PickColour(_working.ArtistColor);
        UpdatePreview();
    }

    private void PlayedColour_Click(object sender, RoutedEventArgs e)
    {
        _working.PlayedTextColor = PickColour(_working.PlayedTextColor);
        UpdatePreview();
    }

    private void BackgroundColour_Click(object sender, RoutedEventArgs e)
    {
        _working.BackgroundColor = PickColour(_working.BackgroundColor);
        UpdatePreview();
    }

    private void ChooseBackground_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new WpfOpenFileDialog
        {
            Title = "Choose Audience Background",
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
            Title = "Load Audience Design",
            Filter = "Hazz Audience Design|*.hmbaudience|JSON files|*.json|All files|*.*"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        _working = _fileService.LoadFromFile(dialog.FileName);

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
            Title = "Save Audience Design",
            Filter = "Hazz Audience Design|*.hmbaudience",
            DefaultExt = ".hmbaudience",
            AddExtension = true,
            FileName = "Music Bingo Audience Design"
        };

        if (dialog.ShowDialog(this) == true)
            _fileService.SaveToFile(_working, dialog.FileName);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _working = new AudienceDesignSettings();

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

    private static WpfFontFamily SafeFont(string name)
    {
        try
        {
            return new WpfFontFamily(
                string.IsNullOrWhiteSpace(name) ? "Arial" : name);
        }
        catch
        {
            return new WpfFontFamily("Arial");
        }
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try
        {
            var color =
                (WpfColor)WpfColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.White);
        }
    }
}
