using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AllO.Services;
using AdRibbonButton = Autodesk.Windows.RibbonButton;
using AdRibbonItem = Autodesk.Windows.RibbonItem;
using AdRibbonListButton = Autodesk.Windows.RibbonListButton;
using AdRibbonTab = Autodesk.Windows.RibbonTab;

namespace AllO.Helpers;

/// <summary>
/// Ráfaga de animación en los iconos del tab AllO cada vez que el usuario activa el tab,
/// vía AdWindows (<c>Autodesk.Windows.ComponentManager</c>). Cada botón recibe un estilo
/// (Spin/Bounce/Slide/Fall/Pulse/Wiggle) y la amplitud sigue una envolvente por ciclo
/// (50% → 100% → 100% → 50% → 25%) para que el arranque y el final sean suaves.
///
/// SEGURIDAD:
/// - La ráfaga se ABORTA en cuanto el usuario activa cualquier elemento del ribbon
///   (<c>ComponentManager.UIElementActivated</c>): mutar LargeImage mientras Revit ejecuta
///   un comando provocó un fatal error en pruebas reales.
/// - Los frames se generan UNA vez como PNG físicos en %APPDATA%\AllO\AnimFrames y se cargan
///   con StreamSource — nunca se asigna un bitmap en memoria (RenderTargetBitmap) al ribbon,
///   porque Revit crashea al serializar el layout en File &gt; New (mismo bug que LoadIcon/UriSource).
/// - Los split/pulldown muestran el icono de su item actual, así que se animan también los hijos.
/// </summary>
public static class RibbonAnimator
{
    private const int FrameCount = 12;
    private const int IntervalMs = 50;
    private const int CanvasSize = 32;

    /// <summary>Amplitud por ciclo: entrada suave, pico, y decaimiento antes de restaurar.</summary>
    private static readonly double[] Envelope = { 0.5, 1.0, 1.0, 0.5, 0.25 };

    private enum AnimStyle
    {
        Spin,
        Bounce,
        Slide,
        Fall,
        Pulse,
        Wiggle
    }

    private sealed class AnimatedButton
    {
        /// <summary>Frames por nivel de amplitud (clave = porcentaje: 100/50/25).</summary>
        public Dictionary<int, List<ImageSource>> FramesByLevel = null!;
        public AdRibbonButton Button = null!;
        public ImageSource? Original;
        public int Stagger;
    }

    private static readonly List<AnimatedButton> Buttons = new();
    private static DispatcherTimer? _timer;
    private static int _burstTicksLeft;

