using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace AllO.Helpers;

/// <summary>
/// Helpers para construir botones de ribbon.
/// NOTA: Se eliminó la generación dinámica de iconos WPF (RenderTargetBitmap) porque
/// causa crash de Revit al serializar el layout del ribbon (File > New > Project).
/// Revit no puede serializar bitmaps generados en memoria; usamos archivos .png físicos.
/// </summary>
public static class RibbonBuilder
{
    /// <summary>Button name → icon name, para que <see cref="RibbonAnimator"/> localice el PNG base de cada botón.</summary>
    public static readonly Dictionary<string, string> IconByButtonName = new();

    /// <summary>Path al ensamblado Shared, calculado relativo al ensamblado de la versión.</summary>
    public static string SharedAssemblyPath()
    {
        var thisDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(thisDir, "AllO.Shared.dll");
    }

    public static PushButtonData Button(string name, string text, string commandClass)
        => new(name, text, SharedAssemblyPath(), commandClass);

    /// <summary>Carga un icono PNG desde <c>Resources/Icons/{name}_{size}.png</c> relativo al assembly.</summary>
    private static ImageSource? LoadIcon(string name, int size)
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var path = Path.Combine(dir, "Resources", "Icons", $"{name}_{size}.png");
        if (!File.Exists(path))
            return null;

        // IMPORTANTE: cargar vía StreamSource, NO UriSource. Si el BitmapImage
        // conserva un UriSource de archivo (C:\...), Revit falla al serializar el
        // layout del ribbon en File > New > Project con "The URI prefix is not
        // recognized" → CLR exception (0xe0434352) → crash. Con StreamSource el
        // bitmap no tiene URI que serializar.
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

    /// <summary>
    /// Configura tooltip, descripción larga e iconos (16×16 y 32×32) desde archivos PNG.
    /// </summary>
    public static void Configure(
        PushButton button,
        string iconName,
        string tooltip,
        string longDescription)
    {
        button.ToolTip = tooltip;
        button.LongDescription = longDescription;
        button.Image = LoadIcon(iconName, 16);
        button.LargeImage = LoadIcon(iconName, 32);
        IconByButtonName[button.Name] = iconName;
    }

    public static void Configure(PulldownButton button, string iconName, string tooltip)
    {
        button.ToolTip = tooltip;
        button.Image = LoadIcon(iconName, 16);
        button.LargeImage = LoadIcon(iconName, 32);
        IconByButtonName[button.Name] = iconName;
    }

    public static void Configure(SplitButton button, string iconName, string tooltip)
    {
        button.ToolTip = tooltip;
        button.Image = LoadIcon(iconName, 16);
        button.LargeImage = LoadIcon(iconName, 32);
        IconByButtonName[button.Name] = iconName;
    }
}
