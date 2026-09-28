using HazzMusicBingo.Data;
using HazzMusicBingo.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using Brushes = System.Windows.Media.Brushes;

namespace HazzMusicBingo.Views;

public partial class WinnerWindow : Window
{
    private readonly AppDatabase _db;
    private readonly long _game;
    public WinnerWindow(AppDatabase db, long game, string code, WinningPattern pattern = WinningPattern.AnyLine)
    {
        InitializeComponent(); _db = db; _game = game;
        PatternBox.SelectedIndex = (int)pattern;
        GameLabel.Text = $"WINNER CHECK • GAME {code}";
        Loaded += async (_, _) =>
        {
            try
            {
                var cards = await _db.GetCardsAsync(_game, 1000);
                if (cards.Count > 0) CardBox.Text = cards.Min(c => c.CardNumber).ToString();
                Check_Click(this, new RoutedEventArgs());
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Winner check"); }
        };
    }
    private async void Navigate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var cards = await _db.GetCardsAsync(_game, 1000);
            int.TryParse(CardBox.Text, out var current);
            var number = LiveWinnerService.Adjacent(cards.Select(c => c.CardNumber), current, ReferenceEquals(sender, NextButton));
            if (number.HasValue) { CardBox.Text = number.Value.ToString(); Check_Click(this, e); }
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Winner check"); }
    }
    private async void Check_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(CardBox.Text, out var number))
                throw new ArgumentException("Enter a saved card number.");
            var cards = await _db.GetCardsAsync(_game, 1000);
            PreviousButton.IsEnabled = LiveWinnerService.Adjacent(cards.Select(c => c.CardNumber), number, false).HasValue;
            NextButton.IsEnabled = LiveWinnerService.Adjacent(cards.Select(c => c.CardNumber), number, true).HasValue;
            var card = cards.SingleOrDefault(c => c.CardNumber == number)
                ?? throw new InvalidOperationException("This game has no saved card with that number. Generate the cards through Print cards first.");
            var played = await _db.GetPlayedTracksAlphabeticalAsync(_game);
            var result = WinnerService.Check(card, played.Select(t => t.Id), (WinningPattern)PatternBox.SelectedIndex);
            Squares.Children.Clear();
            for (var i = 0; i < 25; i++)
            {
                var track = card.Squares[i];
                Squares.Children.Add(new Border { Background = result.Marked[i] ? Brushes.Honeydew : Brushes.White,
                    BorderBrush = Brushes.SlateGray, BorderThickness = new Thickness(.5), Padding = new Thickness(8),
                    Child = new TextBlock { Text = $"{(result.Marked[i] ? "✓ " : "")}{track.Title}\n{track.Artist}",
                        TextWrapping = TextWrapping.Wrap, FontSize = 15, VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center } });
            }
            ResultText.Text = $"CARD {number:000}: {(result.IsWinner ? "BINGO — pattern complete" : "Not a winner yet")} • {result.MatchedCount}/25 marked";
            if (result.CompletedLines.Count > 0) ResultText.Text += "\n" + string.Join(" • ", result.CompletedLines);
        }
        catch (Exception ex) { Squares.Children.Clear(); ResultText.Text = "Unable to verify this card."; MessageBox.Show(this, ex.Message, "Winner check"); }
    }
}
