using HazzMusicBingo.Controls;
using HazzMusicBingo.Models;
using HazzMusicBingo.Services;
using HazzMusicBingo.Views;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

internal static class UiTests
{
    public static Task<int> Run(string outputFolder)
    {
        var done = new TaskCompletionSource<int>();
        var thread = new Thread(() =>
        {
            try { done.SetResult(Verify(outputFolder)); }
            catch (Exception ex) { done.SetException(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return done.Task;
    }
    private static int Verify(string outputFolder)
    {
        Directory.CreateDirectory(outputFolder);
        var count = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); count++; }
        var app = new HazzMusicBingo.App(); app.InitializeComponent();
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var design = new CardDesignSettings();
        var window = new CardDesignWindow(design);
        var fonts = (ComboBox)window.FindName("FontBox");
        Check(fonts.Items.Contains("Times New Roman"), "Font fixture exists");
        fonts.SelectedItem = "Times New Roman";
        Check(window.Settings.FontFamilyName == "Times New Roman", "Font updates immediately on selection event");
        var content = (ContentControl)window.FindName("CardPreview");
        var visual = (FrameworkElement)content.Content;
        Layout(visual, PrintService.CardWidth, PrintService.CardHeight);
        Check(Descendants(visual).OfType<OutlinedTextBlock>().All(t => t.FontFamily.Source == "Times New Roman"), "Entire preview uses selected font");
        Check(design.FontFamilyName == "Arial", "Designer isolates changes until Use Design");
        fonts.SelectedItem = "Arial";
        Check(window.Settings.FontFamilyName == "Arial", "Second selection does not lag behind");
        var preset = Path.Combine(outputFolder, "test.hmbdesign");
        var settings = window.Settings.Clone();
        settings.FooterText = "Custom footer"; settings.GridLineWidth = 2.5; settings.LeftAlignSongs = true;
        var presets = new CardDesignSettingsService(); presets.SaveToFile(settings, preset);
        var loaded = presets.LoadFromFile(preset);
        Check(loaded.GridLineWidth == 2.5 && loaded.LeftAlignSongs && loaded.FooterText == "Custom footer", "New design options round trip");
        File.Delete(preset);
        Render((FrameworkElement)content.Content, Path.Combine(outputFolder, "card-classic.png"), 680, 920);
        var theme = (ComboBox)window.FindName("ThemeBox"); theme.SelectedIndex = 1;
        typeof(CardDesignWindow).GetMethod("ApplyTheme_Click", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(window, [window, new RoutedEventArgs()]);
        Check(window.Settings.PageBackgroundColor == "#172033", "Midnight theme applied");
        Render((FrameworkElement)content.Content, Path.Combine(outputFolder, "card-midnight.png"), 680, 920);
        Render((FrameworkElement)window.Content, Path.Combine(outputFolder, "card-designer.png"), 1120, 760);
        var card = new BingoCard { CardNumber = 1, Squares = Enumerable.Range(1, 25).Select(i => new Track
            { Id = i, Title = i == 1 ? "A very long song title that must remain complete on the printed card" : $"Song {i:00}", Artist = "Example Artist" }).ToList() };
        var cards = Enumerable.Range(1, 60).Select(i => new BingoCard { CardNumber = i, Squares = card.Squares }).ToList();
        foreach (var perPage in new[] { 1, 2, 4 })
        foreach (var landscape in new[] { false, true })
        foreach (var letter in new[] { false, true })
        foreach (var ink in new[] { false, true })
        {
            var options = new PrintOptions { FirstCard = 3, LastCard = 8, CardsPerPage = perPage, Landscape = landscape, LetterPaper = letter, InkSaver = ink };
            var doc = PrintService.BuildDocument(cards, window.Settings, options, "ABCDEF123456");
            Check(doc.Pages.Count == (int)Math.Ceiling(6d / perPage), "Correct ranged print pagination");
            var page = doc.DocumentPaginator.GetPage(0);
            Check(Math.Abs(page.Size.Width - options.PageSize.Width) < .01 && Math.Abs(page.Size.Height - options.PageSize.Height) < .01, "Paper dimensions and orientation");
            var labels = Descendants(page.Visual).OfType<OutlinedTextBlock>().Select(t => t.Text).ToArray();
            Check(labels.Any(t => t.Contains("CARD 003")) && !labels.Any(t => t.Contains("CARD 001")), "Exact selected card range");
            if (perPage == 4 && !landscape && !letter && !ink)
                Render((FrameworkElement)page.Visual, Path.Combine(outputFolder, "print-four-per-page.png"), options.PageSize.Width, options.PageSize.Height);
        }
        try { new PrintOptions { FirstCard = 20, LastCard = 1 }.Validate(); throw new Exception("Invalid range accepted"); }
        catch (ArgumentException) { count++; }
        try { new PrintOptions { MarginMm = double.NaN }.Validate(); throw new Exception("Invalid margin accepted"); }
        catch (ArgumentException) { count++; }
        var printing = new PrintCardsWindow(cards, settings, "ABCDEF123456");
        printing.ShowActivated = false; printing.ShowInTaskbar = false;
        printing.WindowStartupLocation = WindowStartupLocation.Manual;
        printing.Left = -10000; printing.Top = -10000;
        printing.Show();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        var viewer = (DocumentViewer)printing.FindName("Preview");
        Check(viewer.PageViews.Count > 0, "Print preview realizes pages after loading");
        Render((FrameworkElement)printing.Content, Path.Combine(outputFolder, "print-preview.png"), 1100, 800);
        var db = new HazzMusicBingo.Data.AppDatabase();
        db.InitializeAsync().GetAwaiter().GetResult();
        var health = new MusicHealthWindow(db, null);
        var history = new GameHistoryWindow(db);
        var winner = new WinnerWindow(db, 1, "ABCDEF123456");
        var main = new MainWindow();
        foreach (var view in new Window[] { health, history, winner, main })
        {
            Layout((FrameworkElement)view.Content, view.Width, view.Height);
            Check(((FrameworkElement)view.Content).ActualWidth > 0, "Window layout loads");
        }
        Render((FrameworkElement)main.Content, Path.Combine(outputFolder, "host-console.png"), 1380, 820);
        foreach (var view in new Window[] { window, printing, health, history, winner, main }) view.Close();
        app.Shutdown();
        return count;
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        yield return parent;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            foreach (var item in Descendants(VisualTreeHelper.GetChild(parent, i))) yield return item;
    }
    private static void Layout(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
    }
    private static void Render(FrameworkElement element, string path, double width, double height)
    {
        Layout(element, width, height);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
