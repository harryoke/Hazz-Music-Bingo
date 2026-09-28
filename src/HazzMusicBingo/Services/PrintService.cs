using HazzMusicBingo.Models;
using HazzMusicBingo.Controls;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using WpfImage = System.Windows.Controls.Image;
using PrintDialog = System.Windows.Controls.PrintDialog;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfColor = System.Windows.Media.Color;
using WpfFontFamily = System.Windows.Media.FontFamily;
using System.Windows.Media.Imaging;
using WpfSize = System.Windows.Size;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfVerticalAlignment = System.Windows.VerticalAlignment;

namespace HazzMusicBingo.Services;

public sealed class PrintService
{
    public void PrintCards(
        IReadOnlyList<BingoCard> cards,
        CardDesignSettings design)
    {
        if (cards.Count == 0)
            return;

        var dialog = new PrintDialog();

        dialog.PrintTicket ??= new PrintTicket();
        dialog.PrintTicket.PageOrientation = PageOrientation.Portrait;

        try
        {
            dialog.PrintTicket.PageMediaSize =
                new PageMediaSize(PageMediaSizeName.ISOA4);
        }
        catch
        {
            // Some printer drivers do not expose ISO A4 through PrintTicket.
            // The user's selected printer/page size will still be respected.
        }

        if (dialog.ShowDialog() != true)
            return;

        // Always build the logical card as portrait, even if a printer driver
        // reports its printable dimensions in the opposite order.
        var pageWidth = Math.Min(
            dialog.PrintableAreaWidth,
            dialog.PrintableAreaHeight);
        var pageHeight = Math.Max(
            dialog.PrintableAreaWidth,
            dialog.PrintableAreaHeight);

        var document = BuildDocument(
            cards,
            pageWidth,
            pageHeight,
            design);

        dialog.PrintDocument(
            document.DocumentPaginator,
            "Hazz Music Bingo Cards");
    }

    private static FixedDocument BuildDocument(
        IReadOnlyList<BingoCard> cards,
        double pageWidth,
        double pageHeight,
        CardDesignSettings design)
    {
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize =
            new WpfSize(pageWidth, pageHeight);

        foreach (var card in cards)
        {
            var page = new FixedPage
            {
                Width = pageWidth,
                Height = pageHeight,
                Background = BrushFromHex(design.PageBackgroundColor)
            };

            var root = new Grid
            {
                Width = pageWidth,
                Height = pageHeight
            };

            root.Children.Add(new Border
            {
                Background = BrushFromHex(design.PageBackgroundColor)
            });

            AddBackgroundImage(root, design);

            var cardVisual = CreateCardVisual(card, design);
            cardVisual.Margin = new Thickness(10);
            root.Children.Add(cardVisual);

            page.Children.Add(root);

            var content = new PageContent();
            ((IAddChild)content).AddChild(page);
            document.Pages.Add(content);
        }

        return document;
    }

