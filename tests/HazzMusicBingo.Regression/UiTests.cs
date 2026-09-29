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
        Check(((System.Windows.Controls.Primitives.UniformGrid)main.FindName("GameButtonsPanel")).Children.Count == 8, "Eight game buttons are present in the header");
        Check(main.Icon is not null, "Window loads branded icon");
        var shortcutEditor = new GameShortcutWindow(new GameShortcut { Label = "1960s", Color = "#FFAA00", GameFile = "fixture.hmbgame",
            Audience = new AudienceDesignSettings { FontFamilyName = "Georgia" } }, new AudienceDesignSettings());
        Check(shortcutEditor.ButtonLabel == "1960s" && shortcutEditor.Audience.FontFamilyName == "Georgia", "Shortcut editor loads saved label and theme");
        Render((FrameworkElement)shortcutEditor.Content, Path.Combine(outputFolder, "game-button-editor.png"), 620, 470);
        shortcutEditor.Close();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var demoButtons = new[] { "1960s", "1970s", "1980s", "1990s", "Rock", "Disco", "Christmas", "Party mix" }
            .Select((label, index) => (GameShortcut?)new GameShortcut { Label = label,
                Color = new[] { "#F59E0B", "#9333EA", "#DB2777", "#2563EB", "#DC2626", "#0891B2", "#15803D", "#D97706" }[index] }).ToList();
        typeof(MainWindow).GetField("_gameButtons", flags)!.SetValue(main, demoButtons);
        typeof(MainWindow).GetMethod("RenderGameButtons", flags)!.Invoke(main, null);
        ((FrameworkElement)main.FindName("GameButtonsPanel")).IsEnabled = true;
        for (var i = 1; i <= 60; i++)
            db.UpsertTrackAsync(Path.Combine(outputFolder, $"ui-song-{i}.wav"), $"Song {i}", "Artist", 30, 0).GetAwaiter().GetResult();
        var liveGame = db.CreateGameAsync(db.GetRandomTracksAsync(60).GetAwaiter().GetResult()).GetAwaiter().GetResult();
        new CardGenerator(db).EnsureStrictCardsExistAsync(liveGame).GetAwaiter().GetResult();
        var liveCards = db.GetCardsAsync(liveGame, 60).GetAwaiter().GetResult();
        db.SaveWinningSettingsAsync(liveGame, new WinningSettings { FirstCard = 1, LastCard = 42 }).GetAwaiter().GetResult();
        foreach (var track in liveCards[0].Squares.Take(5)) db.MarkTrackPlayedAsync(liveGame, track.Id).GetAwaiter().GetResult();
        typeof(MainWindow).GetField("_gameId", flags)!.SetValue(main, (long?)liveGame);
        typeof(MainWindow).GetMethod("EnableGameControls", flags)!.Invoke(main, [true]);
        ((Task)typeof(MainWindow).GetMethod("RefreshWinningAsync", flags)!.Invoke(main, null)!).GetAwaiter().GetResult();
        Check(((TextBlock)main.FindName("WinnerStatusText")).Text.Contains("LINE WON"), "Host displays live winner");
        ((Button)main.FindName("AcknowledgeButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(!((Button)main.FindName("AcknowledgeButton")).IsEnabled, "Acknowledge button suppresses repeat alert");
        ((Button)main.FindName("CornersRuleButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(db.GetWinningSettingsAsync(liveGame).GetAwaiter().GetResult().Pattern == WinningPattern.FourCorners
            && db.GetPlayedCountAsync(liveGame).GetAwaiter().GetResult() == 5, "Host rule button changes rule without resetting songs");
        var navigate = new WinnerWindow(db, liveGame, "ABCDEF123456");
        typeof(WinnerWindow).GetMethod("Check_Click", flags)!.Invoke(navigate, [navigate, new RoutedEventArgs()]);
        Check(!((Button)navigate.FindName("PreviousButton")).IsEnabled, "Previous disabled at first card");
        ((Button)navigate.FindName("NextButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(((TextBox)navigate.FindName("CardBox")).Text == "2" && ((TextBlock)navigate.FindName("ResultText")).Text.Contains("CARD 002"), "Next button redraws saved card");
        ((Button)navigate.FindName("PreviousButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(((TextBox)navigate.FindName("CardBox")).Text == "1", "Previous button navigates back");
        var audience = new AudienceWindow(new AudienceDesignSettings());
        audience.ShowWinningMessage("LINE WON!");
        audience.ApplyDesign(new AudienceDesignSettings { FontFamilyName = "Georgia", TitleColor = "#FFAA00" });
        Check(((TextBlock)audience.FindName("WinningMessage")).FontFamily.Source == "Georgia", "Audience theme includes winning banner font");
        var messageDesign = new AudienceDesignSettings { FontFamilyName = "Arial", WinningFontFamilyName = "Georgia", WinningTextColor = "#00FFAA", WinningFontSize = 46, WinningBold = false, WinningItalic = true };
        var messageEditor = new AudienceDesignWindow(messageDesign);
        ((ComboBox)messageEditor.FindName("WinningFontBox")).SelectedItem = "Times New Roman";
        Check(messageEditor.Settings.WinningFontFamilyName == "Times New Roman" && messageEditor.Settings.FontFamilyName == "Arial", "Message font changes immediately and independently");
        ((TextBox)messageEditor.FindName("WinningSizeBox")).Text = "52";
        Check(((TextBlock)messageEditor.FindName("PreviewWinning")).FontSize == 52, "Message size updates live preview");
        var messagePath = Path.Combine(outputFolder, "message-theme.hmbaudience");
        var audienceFiles = new AudienceDesignSettingsService();
        audienceFiles.SaveToFile(messageEditor.Settings.Clone(), messagePath);
        var savedMessage = audienceFiles.LoadFromFile(messagePath);
        audience.ApplyDesign(savedMessage);
        var banner = (TextBlock)audience.FindName("WinningMessage");
        Check(banner.FontSize == 52 && banner.FontStyle == FontStyles.Italic && banner.FontWeight == FontWeights.Normal
            && ((SolidColorBrush)banner.Foreground).Color == Color.FromRgb(0, 255, 170), "Saved message style applies to audience display");
        Check(messageDesign.WinningFontSize == 46, "Editing isolates original theme");
        Render((FrameworkElement)messageEditor.Content, Path.Combine(outputFolder, "audience-message-design.png"), 1120, 760);
        messageEditor.Close(); File.Delete(messagePath);
        audience.ShowTrack(liveCards[0].Squares[0]);
        Check(((TextBlock)audience.FindName("WinningMessage")).Text == "LINE WON!", "Track display preserves rule banner");
        audience.ShowPlayedSongs(liveCards[0].Squares.Take(5).ToList(), 0);
        Check(((TextBlock)audience.FindName("WinningMessage")).Text == "LINE WON!", "Played list preserves rule banner");
        Render((FrameworkElement)audience.Content, Path.Combine(outputFolder, "audience-winner.png"), 1280, 720);
        Render((FrameworkElement)navigate.Content, Path.Combine(outputFolder, "winner-navigation.png"), 1050, 780);
        navigate.Close(); audience.Close();
        foreach (var view in new Window[] { health, history, winner, main })
        {
            Layout((FrameworkElement)view.Content, view.Width, view.Height);
            Check(((FrameworkElement)view.Content).ActualWidth > 0, "Window layout loads");
        }
        Render((FrameworkElement)main.Content, Path.Combine(outputFolder, "host-console.png"), 1380, 820);
        Render((FrameworkElement)main.Content, Path.Combine(outputFolder, "host-console-minimum.png"), 1120, 680);
        Check(((System.Windows.Controls.Primitives.UniformGrid)main.FindName("GameButtonsPanel")).Children.Cast<Button>()
            .All(b => b.ActualWidth >= 150 && b.ActualHeight >= 40), "All eight shortcuts fit at minimum window size");
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
