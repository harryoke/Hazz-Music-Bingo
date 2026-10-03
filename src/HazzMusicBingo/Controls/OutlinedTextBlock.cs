using System.Globalization;
using System.Windows;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;
using WpfFontWeight = System.Windows.FontWeight;
using WpfTextWrapping = System.Windows.TextWrapping;
using WpfTextAlignment = System.Windows.TextAlignment;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace HazzMusicBingo.Controls;

/// <summary>
/// WPF text element that renders a real vector outline/stroke around glyphs.
/// </summary>
public sealed class OutlinedTextBlock : FrameworkElement
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                "",
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontFamilyProperty =
        DependencyProperty.Register(
            nameof(FontFamily),
            typeof(WpfFontFamily),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                new WpfFontFamily("Arial"),
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeProperty =
        DependencyProperty.Register(
            nameof(FontSize),
            typeof(double),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                16.0,
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register(
        nameof(FontStyle), typeof(System.Windows.FontStyle), typeof(OutlinedTextBlock),
        new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
    public System.Windows.FontStyle FontStyle
    {
        get => (System.Windows.FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public static readonly DependencyProperty FontWeightProperty =
        DependencyProperty.Register(
            nameof(FontWeight),
            typeof(WpfFontWeight),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                FontWeights.Normal,
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(
            nameof(Foreground),
            typeof(WpfBrush),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                WpfBrushes.White,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(
            nameof(Stroke),
            typeof(WpfBrush),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                WpfBrushes.Black,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                0.0,
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextAlignmentProperty =
        DependencyProperty.Register(
            nameof(TextAlignment),
            typeof(WpfTextAlignment),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                WpfTextAlignment.Left,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register(
            nameof(TextWrapping),
            typeof(WpfTextWrapping),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                WpfTextWrapping.NoWrap,
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaxLinesProperty =
        DependencyProperty.Register(
            nameof(MaxLines),
            typeof(int),
            typeof(OutlinedTextBlock),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public WpfFontFamily FontFamily
    {
        get => (WpfFontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public WpfFontWeight FontWeight
    {
        get => (WpfFontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public WpfBrush Foreground
    {
        get => (WpfBrush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public WpfBrush Stroke
    {
        get => (WpfBrush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public WpfTextAlignment TextAlignment
    {
        get => (WpfTextAlignment)GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public WpfTextWrapping TextWrapping
    {
        get => (WpfTextWrapping)GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    /// <summary>0 means unlimited.</summary>
    public int MaxLines
    {
        get => (int)GetValue(MaxLinesProperty);
        set => SetValue(MaxLinesProperty, value);
    }

    protected override WpfSize MeasureOverride(WpfSize availableSize)
    {
        var maxWidth = double.IsInfinity(availableSize.Width)
            ? 100000
            : Math.Max(0, availableSize.Width);

        var ft = CreateFormattedText(maxWidth);

        var width = Math.Min(
            maxWidth,
            Math.Ceiling(
                ft.WidthIncludingTrailingWhitespace +
                StrokeThickness * 2));

        var height = Math.Ceiling(
            ft.Height + StrokeThickness * 2);

        if (!double.IsInfinity(availableSize.Height))
            height = Math.Min(height, availableSize.Height);

        return new WpfSize(width, height);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = Math.Max(
            1,
            ActualWidth - StrokeThickness * 2);

        var ft = CreateFormattedText(width);

        var geometry = ft.BuildGeometry(
            new WpfPoint(
                StrokeThickness,
                StrokeThickness));

        WpfPen? pen = null;

        if (StrokeThickness > 0 && Stroke is not null)
        {
            pen = new WpfPen(
                Stroke,
                StrokeThickness * 2)
            {
                LineJoin = PenLineJoin.Round
            };

            pen.Freeze();
        }

        // Draw the outline behind the fill so thick outlines do not swallow thin glyphs.
        if (pen is not null) drawingContext.DrawGeometry(null, pen, geometry);
        drawingContext.DrawGeometry(Foreground, null, geometry);
    }

    private FormattedText CreateFormattedText(double maxWidth)
    {
        var ft = new FormattedText(
            Text ?? "",
            CultureInfo.CurrentUICulture,
            WpfFlowDirection.LeftToRight,
            new Typeface(
                FontFamily,
                FontStyle,
                FontWeight,
                FontStretches.Normal),
            FontSize,
            Foreground,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            TextAlignment = TextAlignment,
            MaxTextWidth = Math.Max(1, maxWidth)
        };

        if (TextWrapping == WpfTextWrapping.NoWrap)
        {
            ft.Trimming = TextTrimming.CharacterEllipsis;
            ft.MaxLineCount = 1;
        }
        else if (MaxLines > 0)
        {
            ft.MaxLineCount = MaxLines;
            ft.Trimming = TextTrimming.CharacterEllipsis;
        }

        return ft;
    }
}
