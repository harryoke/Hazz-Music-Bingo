using HazzMusicBingo.Data;
using HazzMusicBingo.Models;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HazzMusicBingo.Views;

public partial class GameHistoryWindow : Window
{
    private readonly AppDatabase _db;
    public long? SelectedGameId { get; private set; }
    public GameHistoryWindow(AppDatabase db)
    {
        InitializeComponent(); _db = db;
        Loaded += async (_, _) =>
        {
            try { var games = await _db.GetGameHistoryAsync(); HistoryGrid.ItemsSource = games; StatusText.Text = $"{games.Count} games in this computer's database. Imported copies can share a game code; use date and progress to choose."; }
            catch (Exception ex) { StatusText.Text = ex.Message; RecoverButton.IsEnabled = false; }
        };
    }
    private void Recover_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not GameSummary game) { MessageBox.Show(this, "Select a game first.", "Game history"); return; }
        SelectedGameId = game.Id;
        DialogResult = true;
    }
    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Title = "Create a new database backup", Filter = "SQLite database|*.db", FileName = $"HazzMusicBingo-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            BackupButton.IsEnabled = false;
            await Task.Run(() => _db.BackupAsync(dialog.FileName));
            StatusText.Text = "Database backup saved. It contains game history, cards and file paths; music files and design presets are separate.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Backup failed"); }
        finally { BackupButton.IsEnabled = true; }
    }
}
