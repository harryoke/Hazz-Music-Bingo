using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace HazzMusicBingo.Views;

public partial class CardCountWindow : Window
{
    public int CardCount { get; private set; } = 20;

    public CardCountWindow() => InitializeComponent();

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CountBox.Text, out var count) || count < 1 || count > 60)
        {
            MessageBox.Show(
                this,
                "Enter a number from 1 to 60.",
                "Invalid card count",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        CardCount = count;
        DialogResult = true;
    }
}
