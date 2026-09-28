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
    public WinnerWindow(AppDatabase db, long game, string code)
    {
        InitializeComponent(); _db = db; _game = game;
        GameLabel.Text = $"WINNER CHECK • GAME {code}";
        Loaded += Check_Click;
    }
    private async void Check_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(CardBox.Text, out var number) || number < 1 || number > 60)
                throw new ArgumentException("Enter a card number from 1 to 60.");
            var card = (await _db.GetCardsAsync(_game, 60)).SingleOrDefault(c => c.CardNumber == number)
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