    private static void AddBackgroundImage(
        Grid root,
        CardDesignSettings design)
    {
        if (string.IsNullOrWhiteSpace(design.BackgroundImagePath)
            || !File.Exists(design.BackgroundImagePath))
            return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource =
                new Uri(design.BackgroundImagePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            root.Children.Add(new WpfImage
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                Opacity = Math.Clamp(
                    design.BackgroundImageOpacity,
                    0.0,
                    1.0)
            });
        }
        catch
        {
            // If an image becomes unavailable or corrupt, printing continues
            // with the selected solid page background.
        }
    }

    private static FrameworkElement CreateCardVisual(
        BingoCard card,
        CardDesignSettings design)
    {
        var font = SafeFont(design.FontFamilyName);

        var outer = new Grid();
        outer.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });
        outer.RowDefinitions.Add(new RowDefinition());

        var top = new Grid
        {
            Margin = new Thickness(6, 3, 6, 3)
        };

        var title = new OutlinedTextBlock
        {
            Text = string.IsNullOrWhiteSpace(design.Title)
                ? "MUSIC BINGO"
                : design.Title,
            FontFamily = font,
            FontSize = Math.Clamp(design.TitleFontSize, 12, 72),
            FontWeight = FontWeights.Bold,
            Foreground = BrushFromHex(design.TitleColor),
            Stroke = BrushFromHex(design.TextOutlineColor),
            StrokeThickness =
                design.UseTextOutline ? design.TextOutlineWidth : 0,
            HorizontalAlignment = WpfHorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        top.Children.Add(title);

        top.Children.Add(new OutlinedTextBlock
        {
            Text = $"CARD {card.CardNumber:000}",
            FontFamily = font,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushFromHex(design.TextColor),
            Stroke = BrushFromHex(design.TextOutlineColor),
            StrokeThickness =
                design.UseTextOutline ? design.TextOutlineWidth : 0,
            HorizontalAlignment = WpfHorizontalAlignment.Right,
            VerticalAlignment = WpfVerticalAlignment.Top,
            Margin = new Thickness(4)
        });

        Grid.SetRow(top, 0);
        outer.Children.Add(top);

        var header = new OutlinedTextBlock
        {
            Text = design.HeaderText,
            FontFamily = font,
            FontSize = Math.Clamp(design.HeaderFontSize, 10, 48),
            FontWeight = FontWeights.SemiBold,
            Foreground = BrushFromHex(design.TextColor),
            Stroke = BrushFromHex(design.TextOutlineColor),
            StrokeThickness =
                design.UseTextOutline ? design.TextOutlineWidth : 0,
            HorizontalAlignment = WpfHorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 2, 8, 6),
            Visibility = string.IsNullOrWhiteSpace(design.HeaderText)
                ? Visibility.Collapsed
                : Visibility.Visible
        };

        Grid.SetRow(header, 1);
        outer.Children.Add(header);

        var bingo = new Grid
        {
            Margin = new Thickness(3, 0, 3, 5),
            Visibility = design.ShowBingoLetters
                ? Visibility.Visible
                : Visibility.Collapsed
        };

        for (var c = 0; c < 5; c++)
            bingo.ColumnDefinitions.Add(new ColumnDefinition());

        var letters = new[] { "B", "I", "N", "G", "O" };
        for (var c = 0; c < 5; c++)
        {
            var tb = new OutlinedTextBlock
            {
                Text = letters[c],
                FontFamily = font,
                FontSize = Math.Max(16, design.HeaderFontSize),
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex(design.TitleColor),
                Stroke = BrushFromHex(design.TextOutlineColor),
                StrokeThickness =
                    design.UseTextOutline ? design.TextOutlineWidth : 0,
                HorizontalAlignment = WpfHorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(tb, c);
            bingo.Children.Add(tb);
        }

        Grid.SetRow(bingo, 2);
        outer.Children.Add(bingo);

        var grid = new Grid();
        for (var i = 0; i < 5; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        for (var i = 0; i < 25; i++)
        {
            var track = card.Squares[i];

            var songText = design.ShowArtist
                && !string.IsNullOrWhiteSpace(track.Artist)
                ? $"{track.Title}\n— {track.Artist}"
                : track.Title;

            var cellBackground =
                BrushFromHex(design.CellBackgroundColor);
            cellBackground.Opacity =
                Math.Clamp(design.CellBackgroundOpacity, 0, 1);

            var cell = new Border
            {
                Background = cellBackground,
                BorderBrush = BrushFromHex(design.GridLineColor),
                BorderThickness = new Thickness(0.8),
                Padding = new Thickness(6),
                Child = new Viewbox
                {
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.DownOnly,
                    Child = new OutlinedTextBlock
                    {
                        Text = songText,
                        FontFamily = font,
                        FontSize = Math.Clamp(
                            design.CellFontSize,
                            8,
                            32),
                        FontWeight = design.BoldSongText
                            ? FontWeights.Bold
                            : FontWeights.Normal,
                        Foreground = BrushFromHex(design.TextColor),
                        Stroke = BrushFromHex(design.TextOutlineColor),
                        StrokeThickness =
                            design.UseTextOutline ? design.TextOutlineWidth : 0,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        MaxLines = 4,
                        MaxWidth = 150,
                        HorizontalAlignment =
                            WpfHorizontalAlignment.Center,
                        VerticalAlignment =
                            WpfVerticalAlignment.Center
                    }
                }
            };

            Grid.SetRow(cell, i / 5);
            Grid.SetColumn(cell, i % 5);
            grid.Children.Add(cell);
        }

        Grid.SetRow(grid, 3);
        outer.Children.Add(grid);

        return outer;
    }

    private static WpfFontFamily SafeFont(string familyName)
    {
        try
        {
            return new WpfFontFamily(
                string.IsNullOrWhiteSpace(familyName)
                    ? "Arial"
                    : familyName);
        }
        catch
        {
            return new WpfFontFamily("Arial");
        }
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try
        {
            var color =
                (WpfColor)WpfColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.Black);
        }
    }
}
