using Autodesk.Revit.UI;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows;

namespace AllO.UI;

public static class RibbonBuilder
{
    public static PushButtonData Button(string id, string label, string assemblyPath, string className)
    {
        return new PushButtonData(id, label, assemblyPath, className);
    }

    public static void Configure(PushButton btn, string glyph, string tooltip, string longDescription)
    {
        btn.ToolTip = tooltip;
        btn.LongDescription = longDescription;
        btn.Image = CreateGlyphBitmap(glyph, 16);
        btn.LargeImage = CreateGlyphBitmap(glyph, 32);
    }

    private static BitmapSource CreateGlyphBitmap(string glyph, int size)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(44, 44, 44)), null, new Rect(0, 0, size, size));
            var ft = new FormattedText(glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe MDL2 Assets"), size * 0.55, Brushes.White, 1.0);
            dc.DrawText(ft, new Point((size - ft.Width) / 2, (size - ft.Height) / 2));
        }
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        return bmp;
    }
}
