using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace AllO.Helpers;

/// <summary>
/// Helpers para construir botones de ribbon con icono Segoe MDL2 y tooltip con imagen.
/// Antes el código de iconos vivía en App.cs duplicado por versión de Revit.
/// </summary>
public static class RibbonBuilder
{
    /// <summary>Path al ensamblado Shared, calculado relativo al ensamblado de la versión.</summary>
    public static string SharedAssemblyPath()
    {
        var thisDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(thisDir, "AllO.Shared.dll");
    }

    public static PushButtonData Button(string name, string text, string commandClass)
        => new(name, text, SharedAssemblyPath(), commandClass);

    /// <summary>
    /// Configura icono pequeño + grande + tooltip largo + imagen de tooltip.
    /// Si <paramref name="tooltipImageGlyph"/> es null se reusa el glyph principal.
    /// </summary>
    public static void Configure(
        PushButton button,
        string glyph,
        string tooltip,
        string longDescription,
        Color? bg = null,
        Color? fg = null,
        string? tooltipImageGlyph = null)
    {
        var background = bg ?? Color.FromRgb(44, 44, 44);
        var foreground = fg ?? Color.FromRgb(224, 224, 224);

        button.ToolTip = tooltip;
        button.LongDescription = longDescription;
        button.Image      = CreateGlyphIcon(glyph, 16, background, foreground);
        button.LargeImage = CreateGlyphIcon(glyph, 32, background, foreground);
        button.ToolTipImage = CreateGlyphIcon(tooltipImageGlyph ?? glyph, 96, background, foreground);
    }

    public static void Configure(PulldownButton button, string glyph, string tooltip)
    {
        button.ToolTip = tooltip;
        button.Image      = CreateGlyphIcon(glyph, 16, Color.FromRgb(44,44,44), Color.FromRgb(224,224,224));
        button.LargeImage = CreateGlyphIcon(glyph, 32, Color.FromRgb(44,44,44), Color.FromRgb(224,224,224));
    }

    public static BitmapSource CreateGlyphIcon(string glyph, int size, Color bg, Color fg)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(bg), null, new Rect(0, 0, size, size));
            var ft = new FormattedText(
                glyph,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe MDL2 Assets"),
                size * 0.55,
                new SolidColorBrush(fg),
                1.0);
            dc.DrawText(ft, new Point((size - ft.Width) / 2, (size - ft.Height) / 2));
        }
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        return bmp;
    }
}
