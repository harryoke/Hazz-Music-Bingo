using HazzMusicBingo.Models;
using HazzMusicBingo.Controls;
using HazzMusicBingo.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;

namespace HazzMusicBingo.Views;

public partial class AudienceWindow : Window
{
    public const int PlayedSongsPageSize = 6;
    private Track? _currentTrack;
    private bool _showingPlayed;
    public void ShowWinningMessage(string message)
    {
        WinningMessage.Text = message;
        AudienceTextStyling.Apply(WinningMessage, _design,
            message.EndsWith("WON!", StringComparison.Ordinal) ? AudienceTextRole.Win : AudienceTextRole.Rule);
    }

    private AudienceDesignSettings _design;
    private IReadOnlyList<Track> _currentPlayedTracks = Array.Empty<Track>();
    private int _playedSongsPageIndex;

    public AudienceWindow(AudienceDesignSettings design)
    {
        InitializeComponent();
        _design = design.Clone();
        ApplyDesign(_design);
        MonitorHelper.ConfigureAudienceWindow(this);
    }

    public void ApplyDesign(AudienceDesignSettings design)
    {
        _design = design.Clone();

        AudienceRoot.Background = BrushFromHex(_design.BackgroundColor);
        AudienceHeader.Text = _design.HeaderText;
        PlayedHeader.Text = _design.PlayedHeaderText;
        AudienceTextStyling.Apply(AudienceHeader, _design, AudienceTextRole.Header);
        AudienceTextStyling.Apply(AudienceArtist, _design, AudienceTextRole.Artist);
        AudienceTextStyling.Apply(PlayedHeader, _design, AudienceTextRole.PlayedHeader);
        AudienceTextStyling.Apply(PlayedPageText, _design, AudienceTextRole.Page);
        AudienceArtist.Visibility = _design.ShowArtist ? Visibility.Visible : Visibility.Collapsed;
        ShowWinningMessage(WinningMessage.Text);
        LoadBackgroundImage();
        if (_showingPlayed) ShowPlayedSongs(_currentPlayedTracks, _playedSongsPageIndex);
        else ShowTrack(_currentTrack);
    }

    public void ShowTrack(Track? track)
    {
        _currentTrack = track;
        _showingPlayed = false;
        AudienceTextStyling.Apply(AudienceTitle, _design, track is null ? AudienceTextRole.Ready : AudienceTextRole.Title);
        PlayedPanel.Visibility = Visibility.Collapsed;
        NowPlayingPanel.Visibility = Visibility.Visible;

        if (track is null)
        {
            AudienceTitle.Text = _design.ReadyText;
            AudienceArtist.Text = "";
            return;
        }

        AudienceTitle.Text = track.Title;
        AudienceArtist.Text = _design.ShowArtist ? track.Artist : "";
    }

    public void ShowPlayedSongs(
        IReadOnlyList<Track> tracks,
        int pageIndex = 0)
    {
        _showingPlayed = true;
        _currentPlayedTracks =
            (tracks ?? Array.Empty<Track>())
            .OrderBy(
                t => t.Title,
                StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(
                t => t.Artist,
                StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var pageCount = Math.Max(
            1,
            (int)Math.Ceiling(
                _currentPlayedTracks.Count /
                (double)PlayedSongsPageSize));

        _playedSongsPageIndex =
            Math.Clamp(
                pageIndex,
                0,
                pageCount - 1);

        NowPlayingPanel.Visibility = Visibility.Collapsed;
        PlayedPanel.Visibility = Visibility.Visible;

        var pageTracks =
            _currentPlayedTracks
            .Skip(_playedSongsPageIndex * PlayedSongsPageSize)
            .Take(PlayedSongsPageSize)
            .ToList();

        BuildPlayedSongsGrid(pageTracks);

        PlayedPageText.Text =
            $"PAGE {_playedSongsPageIndex + 1} OF {pageCount}";

        PlayedPanel.UpdateLayout();
    }

    private void BuildPlayedSongsGrid(IReadOnlyList<Track> tracks)
    {
        PlayedSongsGrid.Children.Clear();
        PlayedSongsGrid.RowDefinitions.Clear();
        PlayedSongsGrid.ColumnDefinitions.Clear();

        const int columns = 2;
        const int rows = 3;

        for (var column = 0; column < columns; column++)
        {
            PlayedSongsGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
        }

        for (var row = 0; row < rows; row++)
        {
            PlayedSongsGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
        }

        var font = SafeFont(_design.FontFamilyName);
        var foreground = BrushFromHex(_design.PlayedTextColor);
        var background =
            new SolidColorBrush(
                WpfColor.FromArgb(42, 255, 255, 255));

        // Keep the played-song list readable regardless of how many songs
        // have been played. More songs create more pages, not smaller text.
        var playedFontSize =
            Math.Clamp(_design.PlayedFontSize, 28, 40);

        for (var i = 0; i < tracks.Count; i++)
        {
            var track = tracks[i];

            var border = new Border
            {
                ClipToBounds = true,
                Margin = new Thickness(7, 6, 7, 6),
                Padding = new Thickness(10, 8, 10, 8),
                Background = background.Clone(),
                CornerRadius = new CornerRadius(7),
                BorderBrush =
                    new SolidColorBrush(
                        WpfColor.FromArgb(55, 255, 255, 255)),
                BorderThickness = new Thickness(1)
            };

            var labels = new StackPanel { VerticalAlignment = System.Windows.VerticalAlignment.Center };
            var title = new OutlinedTextBlock { Text = track.Title, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxLines = 2 };
            AudienceTextStyling.Apply(title, _design, AudienceTextRole.PlayedTitle);
            labels.Children.Add(title);
            if (_design.ShowArtist)
            {
                var artist = new OutlinedTextBlock { Text = track.Artist, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxLines = 1 };
                AudienceTextStyling.Apply(artist, _design, AudienceTextRole.PlayedArtist);
                labels.Children.Add(artist);
            }
            border.Child = labels;

            Grid.SetRow(border, i / columns);
            Grid.SetColumn(border, i % columns);
            PlayedSongsGrid.Children.Add(border);
        }

        if (tracks.Count == 0)
        {
            var empty = new OutlinedTextBlock
            {
                Text = "No songs have been played yet.",
                FontFamily = font,
                FontSize = playedFontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = foreground,
                Stroke = BrushFromHex(_design.TextOutlineColor),
                StrokeThickness =
                    _design.UseTextOutline
                        ? _design.TextOutlineWidth
                        : 0,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment =
                    System.Windows.HorizontalAlignment.Center,
                VerticalAlignment =
                    System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(20)
            };

            AudienceTextStyling.Apply(empty, _design, AudienceTextRole.Empty);
            Grid.SetRowSpan(empty, rows);
            Grid.SetColumnSpan(empty, columns);
            PlayedSongsGrid.Children.Add(empty);
        }
    }

    private void LoadBackgroundImage()
    {
        AudienceBackgroundImage.Source = null;
        AudienceBackgroundImage.Opacity =
            Math.Clamp(_design.BackgroundImageOpacity, 0, 1);

        if (string.IsNullOrWhiteSpace(_design.BackgroundImagePath)
            || !File.Exists(_design.BackgroundImagePath))
            return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(
                _design.BackgroundImagePath,
                UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            AudienceBackgroundImage.Source = bitmap;
        }
        catch
        {
            AudienceBackgroundImage.Source = null;
        }
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
