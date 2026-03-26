using Autodesk.Revit.UI;

namespace AllO.Services;

/// <summary>
/// Factory que permite a cada proyecto de version (Revit2023/2024/2025)
/// registrar su implementacion concreta de IRevitService.
/// Esto resuelve el problema de que RevitService depende de APIs
/// especificas de cada version (ej. ElementId.IntegerValue vs .Value).
/// </summary>
public static class RevitServiceFactory
{
    /// <summary>
    /// Delegado que cada version registra en su App.OnStartup()
    /// para crear la implementacion correcta de IRevitService.
    /// </summary>
    public static Func<UIApplication, IRevitService>? Creator { get; set; }

    /// <summary>
    /// Crea una instancia de IRevitService usando la factory registrada.
    /// Si no hay factory registrada, usa MockService como fallback.
    /// </summary>
    public static IRevitService Create(UIApplication uiApp)
    {
        if (Creator != null)
            return Creator(uiApp);

        // Fallback: si no hay factory registrada, usar Mock
        return new MockService();
    }
}
