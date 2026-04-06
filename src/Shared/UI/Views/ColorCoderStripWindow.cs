using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AllO.Services;
using MediaColor = System.Windows.Media.Color;

namespace AllO.UI.Views;

/// <summary>
/// Transparent, click-through overlay. Top/bottom modes use a soft &quot;light wash&quot; (wide gradient + highlight)
/// instead of a flat bar. Border mode keeps a crisp outline.
/// </summary>
public sealed class ColorCoderStripWindow : Window
{
    private readonly Rectangle _screenRect;
    private readonly ColorCoderDisplayMode _mode;
    private readonly double _thicknessDip;

    public ColorCoderStripWindow(Rectangle screenRect, MediaColor color, ColorCoderDisplayMode mode, double opacity, double thicknessDip)
    {
        _screenRect = screenRect;
        _mode = mode;
        _thicknessDip = Math.Max(2, thicknessDip);

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;

        Content = BuildContent(color, mode, opacity, _thicknessDip);

        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) => ApplyGeometry();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            ApplyGeometry();
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
                MakeClickThrough(hwnd);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>Vertical extent of the glow in DIPs (thicker slider = light reaches farther into the canvas).</summary>
    private double GlowHeightDip(double viewH)
    {
        // Min ~26 DIP wash; scale with thickness; cap so we never exceed the view height.
        double h = Math.Max(26, _thicknessDip * 4.8);
        h = Math.Min(h, 140);
        return Math.Min(h, viewH);
    }

    /// <summary>Maps Revit screen rectangle to WPF logical coords and sizes the window (full view or glow band).</summary>
    private void ApplyGeometry()
    {
        var r = _screenRect;
        if (r.Width <= 0 || r.Height <= 0) return;

        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        var source = HwndSource.FromHwnd(hwnd);
        if (source?.CompositionTarget == null)
        {
            Left = r.Left;
            Top = r.Top;
            Width = r.Width;
            Height = r.Height;
            return;
        }

        var t = source.CompositionTarget.TransformFromDevice;
        var tl = t.Transform(new System.Windows.Point(r.Left, r.Top));
        var br = t.Transform(new System.Windows.Point(r.Right, r.Bottom));
        double w = Math.Max(1, br.X - tl.X);
        double viewH = Math.Max(1, br.Y - tl.Y);
        double glowH = GlowHeightDip(viewH);

        switch (_mode)
        {
            case ColorCoderDisplayMode.FillBar:
                Left = tl.X;
                Top = tl.Y;
                Width = w;
                Height = glowH;
                break;
            case ColorCoderDisplayMode.BottomLine:
                Left = tl.X;
                Top = br.Y - glowH;
                Width = w;
                Height = glowH;
                break;
            default:
                Left = tl.X;
                Top = tl.Y;
                Width = w;
                Height = viewH;
                break;
        }
    }

    private static UIElement BuildContent(MediaColor color, ColorCoderDisplayMode mode, double opacity, double thicknessDip)
    {
        switch (mode)
        {
            case ColorCoderDisplayMode.FillBar:
            case ColorCoderDisplayMode.BottomLine:
                return new Border
                {
                    Background = CreateGlowBrush(color, opacity, mode),
                    SnapsToDevicePixels = true
                };
            case ColorCoderDisplayMode.Border:
            {
                var brush = new SolidColorBrush(color) { Opacity = opacity };
                return new Border
                {
                    BorderBrush = brush,
                    BorderThickness = new Thickness(Math.Max(1, thicknessDip * 0.35)),
                    Background = System.Windows.Media.Brushes.Transparent,
                    SnapsToDevicePixels = true
                };
            }
            default:
                return new Grid();
        }
    }

    /// <summary>Soft light falloff: brighter, slightly desaturated &quot;hot&quot; edge against the view chrome, then diffuse fade.</summary>
    private static System.Windows.Media.Brush CreateGlowBrush(MediaColor color, double opacity, ColorCoderDisplayMode mode)
    {
        // Peak strength follows user opacity; inner wash stays subtle.
        double o = opacity;
        if (o < 0.08) o = 0.08;
        if (o > 1.0) o = 1.0;
        var hot = BlendRgb(color, MediaColor.FromRgb(255, 255, 255), 0.42);

        var cEdge = MediaColor.FromArgb(ToByteAlpha(255 * o * 0.62), hot.R, hot.G, hot.B);
        var cMid = MediaColor.FromArgb(ToByteAlpha(255 * o * 0.28), color.R, color.G, color.B);
        var cTail = MediaColor.FromArgb(ToByteAlpha(255 * o * 0.10), color.R, color.G, color.B);
        var cClear = MediaColor.FromArgb(0, color.R, color.G, color.B);

        var g = new LinearGradientBrush
        {
            MappingMode = BrushMappingMode.RelativeToBoundingBox,
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1)
        };

        if (mode == ColorCoderDisplayMode.FillBar)
        {
            // Light from top (under the tabs) washing downward into the canvas.
            g.GradientStops.Add(new GradientStop(cEdge, 0));
            g.GradientStops.Add(new GradientStop(cMid, 0.22));
            g.GradientStops.Add(new GradientStop(cTail, 0.52));
            g.GradientStops.Add(new GradientStop(cClear, 1));
        }
        else
        {
            // Light from bottom edge upward.
            g.GradientStops.Add(new GradientStop(cClear, 0));
            g.GradientStops.Add(new GradientStop(cTail, 0.48));
            g.GradientStops.Add(new GradientStop(cMid, 0.78));
            g.GradientStops.Add(new GradientStop(cEdge, 1));
        }

        return g;
    }

    private static MediaColor BlendRgb(MediaColor a, MediaColor b, double t)
    {
        if (t <= 0) return a;
        if (t >= 1) return b;
        return MediaColor.FromRgb(
            (byte)Math.Round(a.R + (b.R - a.R) * t),
            (byte)Math.Round(a.G + (b.G - a.G) * t),
            (byte)Math.Round(a.B + (b.B - a.B) * t));
    }

    private static byte ToByteAlpha(double scaledAlpha)
    {
        int v = (int)Math.Round(scaledAlpha);
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)v;
    }

    private static void MakeClickThrough(IntPtr hwnd)
    {
        const int gwlExstyle = -20;
        const int wsExLayered = 0x80000;
        const int wsExTransparent = 0x20;

        int ex = GetWindowLong(hwnd, gwlExstyle);
        SetWindowLong(hwnd, gwlExstyle, ex | wsExLayered | wsExTransparent);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
