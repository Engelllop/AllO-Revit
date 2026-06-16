using System.Windows.Threading;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AllO.UI.Views;
using AppServicesApplication = Autodesk.Revit.ApplicationServices.Application;

namespace AllO.Services;

/// <summary>
/// Manages Color Coder visuals: native document tab painting (<see cref="DocumentTabColorizer"/>,
/// modo Tabs) o overlays semitransparentes sobre el rectángulo de cada UIView (modos legacy).
/// </summary>
public static class ColorCoderOverlayHost
{
    private static bool _registered;
    private static DispatcherTimer? _timer;
    /// <summary>Coalesces rapid ViewActivated events so layout settles before recomputing strip positions.</summary>
    private static DispatcherTimer? _debounceRefresh;
    private static readonly List<ColorCoderStripWindow> Overlays = new();
    private static Dispatcher? _uiDispatcher;

    /// <summary>Last UIApplication seen (set from commands / refresh).</summary>
    public static UIApplication? LastUiApp { get; set; }

    public static void Register(UIControlledApplication application)
    {
        if (_registered) return;
        _registered = true;
        // Usar Application.Current.Dispatcher si está disponible; de lo contrario
        // Dispatcher.CurrentDispatcher (puede no ser el UI thread de Revit, pero
        // SafeInvoke maneja el marshalling).
        _uiDispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        // Revit dispara estos eventos desde threads variables (background para modelos
        // workshared/cloud). Cualquier excepción que escape de la lambda es UNHANDLED
        // y Revit se cierra silenciosamente — por eso try/catch obligatorio aquí.
        application.ViewActivated += (_, _) => SafeInvoke(ScheduleDebouncedRefresh);
        try
        {
            application.ControlledApplication.DocumentOpened += (_, _) => SafeInvoke(RefreshIfActive);
            application.ControlledApplication.DocumentClosed += (_, _) => SafeInvoke(RefreshIfActive);
        }
        catch
        {
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

    /// <summary>
    /// Ejecuta <paramref name="action"/> en el UI dispatcher capturado en Register,
    /// tragando cualquier excepción. Los eventos de Revit pueden llegar en threads
    /// arbitrarios; tocar DispatcherTimer/WPF fuera del UI thread lanza
    /// InvalidOperationException que Revit no maneja → cierre silencioso.
    /// </summary>
    private static void SafeInvoke(Action action)
    {
        try
        {
            if (_uiDispatcher != null && !_uiDispatcher.CheckAccess())
                _uiDispatcher.BeginInvoke(action);
            else
                action();
        }
        catch
        {
        }
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

        if (ColorCoderState.DisplayMode == ColorCoderDisplayMode.Tabs)
        {
            DocumentTabColorizer.Apply(uiApp);
            return;
        }

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
        DocumentTabColorizer.Clear();
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
