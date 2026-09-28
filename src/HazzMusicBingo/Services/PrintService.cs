using HazzMusicBingo.Models;
using HazzMusicBingo.Controls;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;
using PrintDialog = System.Windows.Controls.PrintDialog;
using Size = System.Windows.Size;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using Brushes = System.Windows.Media.Brushes;

namespace HazzMusicBingo.Services;

public sealed class PrintService
{
    public const double CardWidth = 680;
    public const double CardHeight = 920;

    public static FixedDocument BuildDocument(IReadOnlyList<BingoCard> cards, CardDesignSettings design,
        PrintOptions options, string sessionCode)
    {
        options.Validate();
        var (width, height) = options.PageSize;
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = new Size(width, height);
        var selected = cards.Where(c => c.CardNumber >= options.FirstCard && c.CardNumber <= options.LastCard)
            .OrderBy(c => c.CardNumber).ToList();
        var margin = options.MarginMm * 96 / 25.4;
        var columns = options.CardsPerPage == 4 || (options.CardsPerPage == 2 && options.Landscape) ? 2 : 1;
        var rows = options.CardsPerPage / columns;
        for (var start = 0; start < selected.Count; start += options.CardsPerPage)
        {
            var page = new FixedPage { Width = width, Height = height, Background = Brushes.White };
            var grid = new Grid { Width = width - 2 * margin, Height = height - 2 * margin };
            FixedPage.SetLeft(grid, margin);
            FixedPage.SetTop(grid, margin);
            for (var i = 0; i < columns; i++) grid.ColumnDefinitions.Add(new());
            for (var i = 0; i < rows; i++) grid.RowDefinitions.Add(new());
            for (var slot = 0; slot < options.CardsPerPage && start + slot < selected.Count; slot++)
            {
                var view = new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(options.CardsPerPage == 1 ? 0 : 6),
                    Child = CreateCardVisual(selected[start + slot], design, sessionCode, options.InkSaver)
                };
                Grid.SetColumn(view, slot % columns);
                Grid.SetRow(view, slot / columns);
                grid.Children.Add(view);
            }
            page.Children.Add(grid);
            var content = new PageContent();
            ((IAddChild)content).AddChild(page);
            document.Pages.Add(content);
        }
        return document;
    }

    public static FrameworkElement CreateCardVisual(BingoCard card, CardDesignSettings settings,
        string sessionCode = "PREVIEW", bool inkSaver = false)
    {
        if (card.Squares.Count != 25) throw new ArgumentException("A printed card needs 25 songs.");
        var design = settings.Clone();
        if (inkSaver)
        {
            design.PageBackgroundColor = design.CellBackgroundColor = "#FFFFFF";
            design.TextColor = design.TitleColor = design.GridLineColor = "#000000";
            design.BackgroundImagePath = "";
            design.AlternateRows = false;
            design.UseTextOutline = false;
        }
        var root = new Grid { Width = CardWidth, Height = CardHeight, Background = Brush(design.PageBackgroundColor) };
        if (!string.IsNullOrWhiteSpace(design.BackgroundImagePath) && File.Exists(design.BackgroundImagePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(design.BackgroundImagePath, UriKind.Absolute);
                bitmap.EndInit(); bitmap.Freeze();
                root.Children.Add(new Image { Source = bitmap, Stretch = Stretch.Uniform,
                    Opacity = Safe(design.BackgroundImageOpacity, 0, 1, .3) });
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException) { }
        }
        var body = new Grid { Margin = new Thickness(20) };
        body.RowDefinitions.Add(new() { Height = GridLength.Auto });
        body.RowDefinitions.Add(new() { Height = GridLength.Auto });
        body.RowDefinitions.Add(new() { Height = GridLength.Auto });
        body.RowDefinitions.Add(new() { Height = GridLength.Auto });
        body.RowDefinitions.Add(new());
        body.RowDefinitions.Add(new() { Height = GridLength.Auto });
        root.Children.Add(body);
        FontFamily font;
        try { font = new FontFamily(string.IsNullOrWhiteSpace(design.FontFamilyName) ? "Arial" : design.FontFamilyName); }
        catch { font = new FontFamily("Arial"); }
        OutlinedTextBlock Text(string value, double size, string color, bool bold = false) => new()
        {
            Text = value, FontFamily = font, FontSize = size, FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Brush(color), Stroke = Brush(design.TextOutlineColor),
            StrokeThickness = design.UseTextOutline ? Safe(design.TextOutlineWidth, 0, 12, 2) : 0,
            TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap
        };
        void Add(FrameworkElement element, int row) { Grid.SetRow(element, row); body.Children.Add(element); }
        var identity = Text($"GAME {sessionCode}    •    CARD {card.CardNumber:000}", 13, design.TextColor, true);
        identity.Margin = new Thickness(0, 0, 0, 10);
        Add(identity, 0);
        var title = Text(string.IsNullOrWhiteSpace(design.Title) ? "MUSIC BINGO" : design.Title,
            Safe(design.TitleFontSize, 12, 72, 34), design.TitleColor, true);
        title.MaxLines = 2;
        Add(title, 1);
        var subtitle = Text(design.HeaderText, Safe(design.HeaderFontSize, 10, 48, 18), design.TextColor);
        subtitle.Margin = new Thickness(0, 5, 0, 12);
        subtitle.MaxLines = 2;
        subtitle.Visibility = string.IsNullOrWhiteSpace(design.HeaderText) ? Visibility.Collapsed : Visibility.Visible;
        Add(subtitle, 2);
        var letters = new System.Windows.Controls.Primitives.UniformGrid { Columns = 5, Margin = new Thickness(0, 12, 0, 8) };
        foreach (var letter in "BINGO") letters.Children.Add(Text(letter.ToString(), 26, design.TitleColor, true));
        letters.Visibility = design.ShowBingoLetters ? Visibility.Visible : Visibility.Collapsed;
        Add(letters, 3);
        var squares = new System.Windows.Controls.Primitives.UniformGrid { Rows = 5, Columns = 5 };
        for (var i = 0; i < 25; i++)
        {
            var track = card.Squares[i];
            var value = design.ShowArtist && !string.IsNullOrWhiteSpace(track.Artist) ? $"{track.Title}\n— {track.Artist}" : track.Title;
            var text = Text(value, Safe(design.CellFontSize, 8, 32, 16), design.TextColor, design.BoldSongText);
            text.Width = 108;
            // Unlimited wrapping plus proportional scaling preserves the complete song label.
            text.TextAlignment = design.LeftAlignSongs ? TextAlignment.Left : TextAlignment.Center;
            var fill = Brush(design.AlternateRows && i / 5 % 2 == 1 ? design.AlternateRowColor : design.CellBackgroundColor);
            fill.Opacity = Safe(design.CellBackgroundOpacity, 0, 1, .9);
            squares.Children.Add(new Border
            {
                Background = fill, BorderBrush = Brush(design.GridLineColor),
                BorderThickness = new Thickness(Safe(design.GridLineWidth, .25, 4, 1) / 2),
                Padding = new Thickness(Safe(design.CellPadding, 2, 16, 8)),
                Child = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly,
                    HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Child = text }
            });
        }
        Add(squares, 4);
        var footer = Text(design.FooterText, 12, design.TextColor);
        footer.MaxLines = 2; footer.Margin = new Thickness(0, 12, 0, 0);
        Add(footer, 5);
        return root;
    }

    public static void Print(FixedDocument document, PrintOptions options)
    {
        var dialog = new PrintDialog { UserPageRangeEnabled = true, MinPage = 1, MaxPage = (uint)document.Pages.Count };
        dialog.PrintTicket ??= new PrintTicket();
        dialog.PrintTicket.PageOrientation = options.Landscape ? PageOrientation.Landscape : PageOrientation.Portrait;
        dialog.PrintTicket.PageMediaSize = new PageMediaSize(options.LetterPaper ? PageMediaSizeName.NorthAmericaLetter : PageMediaSizeName.ISOA4);
        if (dialog.ShowDialog() != true) return;
        var first = dialog.PageRangeSelection == PageRangeSelection.UserPages ? dialog.PageRange.PageFrom - 1 : 0;
        var last = dialog.PageRangeSelection == PageRangeSelection.UserPages ? dialog.PageRange.PageTo - 1 : document.Pages.Count - 1;
        var capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
        var area = capabilities.PageImageableArea;
        var size = new Size(capabilities.OrientedPageMediaWidth ?? options.PageSize.Width,
            capabilities.OrientedPageMediaHeight ?? options.PageSize.Height);
        var printable = area is null ? new Rect(0, 0, dialog.PrintableAreaWidth, dialog.PrintableAreaHeight)
            : new Rect(area.OriginWidth, area.OriginHeight, area.ExtentWidth, area.ExtentHeight);
        dialog.PrintDocument(new FittedPaginator(document.DocumentPaginator, first, last, size, printable), "Hazz Music Bingo Cards");
    }

    private sealed class FittedPaginator(DocumentPaginator source, int first, int last, Size size, Rect area) : DocumentPaginator
    {
        private readonly int _first = Math.Clamp(first, 0, source.PageCount - 1);
        private readonly int _last = Math.Clamp(last, Math.Clamp(first, 0, source.PageCount - 1), source.PageCount - 1);
        public override bool IsPageCountValid => true;
        public override int PageCount => _last - _first + 1;
        public override Size PageSize { get => size; set { } }
        public override IDocumentPaginatorSource Source => source.Source;
        public override DocumentPage GetPage(int number)
        {
            if (number < 0 || number >= PageCount) return DocumentPage.Missing;
            var page = source.GetPage(number + _first);
            var scale = Math.Min(area.Width / page.Size.Width, area.Height / page.Size.Height);
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var target = new Rect(area.X + (area.Width - page.Size.Width * scale) / 2,
                    area.Y + (area.Height - page.Size.Height * scale) / 2, page.Size.Width * scale, page.Size.Height * scale);
                context.DrawRectangle(new VisualBrush(page.Visual) { Stretch = Stretch.Fill }, null, target);
            }
            return new DocumentPage(visual, size, new Rect(size), area);
        }
    }
    private static double Safe(double value, double min, double max, double fallback) => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    private static SolidColorBrush Brush(string color)
    {
        try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); }
        catch { return new SolidColorBrush(Colors.Black); }
    }
}
