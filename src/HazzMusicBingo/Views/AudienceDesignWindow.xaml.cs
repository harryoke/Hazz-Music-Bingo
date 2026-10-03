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
    private readonly AudienceWindow _preview;
    public AudienceDesignSettings Settings => _working;
    private AudienceTextRole SelectedRole => ElementBox.SelectedValue is AudienceTextRole role ? role : AudienceTextRole.Rule;

    public AudienceDesignWindow(AudienceDesignSettings current)
    {
        InitializeComponent();
        _working = current.Clone();
        _preview = new AudienceWindow(_working);
        var content = _preview.Content;
        _preview.Content = null;
        AudiencePreview.Content = content;
        Closed += (_, _) => _preview.Close();
        ElementFontBox.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source).Select(f => f.Source).ToList();
        ElementBox.ItemsSource = new Dictionary<AudienceTextRole, string>
        {
            [AudienceTextRole.Rule] = "Playing for a line / corners / house",
            [AudienceTextRole.Win] = "Winner announcement",
            [AudienceTextRole.Header] = "Now playing heading",
            [AudienceTextRole.Ready] = "Ready message",
            [AudienceTextRole.Title] = "Current song title",
            [AudienceTextRole.Artist] = "Current artist",
            [AudienceTextRole.PlayedHeader] = "Played songs heading",
            [AudienceTextRole.PlayedTitle] = "Played-list song titles",
            [AudienceTextRole.PlayedArtist] = "Played-list artists",
            [AudienceTextRole.Page] = "Page number",
            [AudienceTextRole.Empty] = "No songs played message"
        };
        ElementBox.SelectedIndex = 0;
        LoadControls();
        _loading = false;
        UpdatePreview();
    }

    private void LoadControls()
    {
        HeaderBox.Text = _working.HeaderText;
        ReadyBox.Text = _working.ReadyText;
        PlayedHeaderBox.Text = _working.PlayedHeaderText;
        ShowArtistBox.IsChecked = _working.ShowArtist;
        BackgroundImageBox.Text = _working.BackgroundImagePath;
        ImageOpacitySlider.Value = double.IsFinite(_working.BackgroundImageOpacity) ? Math.Clamp(_working.BackgroundImageOpacity, 0, 1) : 0.35;
        LoadElement();
    }

    private void LoadElement()
    {
        var wasLoading = _loading; _loading = true;
        var style = _working.StyleFor(SelectedRole);
        ElementFontBox.SelectedItem = style.FontFamilyName;
        if (ElementFontBox.SelectedItem is null)
        {
            var fonts = ((IEnumerable<string>)ElementFontBox.ItemsSource).ToList();
            fonts.Add(style.FontFamilyName); ElementFontBox.ItemsSource = fonts;
            ElementFontBox.SelectedItem = style.FontFamilyName;
        }
        ElementSizeBox.Text = style.Size.ToString("0.#");
        ElementBoldBox.IsChecked = style.Bold; ElementItalicBox.IsChecked = style.Italic;
        ElementOutlineBox.IsChecked = style.Outline; ElementOutlineWidthBox.Text = style.OutlineWidth.ToString("0.#");
        RefreshColourButtons(); _loading = wasLoading;
    }

    private void ReadControls()
    {
        _working.HeaderText = HeaderBox.Text.Trim();
        _working.ReadyText = string.IsNullOrWhiteSpace(ReadyBox.Text) ? "READY" : ReadyBox.Text.Trim();
        _working.PlayedHeaderText = string.IsNullOrWhiteSpace(PlayedHeaderBox.Text) ? "PLAYED SONGS" : PlayedHeaderBox.Text.Trim();
        _working.ShowArtist = ShowArtistBox.IsChecked == true;
        _working.BackgroundImageOpacity = ImageOpacitySlider.Value;
    }

    private void ElementChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        LoadElement(); UpdatePreview();
    }

    private void ElementStyleChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var style = _working.StyleFor(SelectedRole);
        if (ElementFontBox.SelectedItem is string font) style.FontFamilyName = font;
        if (double.TryParse(ElementSizeBox.Text, out var size) && double.IsFinite(size)) style.Size = Math.Clamp(size, 12, 140);
        if (double.TryParse(ElementOutlineWidthBox.Text, out var width) && double.IsFinite(width)) style.OutlineWidth = Math.Clamp(width, 0, 12);
        style.Bold = ElementBoldBox.IsChecked == true; style.Italic = ElementItalicBox.IsChecked == true;
        style.Outline = ElementOutlineBox.IsChecked == true;
        (_working.TextStyles ??= new())[SelectedRole.ToString()] = style;
        UpdatePreview();
    }

    private void ElementColour_Click(object sender, RoutedEventArgs e)
    {
        var style = _working.StyleFor(SelectedRole); style.Color = PickColour(style.Color);
        (_working.TextStyles ??= new())[SelectedRole.ToString()] = style; UpdatePreview();
    }
    private void ElementOutlineColour_Click(object sender, RoutedEventArgs e)
    {
        var style = _working.StyleFor(SelectedRole); style.OutlineColor = PickColour(style.OutlineColor);
        (_working.TextStyles ??= new())[SelectedRole.ToString()] = style; UpdatePreview();
    }
    private void DesignChanged(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        ReadControls(); UpdatePreview();
    }
    private void UpdatePreview()
    {
        _preview.ApplyDesign(_working);
        _preview.ShowWinningMessage(SelectedRole == AudienceTextRole.Win ? "FULL HOUSE WON!" : "WE ARE PLAYING FOR A FULL HOUSE");
        if (SelectedRole >= AudienceTextRole.PlayedHeader)
            _preview.ShowPlayedSongs(SelectedRole == AudienceTextRole.Empty ? Array.Empty<Track>() : Enumerable.Range(1, 6).Select(i => new Track { Title = $"Example song {i}", Artist = "Example artist" }).ToArray());
        else _preview.ShowTrack(SelectedRole == AudienceTextRole.Ready ? null : new Track { Title = "TAKE ON ME", Artist = "a-ha" });
        RefreshColourButtons();
    }
    private void RefreshColourButtons()
    {
        var style = _working.StyleFor(SelectedRole);
        SetButtonSwatch(ElementColourButton, style.Color);
        SetButtonSwatch(ElementOutlineColourButton, style.OutlineColor);
        SetButtonSwatch(BackgroundColourButton, _working.BackgroundColor);
    }
    private static void SetButtonSwatch(WpfButton button, string color)
    {
        button.BorderBrush = BrushFromHex(color); button.BorderThickness = new Thickness(3);
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
