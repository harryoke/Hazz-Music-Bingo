using HazzMusicBingo.Data;
using HazzMusicBingo.Services;
using NAudio.Wave;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HazzMusicBingo.Views;

public partial class MusicHealthWindow : Window
{
    private readonly AppDatabase _db;
    private readonly long? _game;
    public MusicHealthWindow(AppDatabase db, long? game)
    {
        InitializeComponent(); _db = db; _game = game;
        GameOnlyBox.IsEnabled = game.HasValue; GameOnlyBox.IsChecked = game.HasValue;
        Loaded += Refresh_Click;
    }
    private async Task RefreshAsync()
    {
        RefreshButton.IsEnabled = RelinkButton.IsEnabled = false;
        try
        {
            SummaryText.Text = "Checking files…";
            var tracks = GameOnlyBox.IsChecked == true && _game.HasValue ? await _db.GetGamePoolAsync(_game.Value) : await _db.GetAllTracksAsync();
            var issues = await Task.Run(() => MusicHealthService.Check(tracks));
            IssuesGrid.ItemsSource = issues;
            SummaryText.Text = $"{tracks.Count} songs checked • {issues.Count} need attention";
        }
        finally { RefreshButton.IsEnabled = RelinkButton.IsEnabled = true; }
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try { await RefreshAsync(); }
        catch (Exception ex) { SummaryText.Text = "Check failed"; MessageBox.Show(this, ex.Message, "Music check"); }
    }
    private async void Relink_Click(object sender, RoutedEventArgs e)
    {
        if (IssuesGrid.SelectedItem is not MusicIssue issue) { MessageBox.Show(this, "Select a song first.", "Locate music"); return; }
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = $"Locate the same song: {issue.Title} — {issue.Artist}", Filter = "Audio files|*.mp3;*.wav;*.wma;*.m4a;*.aac;*.flac;*.ogg|All files|*.*" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            RelinkButton.IsEnabled = RefreshButton.IsEnabled = false;
            var duration = await Task.Run(() => { using var reader = new MediaFoundationReader(dialog.FileName); return reader.TotalTime.TotalSeconds; });
            await _db.RelinkTrackAsync(issue.Track.Id, dialog.FileName, duration);
            await RefreshAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not relink song"); }
        finally { RelinkButton.IsEnabled = RefreshButton.IsEnabled = true; }
    }
}
