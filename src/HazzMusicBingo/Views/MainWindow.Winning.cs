using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using Brushes = System.Windows.Media.Brushes;
using MessageBox = System.Windows.MessageBox;

namespace HazzMusicBingo.Views;

public partial class MainWindow
{
    private WinningSettings _winning = new();
    private List<int> _winners = new();
    private long? _winningGame;

    private async Task RefreshWinningAsync()
    {
        var game = _gameId;
        if (game is null)
        {
            _winningGame = null; _winning = new(); _winners.Clear();
            WinnerStatusText.Text = "Generate or load a game to track winners.";
            _audience?.ShowWinningMessage("");
            return;
        }
        var settings = await _db.GetWinningSettingsAsync(game.Value);
        var cards = await _db.GetCardsAsync(game.Value, 1000);
        var played = await _db.GetPlayedTracksAlphabeticalAsync(game.Value);
        if (_gameId != game) return;
        if (_winningGame != game)
        {
            FirstCardBox.Text = settings.FirstCard?.ToString() ?? "1";
            LastCardBox.Text = settings.LastCard?.ToString() ?? (cards.Count == 0 ? "60" : cards.Max(c => c.CardNumber).ToString());
        }
        _winningGame = game; _winning = settings;
        _winners = LiveWinnerService.Detect(cards, played.Select(t => t.Id), settings);
        var pending = LiveWinnerService.Unacknowledged(_winners, settings);
        foreach (var button in new[] { LineRuleButton, CornersRuleButton, HouseRuleButton })
        {
            var selected = int.Parse(button.Tag.ToString()!) == (int)settings.Pattern;
            button.Background = selected ? Brushes.Indigo : Brushes.WhiteSmoke;
            button.Foreground = selected ? Brushes.White : Brushes.Black;
            button.Content = new TextBlock
            {
                Text = (selected ? "✓ " : "") + LiveWinnerService.Label((WinningPattern)int.Parse(button.Tag.ToString()!)),
                Foreground = button.Foreground
            };
            button.BorderThickness = new Thickness(selected ? 3 : 1);
        }
        RangeStatusText.Text = settings.FirstCard is null ? "Live tracking off — apply the sold-card range."
            : $"{settings.LastCard - settings.FirstCard + 1} cards in play: {settings.FirstCard}–{settings.LastCard}";
        WinnerStatusText.Text = _winners.Count == 0 ? $"{LiveWinnerService.Label(settings.Pattern)}: no winner yet"
            : $"{LiveWinnerService.Label(settings.Pattern)} WON — cards {string.Join(", ", _winners)}"
              + (pending.Count == 0 ? " • acknowledged" : $"\nAwaiting acknowledgement: {string.Join(", ", pending)}");
        WinnerStatusText.Background = pending.Count > 0 ? Brushes.LightGoldenrodYellow : Brushes.Transparent;
        AcknowledgeButton.IsEnabled = pending.Count > 0;
        _audience?.ShowWinningMessage(LiveWinnerService.AudienceMessage(settings.Pattern, _winners.Count > 0));
    }

    private async void ApplyRange_Click(object sender, RoutedEventArgs e)
    {
        if (!_gameId.HasValue) return;
        try
        {
            if (!int.TryParse(FirstCardBox.Text, out var first) || !int.TryParse(LastCardBox.Text, out var last))
                throw new ArgumentException("Enter whole card numbers for both ends of the range.");
            var settings = await _db.GetWinningSettingsAsync(_gameId.Value);
            settings.FirstCard = first; settings.LastCard = last;
            await _db.SaveWinningSettingsAsync(_gameId.Value, settings);
            await RefreshWinningAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Cards in play"); }
    }

    private async void WinningRule_Click(object sender, RoutedEventArgs e)
    {
        if (!_gameId.HasValue) return;
        try
        {
            var settings = await _db.GetWinningSettingsAsync(_gameId.Value);
            settings.Pattern = (WinningPattern)int.Parse(((Button)sender).Tag.ToString()!);
            await _db.SaveWinningSettingsAsync(_gameId.Value, settings);
            await RefreshWinningAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Winning rule"); }
    }

    private async void Acknowledge_Click(object sender, RoutedEventArgs e)
    {
        if (!_gameId.HasValue) return;
        try
        {
            foreach (var number in LiveWinnerService.Unacknowledged(_winners, _winning))
                _winning.AcknowledgedWinners.Add(LiveWinnerService.Key(_winning.Pattern, number));
            await _db.SaveWinningSettingsAsync(_gameId.Value, _winning);
            await RefreshWinningAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Acknowledge winner"); }
    }
}