    public static void Start()
    {
        try
        {
            if (!AllOSettings.Current.AnimatedRibbonIcons) return;
            if (_timer != null) return;

            var tab = FindAllOTab();
            if (tab == null)
            {
                Logging.Debug("RibbonAnimator: AllO tab not found in AdWindows ribbon.");
                return;
            }

            var frameCache = new Dictionary<string, Dictionary<int, List<ImageSource>>?>();
            var styles = (AnimStyle[])Enum.GetValues(typeof(AnimStyle));
            foreach (var item in EnumerateItems(tab))
            {
                if (item is not AdRibbonButton btn || btn.LargeImage == null) continue;
                string name = ItemName(btn.Id);
                if (!RibbonBuilder.IconByButtonName.TryGetValue(name, out var icon)) continue;

                var style = styles[Buttons.Count % styles.Length];
                string cacheKey = $"{icon}|{style}";
                if (!frameCache.TryGetValue(cacheKey, out var frames))
                {
                    frames = BuildFrames(icon, style);
                    frameCache[cacheKey] = frames;
                }
                if (frames == null) continue;

                Buttons.Add(new AnimatedButton
                {
                    Button = btn,
                    FramesByLevel = frames,
                    Original = btn.LargeImage,
                    Stagger = Buttons.Count * 2
                });
            }

            if (Buttons.Count == 0) return;

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(IntervalMs)
            };
            _timer.Tick += (_, _) => Animate();

            HookTabActivation(tab);

            // Si el usuario ejecuta un comando, cortar la ráfaga de inmediato: animar mientras
            // Revit ejecuta causó fatal error en pruebas. Solo items de comando — el clic en el
            // propio tab también dispara UIElementActivated y mataría la ráfaga al nacer.
            Autodesk.Windows.ComponentManager.UIElementActivated += (_, e) =>
            {
                try
                {
                    if (e?.Item is Autodesk.Windows.RibbonCommandItem) AbortBurst();
                }
                catch { }
            };

            Logging.Debug($"RibbonAnimator: {Buttons.Count} buttons ready (burst on tab activation).");
        }
        catch (Exception ex)
        {
            Logging.Warning($"RibbonAnimator failed to start: {ex.Message}");
        }
    }

    public static void Stop()
    {
        try
        {
            _timer?.Stop();
            _timer = null;
            RestoreOriginals();
            Buttons.Clear();
        }
        catch
        {
        }
    }

    public static void StartBurst()
    {
        if (_timer == null || Buttons.Count == 0) return;
        _burstTicksLeft = FrameCount * Envelope.Length;
        if (!_timer.IsEnabled) _timer.Start();
    }

    private static void AbortBurst()
    {
        if (_timer == null || !_timer.IsEnabled) return;
        _burstTicksLeft = 0;
        _timer.Stop();
        RestoreOriginals();
    }

    private static void HookTabActivation(AdRibbonTab tab)
    {
        if (tab is System.ComponentModel.INotifyPropertyChanged inpc)
        {
            inpc.PropertyChanged += (_, e) =>
            {
                try
                {
                    if (e.PropertyName == "IsActive" && IsTabActive(tab))
                        StartBurst();
                }
                catch
                {
                }
            };
        }
    }

    private static bool IsTabActive(AdRibbonTab tab)
    {
        try { return tab.GetType().GetProperty("IsActive")?.GetValue(tab) as bool? ?? false; }
        catch { return false; }
    }

    private static void Animate()
    {
        try
        {
            if (_burstTicksLeft <= 0)
            {
                _timer?.Stop();
                RestoreOriginals();
                return;
            }

            int totalTicks = FrameCount * Envelope.Length;
            int elapsed = totalTicks - _burstTicksLeft;
            _burstTicksLeft--;

            int loop = Math.Min(elapsed / FrameCount, Envelope.Length - 1);
            int level = (int)(Envelope[loop] * 100);

            foreach (var b in Buttons)
            {
                try
                {
                    if (b.FramesByLevel.TryGetValue(level, out var frames))
                        b.Button.LargeImage = frames[(elapsed + b.Stagger) % FrameCount];
                }
                catch
                {
                }
            }
        }
        catch
        {
            try { AbortBurst(); } catch { }
        }
    }

    private static void RestoreOriginals()
    {
        foreach (var b in Buttons)
        {
            try { if (b.Original != null) b.Button.LargeImage = b.Original; }
            catch { }
        }
    }

    private static AdRibbonTab? FindAllOTab()
    {
        var ribbon = Autodesk.Windows.ComponentManager.Ribbon;
        if (ribbon == null) return null;
        foreach (AdRibbonTab tab in ribbon.Tabs)
        {
            if (tab.Id == "AllO" || tab.Title == "AllO") return tab;
        }
        return null;
    }

    private static IEnumerable<AdRibbonItem> EnumerateItems(AdRibbonTab tab)
    {
        foreach (Autodesk.Windows.RibbonPanel panel in tab.Panels)
        {
            if (panel.Source == null) continue;
            foreach (var item in Flatten(panel.Source.Items))
                yield return item;
        }
    }

    private static IEnumerable<AdRibbonItem> Flatten(IEnumerable<AdRibbonItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            switch (item)
            {
                case Autodesk.Windows.RibbonRowPanel row:
                    foreach (var sub in Flatten(row.Items)) yield return sub;
                    break;
                // Split/pulldown: el icono visible es el del item actual, no el del contenedor.
                case AdRibbonListButton list when list.Items != null:
                    foreach (var sub in Flatten(list.Items.OfType<AdRibbonItem>())) yield return sub;
                    break;
            }
        }
    }

    /// <summary>API button ids look like "CustomCtrl_%CustomCtrl_%AllO%Panel%Name" — the name is the last segment.</summary>
    private static string ItemName(string? id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        var parts = id!.Split('%');
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (parts[i].Length > 0) return parts[i];
        }
        return "";
    }

    private static Dictionary<int, List<ImageSource>>? BuildFrames(string iconName, AnimStyle style)
    {
        try
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            var srcPath = Path.Combine(dir, "Resources", "Icons", $"{iconName}_32.png");
            if (!File.Exists(srcPath)) return null;

            string cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AllO", "AnimFrames");
            Directory.CreateDirectory(cacheDir);

            BitmapImage? baseImg = null;
            var result = new Dictionary<int, List<ImageSource>>();
            foreach (double amp in Envelope.Distinct())
            {
                int level = (int)(amp * 100);
                var frames = new List<ImageSource>(FrameCount);
                for (int i = 0; i < FrameCount; i++)
                {
                    string framePath = Path.Combine(cacheDir, $"{iconName}_32_{style}_{level}_{i}.png");
                    if (!File.Exists(framePath) ||
                        File.GetLastWriteTimeUtc(framePath) < File.GetLastWriteTimeUtc(srcPath))
                    {
                        baseImg ??= LoadPng(srcPath);
                        if (baseImg == null) return null;
                        RenderFrameToFile(baseImg, style, i, amp, framePath);
                    }

                    var frame = LoadPng(framePath);
                    if (frame == null) return null;
                    frames.Add(frame);
                }
                result[level] = frames;
            }
            return result;
        }
        catch (Exception ex)
        {
            Logging.Warning($"RibbonAnimator: frame build failed for '{iconName}' ({style}): {ex.Message}");
            return null;
        }
    }

    private static void RenderFrameToFile(BitmapImage baseImg, AnimStyle style, int frameIndex, double amp, string path)
    {
        double t = (double)frameIndex / FrameCount; // 0..1, loop perfecto
        double angle = 0, dx = 0, dy = 0, sx = 1, sy = 1;
        double drawSize = 26.0;

        switch (style)
        {
            case AnimStyle.Spin:
                // a plena amplitud gira completo; en los ciclos suaves se balancea amortiguado
                // (un giro parcial escalado se vería roto al cerrar el loop)
                angle = amp >= 1.0 ? 360.0 * t : 25.0 * amp * Math.Sin(2 * Math.PI * t);
                drawSize = 24.0;
                break;

            case AnimStyle.Bounce:
            {
                // dos botes por ciclo, con squash al aterrizar
                double a = Math.Abs(Math.Sin(2 * Math.PI * t));
                dy = -7.0 * a * amp;
                double squash = Math.Max(0, 1 - a * 3) * amp;
                sx = 1 + 0.22 * squash;
                sy = 1 - 0.22 * squash;
                break;
            }

            case AnimStyle.Slide:
                // vaivén izquierda-derecha inclinándose hacia el movimiento
                dx = 8.0 * Math.Sin(2 * Math.PI * t) * amp;
                angle = -6.0 * Math.Sin(2 * Math.PI * t) * amp;
                break;

            case AnimStyle.Fall:
                // se cae fuera del canvas girando y reaparece desde arriba
                if (t < 0.5)
                {
                    double u = t * 2;
                    dy = 36.0 * u * u * amp;
                    angle = 30.0 * u * amp;
                }
                else
                {
                    double u = (t - 0.5) * 2;
                    dy = -36.0 * (1 - u) * (1 - u) * amp;
                }
                break;

            case AnimStyle.Pulse:
            {
                double s = 1 + 0.28 * Math.Sin(2 * Math.PI * t) * amp;
                sx = s;
                sy = s;
                break;
            }

            case AnimStyle.Wiggle:
                angle = 18.0 * Math.Sin(4 * Math.PI * t) * amp;
                dy = -2.0 * Math.Abs(Math.Sin(2 * Math.PI * t)) * amp;
                break;
        }

        double inset = (CanvasSize - drawSize) / 2.0;
        double center = CanvasSize / 2.0;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new TranslateTransform(dx, dy));
            dc.PushTransform(new RotateTransform(angle, center, center));
            dc.PushTransform(new ScaleTransform(sx, sy, center, center));
            dc.DrawImage(baseImg, new Rect(inset, inset, drawSize, drawSize));
            dc.Pop();
            dc.Pop();
            dc.Pop();
        }

        var rtb = new RenderTargetBitmap(CanvasSize, CanvasSize, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        encoder.Save(fs);
    }

    private static BitmapImage? LoadPng(string path)
    {
        if (!File.Exists(path)) return null;
        var bitmap = new BitmapImage();
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
        }
        bitmap.Freeze();
        return bitmap;
    }
}
