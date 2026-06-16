using System.Windows.Media;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using MediaColor = System.Windows.Media.Color;

namespace AllO.Services;

/// <summary>
/// Global Color Coder settings and per-document colors (keyed by document title).
/// </summary>
public static class ColorCoderState
{
    public static readonly MediaColor[] DefaultPalette =
    {
        MediaColor.FromRgb(0x6C, 0x63, 0xFF),
        MediaColor.FromRgb(0x34, 0xD3, 0x99),
        MediaColor.FromRgb(0xFB, 0x71, 0x85),
        MediaColor.FromRgb(0xFB, 0xBF, 0x24),
        MediaColor.FromRgb(0x38, 0xBD, 0xF8),
        MediaColor.FromRgb(0xF4, 0x72, 0xB6),
        MediaColor.FromRgb(0xA7, 0x8B, 0xFA),
        MediaColor.FromRgb(0x2D, 0xD4, 0xBF),
        MediaColor.FromRgb(0xFF, 0x84, 0x4B),
        MediaColor.FromRgb(0x22, 0xD3, 0xEE),
    };

    public static bool IsActive { get; set; }

    public static ColorCoderDisplayMode DisplayMode { get; set; } = ColorCoderDisplayMode.Tabs;

    public static double Opacity { get; set; } = 0.88;

    public static double BarThicknessDip { get; set; } = 5.0;

    /// <summary>Document title → color (case-insensitive).</summary>
    public static Dictionary<string, MediaColor> DocumentColors { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static MediaColor GetColorForDocument(string title)
    {
        string key = string.IsNullOrEmpty(title) ? "Untitled" : title;
        if (DocumentColors.TryGetValue(key, out var c))
            return c;
        return DefaultPalette[0];
    }

    public static void AssignDefaultColors(Application app)
    {
        DocumentColors.Clear();
        int i = 0;
        foreach (Document doc in app.Documents)
        {
            if (doc.IsFamilyDocument) continue;
            string title = string.IsNullOrEmpty(doc.Title) ? "Untitled" : doc.Title;
            DocumentColors[title] = DefaultPalette[i % DefaultPalette.Length];
            i++;
        }
    }

    public static void DeactivateAndClear()
    {
        IsActive = false;
        DocumentColors.Clear();
    }
}
