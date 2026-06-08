using System;
using System.IO;

namespace AllO.Helpers;

/// <summary>
/// Logger mínimo para diagnóstico de startup. Escribe a %TEMP%\AllO_startup.log
/// porque TaskDialog puede no aparecer si Revit cierra silenciosamente.
/// </summary>
public static class StartupLog
{
    private static readonly string LogPath = Path.Combine(
        Path.GetTempPath(), "AllO_startup.log");

    public static void Write(string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
        catch
        {
            // swallow — logging must never crash startup
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(LogPath))
                File.Delete(LogPath);
        }
        catch { }
    }
}
