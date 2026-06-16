using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using AllO.Helpers;

namespace AllO.Services;

/// <summary>
/// Settings persistentes por usuario. Se guardan en
/// <c>%AppData%\AllO\settings.json</c>. Usa DataContractJsonSerializer
/// para evitar añadir dependencias (System.Text.Json no es estándar en net48).
/// </summary>
[DataContract(Name = "AllOSettings", Namespace = "")]
public sealed class AllOSettings
{
    [DataMember] public string LastPublishFolder { get; set; } = string.Empty;
    [DataMember] public string LastTableGenFolder { get; set; } = string.Empty;
    [DataMember] public double DefaultAlignTolerance { get; set; } = 1.0;
    [DataMember] public bool ColorCoderEnabledOnStartup { get; set; } = false;
    [DataMember] public bool AnimatedRibbonIcons { get; set; } = true;

    // Propieddas preexistentes (mantenidas para compatibilidad)
    [DataMember] public string LastTableTemplate { get; set; } = string.Empty;
    [DataMember] public string LastFamilyTransferLink { get; set; } = string.Empty;
    [DataMember] public bool DarkTheme { get; set; } = true;
    [DataMember] public bool ShowToasts { get; set; } = true;
    [DataMember] public string Language { get; set; } = "es";

    /// <summary>
    /// "Auto" = seguir tema de Revit, "Dark" = forzar oscuro, "Light" = forzar claro.
    /// Si es null/vacío se usa <see cref="DarkTheme"/> como fallback legacy.
    /// </summary>
    [DataMember] public string ThemeMode { get; set; } = "Auto";

    private static readonly object FileLock = new();
    private static AllOSettings? _cached;

    private static string SettingsPath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AllO");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    public static AllOSettings Current
    {
        get
        {
            if (_cached != null) return _cached;
            lock (FileLock)
            {
                _cached ??= Load();
            }
            return _cached;
        }
    }

    private static AllOSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AllOSettings();
            using var fs = File.OpenRead(SettingsPath);
            var ser = new DataContractJsonSerializer(typeof(AllOSettings));
            return (AllOSettings?)ser.ReadObject(fs) ?? new AllOSettings();
        }
        catch (Exception ex)
        {
            Logging.Warning($"No se pudieron cargar settings: {ex.Message}");
            return new AllOSettings();
        }
    }

    public void Save()
    {
        lock (FileLock)
        {
            try
            {
                using var fs = File.Create(SettingsPath);
                var ser = new DataContractJsonSerializer(typeof(AllOSettings));
                ser.WriteObject(fs, this);
            }
            catch (Exception ex)
            {
                Logging.Error("Fallo guardando settings", ex);
            }
        }
    }
}
