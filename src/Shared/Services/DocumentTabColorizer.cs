using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Autodesk.Revit.UI;
using AllO.Helpers;

namespace AllO.Services;

/// <summary>
/// Paints Revit's native document tab headers (AvalonDock DocumentPaneTabPanel children)
/// with the Color Coder per-document colors — same look as NonicaTab/pyRevit tab coloring.
/// Pure visual-tree walk + reflection: no Xceed/pyRevit reference, so it tolerates
/// AvalonDock version drift across Revit 2023-2025.
/// </summary>
public static class DocumentTabColorizer
{
    private sealed class Painted
    {
        public WeakReference<Control> Tab = null!;
        public bool HadLocalBackground;
        public Brush? OriginalBackground;
        public bool HadLocalForeground;
        public Brush? OriginalForeground;
    }

    private static readonly List<Painted> PaintedTabs = new();

    public static void Apply(UIApplication uiApp)
    {
        Clear();

        Visual? root;
        try
        {
            root = HwndSource.FromHwnd(uiApp.MainWindowHandle)?.RootVisual;
        }
        catch
        {
            return;
        }
        if (root == null) return;

        var tabs = new List<Control>();
        foreach (var panel in FindByTypeName(root, "DocumentPaneTabPanel"))
        {
            int count = VisualTreeHelper.GetChildrenCount(panel);
            for (int i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(panel, i) is Control c &&
                    c.GetType().Name.IndexOf("TabItem", StringComparison.Ordinal) >= 0)
                    tabs.Add(c);
            }
        }
        if (tabs.Count == 0)
        {
            Logging.Debug("DocumentTabColorizer: no document tab panel found in visual tree.");
            return;
        }

        string? onlyDoc = ColorCoderState.DocumentColors.Count == 1
            ? ColorCoderState.DocumentColors.Keys.First()
            : null;

        foreach (var tab in tabs)
        {
            try
            {
                object? model = tab.DataContext ?? (tab as ContentControl)?.Content;
                bool isSelected = tab is TabItem ti
                    ? ti.IsSelected
                    : GetBoolProp(model, "IsSelected") ?? false;

                string? docTitle = onlyDoc ?? MatchDocument(model);
                if (docTitle == null) continue;

                var color = ColorCoderState.GetColorForDocument(docTitle);
                double alphaFactor = isSelected ? 1.0 : 0.45;
                byte alpha = (byte)Math.Max(0, Math.Min(255,
                    ColorCoderState.Opacity * alphaFactor * 255));
                var bg = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
                bg.Freeze();

                double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255.0;
                var fg = luminance > 0.65 && alpha > 180 ? Brushes.Black : Brushes.White;

                var paint = new Painted
                {
                    Tab = new WeakReference<Control>(tab),
                    HadLocalBackground = tab.ReadLocalValue(Control.BackgroundProperty) != DependencyProperty.UnsetValue,
                    OriginalBackground = tab.Background,
                    HadLocalForeground = tab.ReadLocalValue(Control.ForegroundProperty) != DependencyProperty.UnsetValue,
                    OriginalForeground = tab.Foreground
                };

                tab.Background = bg;
                tab.Foreground = fg;
                PaintedTabs.Add(paint);
            }
            catch
            {
                // single tab failure must not break the rest
            }
        }
    }

    public static void Clear()
    {
        foreach (var p in PaintedTabs)
        {
            try
            {
                if (!p.Tab.TryGetTarget(out var tab)) continue;
                if (p.HadLocalBackground) tab.Background = p.OriginalBackground;
                else tab.ClearValue(Control.BackgroundProperty);
                if (p.HadLocalForeground) tab.Foreground = p.OriginalForeground;
                else tab.ClearValue(Control.ForegroundProperty);
            }
            catch
            {
            }
        }
        PaintedTabs.Clear();
    }

    /// <summary>Maps a tab's layout model to an open document by Title/ToolTip substring.</summary>
    private static string? MatchDocument(object? model)
    {
        string title = GetStringProp(model, "Title") ?? "";
        string tooltip = GetStringProp(model, "ToolTip") ?? "";

        foreach (var docTitle in ColorCoderState.DocumentColors.Keys)
        {
            if (title.IndexOf(docTitle, StringComparison.OrdinalIgnoreCase) >= 0 ||
                tooltip.IndexOf(docTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                return docTitle;
        }
        // Tab titles omit the project name while a single project is open in practice,
        // but with multiple docs an unmatched tab is better left unpainted than wrong.
        return null;
    }

    private static IEnumerable<DependencyObject> FindByTypeName(DependencyObject root, string typeName)
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node.GetType().Name.IndexOf(typeName, StringComparison.Ordinal) >= 0)
            {
                yield return node;
                continue;
            }
            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
                queue.Enqueue(VisualTreeHelper.GetChild(node, i));
        }
    }

    private static string? GetStringProp(object? obj, string name)
    {
        if (obj == null) return null;
        try { return obj.GetType().GetProperty(name)?.GetValue(obj)?.ToString(); }
        catch { return null; }
    }

    private static bool? GetBoolProp(object? obj, string name)
    {
        if (obj == null) return null;
        try { return obj.GetType().GetProperty(name)?.GetValue(obj) as bool?; }
        catch { return null; }
    }
}
