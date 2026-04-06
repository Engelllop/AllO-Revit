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
/// Transparent, click-through overlay aligned to a Revit UIView rectangle.
/// </summary>
public sealed class ColorCoderStripWindow : Window
{
    private readonly Rectangle _screenRect;

    public ColorCoderStripWindow(Rectangle screenRect, MediaColor color, ColorCoderDisplayMode mode, double opacity, double thicknessDip)
    {
        _screenRect = screenRect;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        SnapsToDevicePixels = true;

        Content = BuildContent(color, mode, opacity, thicknessDip);

        SourceInitialized += (_, _) =>
        {
            try
            {
                PositionFromScreenRect(_screenRect);
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                    MakeClickThrough(hwnd);
            }
            catch
            {
                // ignore positioning failures
            }
        };

        Loaded += (_, _) =>
        {
            try
            {
                PositionFromScreenRect(_screenRect);
            }
            catch
            {
                // ignore
            }
        };
    }

    private void PositionFromScreenRect(Rectangle r)
    {
        if (r.Width <= 0 || r.Height <= 0) return;

        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero) return;

        var source = HwndSource.FromHwnd(hwnd);
        if (source?.CompositionTarget != null)
        {
            var t = source.CompositionTarget.TransformFromDevice;
            var tl = t.Transform(new System.Windows.Point(r.Left, r.Top));
            var br = t.Transform(new System.Windows.Point(r.Right, r.Bottom));
            Left = tl.X;
            Top = tl.Y;
            Width = Math.Max(1, br.X - tl.X);
            Height = Math.Max(1, br.Y - tl.Y);
        }
        else
        {
            Left = r.Left;
            Top = r.Top;
            Width = r.Width;
            Height = r.Height;
        }
    }

    private static UIElement BuildContent(MediaColor color, ColorCoderDisplayMode mode, double opacity, double thicknessDip)
    {
        var brush = new SolidColorBrush(color) { Opacity = opacity };

        return mode switch
        {
            ColorCoderDisplayMode.FillBar => new Grid
            {
                Children =
                {
                    new Border
                    {
                        Background = brush,
                        Height = Math.Max(2, thicknessDip),
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    }
                }
            },
            ColorCoderDisplayMode.Border => new Border
            {
                BorderBrush = brush,
                BorderThickness = new Thickness(Math.Max(1, thicknessDip * 0.35)),
                Background = System.Windows.Media.Brushes.Transparent,
                SnapsToDevicePixels = true
            },
            ColorCoderDisplayMode.BottomLine => new Grid
            {
                Children =
                {
                    new Border
                    {
                        Background = brush,
                        Height = Math.Max(2, thicknessDip),
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    }
                }
            },
            _ => new Grid()
        };
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
