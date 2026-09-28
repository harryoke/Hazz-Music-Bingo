using HazzMusicBingo.Models;
using System.Windows;

namespace HazzMusicBingo.Views;

public partial class PlayedSongsWindow : Window
{
    private readonly IReadOnlyList<Track> _tracks;
    private readonly Action<int>? _pageChanged;
    private int _pageIndex;

    public PlayedSongsWindow(
        IReadOnlyList<Track> tracks,
        Action<int>? pageChanged = null)
    {
        InitializeComponent();

        _tracks =
            (tracks ?? Array.Empty<Track>())
            .OrderBy(
                t => t.Title,
                StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(
                t => t.Artist,
                StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _pageChanged = pageChanged;

        TotalSongsText.Text =
            $"{_tracks.Count} song{(_tracks.Count == 1 ? "" : "s")} played";

        ShowPage(0, notifyAudience: false);
    }

    private int PageCount =>
        Math.Max(
            1,
            (int)Math.Ceiling(
                _tracks.Count /
                (double)AudienceWindow.PlayedSongsPageSize));

    private void ShowPage(
        int pageIndex,
        bool notifyAudience = true)
    {
        _pageIndex =
            Math.Clamp(
                pageIndex,
                0,
                PageCount - 1);

        var start =
            _pageIndex *
            AudienceWindow.PlayedSongsPageSize;

        var page =
            _tracks
            .Skip(start)
            .Take(AudienceWindow.PlayedSongsPageSize)
            .ToList();

        SongsList.ItemsSource = page;

        PageText.Text =
            $"Page {_pageIndex + 1} of {PageCount}";

        PreviousButton.IsEnabled =
            _pageIndex > 0;

        NextButton.IsEnabled =
            _pageIndex < PageCount - 1;

        if (_tracks.Count == 0)
        {
            RangeText.Text =
                "No songs have been played yet.";
        }
        else
        {
            var end =
                Math.Min(
                    start + page.Count,
                    _tracks.Count);

            RangeText.Text =
                $"Showing songs {start + 1}-{end} of {_tracks.Count}";
        }

        if (notifyAudience)
            _pageChanged?.Invoke(_pageIndex);
    }

    private void Previous_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowPage(_pageIndex - 1);
    }

    private void Next_Click(
        object sender,
        RoutedEventArgs e)
    {
        ShowPage(_pageIndex + 1);
    }

    private void Close_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
