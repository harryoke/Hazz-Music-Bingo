using HazzMusicBingo.Models;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HazzMusicBingo.Views;

public partial class GameShortcutWindow : Window
{
    private readonly AudienceDesignSettings _current;
    public AudienceDesignSettings Audience { get; private set; }
    public string ButtonLabel => LabelBox.Text.Trim();
    public string ButtonColor => ColorBox.Text.Trim();
    public string GameFile => FileBox.Text.Trim();
    public GameShortcutWindow(GameShortcut? slot, AudienceDesignSettings current)
    {
        InitializeComponent();
        _current = current.Clone(); Audience = (slot?.Audience ?? current).Clone();
        LabelBox.Text = slot?.Label ?? ""; ColorBox.Text = slot?.Color ?? "#4F46E5"; FileBox.Text = slot?.GameFile ?? "";
        UpdateTheme();
    }
    private void UpdateTheme() => ThemeText.Text = $"Theme: {Audience.FontFamilyName} • {Audience.BackgroundColor} • {(string.IsNullOrEmpty(Audience.BackgroundImagePath) ? "no background image" : System.IO.Path.GetFileName(Audience.BackgroundImagePath))}";
    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Hazz Music Bingo game|*.hmbgame" };
        if (dialog.ShowDialog(this) == true) FileBox.Text = dialog.FileName;
    }
    private void Color_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            ColorBox.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }
    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AudienceDesignWindow(Audience) { Owner = this };
        if (dialog.ShowDialog() == true) { Audience = dialog.Settings.Clone(); UpdateTheme(); }
    }
    private void Current_Click(object sender, RoutedEventArgs e) { Audience = _current.Clone(); UpdateTheme(); }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ButtonLabel) || !System.IO.File.Exists(GameFile)
            || !System.Text.RegularExpressions.Regex.IsMatch(ButtonColor, "^#[0-9a-fA-F]{6}$"))
        { MessageBox.Show(this, "Enter a label, choose an existing saved game and select a #RRGGBB colour.", "Game button"); return; }
        DialogResult = true;
    }
}
