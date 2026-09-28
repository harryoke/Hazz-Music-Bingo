using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace HazzMusicBingo.Views;

public partial class PrintCardsWindow : Window
{
    private readonly IReadOnlyList<BingoCard> _cards;
    private readonly CardDesignSettings _design;
    private readonly string _code;
    private PrintOptions _options = new();
    public PrintCardsWindow(IReadOnlyList<BingoCard> cards, CardDesignSettings design, string code)
    {
        InitializeComponent();
        _cards = cards; _design = design.Clone(); _code = code;
        GameLabel.Text = $"Game {code} • 60 saved layouts";
        Preview.CommandBindings.Add(new CommandBinding(ApplicationCommands.Print, (_, e) => { Print_Click(this, e); e.Handled = true; }));
        RefreshPreview();
        Loaded += (_, _) => Preview.FitToHeight();
    }
    private void RefreshPreview()
    {
        if (!int.TryParse(FirstBox.Text, out var first) || !int.TryParse(LastBox.Text, out var last)
            || !double.TryParse(MarginBox.Text, out var margin))
            throw new ArgumentException("Enter valid numbers for the card range and margin.");
        _options = new PrintOptions { FirstCard = first, LastCard = last, CardsPerPage = new[] { 1, 2, 4 }[PerPageBox.SelectedIndex],
            LetterPaper = PaperBox.SelectedIndex == 1, Landscape = OrientationBox.SelectedIndex == 1,
            InkSaver = InkSaverBox.IsChecked == true, MarginMm = margin };
        var document = PrintService.BuildDocument(_cards, _design, _options, _code);
        Preview.Document = document;
        if (IsLoaded) Preview.FitToHeight();
        SummaryText.Text = $"{last - first + 1} cards • {document.Pages.Count} sheets per copy\nCard numbers {first:000}–{last:000}";
    }
    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        try { RefreshPreview(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Print options"); }
    }
    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try { RefreshPreview(); PrintService.Print((System.Windows.Documents.FixedDocument)Preview.Document, _options); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not print"); }
    }
}
