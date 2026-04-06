using System.Windows.Threading;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AllO.UI.Views;
using AppServicesApplication = Autodesk.Revit.ApplicationServices.Application;

namespace AllO.Services;

/// <summary>
/// Manages semi-transparent overlays on top of each open Revit UIView drawing rectangle.
/// </summary>
/// <remarks>
/// pyRevit "Tab Coloring" uses <c>DocumentTabEventUtils</c> from the IronPython-bound <c>pyrevit.runtime</c> assembly
/// (see <c>pyrevit/revit/tabs.py</c>) to paint native document tabs. That API is not part of the public Revit SDK and is
/// not shipped as a standalone reference we can call from AllO without bundling pyRevit's runtime DLLs and matching Revit versions.
/// </remarks>
public static class ColorCoderOverlayHost
{
    private static bool _registered;
    private static DispatcherTimer? _timer;
    /// <summary>Coalesces rapid ViewActivated events so layout settles before recomputing strip positions.</summary>
    private static DispatcherTimer? _debounceRefresh;
    private static readonly List<ColorCoderStripWindow> Overlays = new();

    /// <summary>Last UIApplication seen (set from commands / refresh).</summary>
    public static UIApplication? LastUiApp { get; set; }

    public static void Register(UIControlledApplication application)
    {
        if (_registered) return;
        _registered = true;

        application.ViewActivated += (_, _) => ScheduleDebouncedRefresh();
        try
        {
            // Revit 2023/2024: use ControlledApplication (UIControlledApplication.Application is newer).
            application.ControlledApplication.DocumentOpened += (_, _) => RefreshIfActive();
            application.ControlledApplication.DocumentClosed += (_, _) => RefreshIfActive();
        }
        catch
        {
            // ignore if API differs
        }

        _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(450),
            IsEnabled = false
        };
        _timer.Tick += (_, _) => RefreshIfActive();

        _debounceRefresh = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(95),
            IsEnabled = false
        };
        _debounceRefresh.Tick += (_, _) =>
        {
            _debounceRefresh!.IsEnabled = false;
            RefreshIfActive();
        };
    }

    /// <summary>Waits briefly after a view switch so Revit finishes resizing UIView rectangles before we place strips.</summary>
    private static void ScheduleDebouncedRefresh()
    {
        if (_debounceRefresh == null)
        {
            RefreshIfActive();
            return;
        }

        _debounceRefresh.IsEnabled = false;
        _debounceRefresh.IsEnabled = true;
    }

    public static void SetTimerEnabled(bool enabled)
    {
        if (_timer == null) return;
        _timer.IsEnabled = enabled;
    }

    public static void RefreshIfActive()
    {
        if (!ColorCoderState.IsActive || LastUiApp == null) return;
        try
        {
            Refresh(LastUiApp);
        }
        catch
        {
            // ignore
        }
    }

    public static void Refresh(UIApplication uiApp)
    {
        LastUiApp = uiApp;
        ClearOverlays();

        if (!ColorCoderState.IsActive) return;

        AppServicesApplication app = uiApp.Application;

        foreach (Document doc in app.Documents)
        {
            if (doc.IsFamilyDocument) continue;

            string title = string.IsNullOrEmpty(doc.Title) ? "Untitled" : doc.Title;
            var wpfColor = ColorCoderState.GetColorForDocument(title);

            UIDocument uidoc;
            try
            {
                uidoc = new UIDocument(doc);
            }
            catch
            {
                continue;
            }

            IList<UIView> views;
            try
            {
                views = uidoc.GetOpenUIViews();
            }
            catch
            {
                continue;
            }

            if (views == null || views.Count == 0) continue;

            foreach (UIView uv in views)
            {
                try
                {
                    using (var dbRect = uv.GetWindowRectangle())
                    {
                        // Revit returns Autodesk.Revit.DB.Rectangle; WPF overlay uses System.Drawing.Rectangle.
                        var rect = System.Drawing.Rectangle.FromLTRB(
                            dbRect.Left, dbRect.Top, dbRect.Right, dbRect.Bottom);
                        if (rect.Width <= 0 || rect.Height <= 0) continue;

                        var win = new ColorCoderStripWindow(
                            rect,
                            wpfColor,
                            ColorCoderState.DisplayMode,
                            ColorCoderState.Opacity,
                            ColorCoderState.BarThicknessDip);
                        win.Show();
                        Overlays.Add(win);
                    }
                }
                catch
                {
                    // ignore single overlay failures
                }
            }
        }
    }

    public static void ClearOverlays()
    {
        foreach (var w in Overlays)
        {
            try
            {
                w.Close();
            }
            catch
            {
                // ignore
            }
        }
        Overlays.Clear();
    }

    public static void Shutdown()
    {
        SetTimerEnabled(false);
        if (_debounceRefresh != null)
            _debounceRefresh.IsEnabled = false;
        ClearOverlays();
        LastUiApp = null;
    }
}
