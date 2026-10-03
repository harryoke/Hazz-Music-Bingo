using HazzMusicBingo.Controls;
using HazzMusicBingo.Models;
using System.Windows;
using System.Windows.Media;

namespace HazzMusicBingo.Services;

public static class AudienceTextStyling
{
    public static void Apply(OutlinedTextBlock text, AudienceDesignSettings design, AudienceTextRole role)
    {
        var style = design.StyleFor(role);
        try { text.FontFamily = new System.Windows.Media.FontFamily(string.IsNullOrWhiteSpace(style.FontFamilyName) ? "Arial" : style.FontFamilyName); }
        catch { text.FontFamily = new System.Windows.Media.FontFamily("Arial"); }
        text.FontSize = double.IsFinite(style.Size) ? Math.Clamp(style.Size, 12, 140) : 34;
        text.Foreground = Brush(style.Color);
        text.FontWeight = style.Bold ? FontWeights.Bold : FontWeights.Normal;
        text.FontStyle = style.Italic ? FontStyles.Italic : FontStyles.Normal;
        text.Stroke = Brush(style.OutlineColor);
        text.StrokeThickness = style.Outline && double.IsFinite(style.OutlineWidth) ? Math.Clamp(style.OutlineWidth, 0, 12) : 0;
    }

    private static SolidColorBrush Brush(string color)
    {
        try { return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color)); }
        catch { return new SolidColorBrush(Colors.White); }
    }
}
