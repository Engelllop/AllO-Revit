using Microsoft.Win32;

namespace AllO.Helpers;

/// <summary>
/// Detecta si Revit 2024+ está usando tema oscuro. Revit guarda
/// <c>UITheme</c> y <c>CanvasTheme</c> en HKCU\Software\Autodesk\Revit\&lt;ver&gt;\Profile.
/// Valor 0 = Light, 1 = Dark.
/// Si no se puede leer, asume oscuro (la UI de AllO está pensada para dark).
/// </summary>
public static class RevitTheme
{
    public enum Mode { Light, Dark }

    public static Mode Detect()
    {
        try
        {
            using var sw = Registry.CurrentUser.OpenSubKey(@"Software\Autodesk\Revit");
            if (sw == null) return Mode.Dark;

            foreach (var verName in sw.GetSubKeyNames())
            {
                using var verKey = sw.OpenSubKey(verName);
                using var profile = verKey?.OpenSubKey("Profile");
                if (profile == null) continue;
                if (profile.GetValue("UITheme") is int v) return v == 0 ? Mode.Light : Mode.Dark;
            }
        }
        catch { /* fallback abajo */ }
        return Mode.Dark;
    }

    public static bool IsDark() => Detect() == Mode.Dark;
}
