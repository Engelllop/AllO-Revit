using Autodesk.Revit.UI;
using AllO.Core;
using AllO.Helpers;
using AllO.Versioned.Services;

namespace AllO;

/// <summary>
/// Entry point del add-in, compartido entre Revit 2023/2024/2025 (clase AllO.App;
/// los 3 .addin apuntan aquí). El RevitService concreto vive en
/// AllO.Versioned.Services con las diferencias por versión resueltas con #if.
/// </summary>
public class App : IExternalApplication
{
#if REVIT2023
    private const string Ver = "2023";
#elif REVIT2024
    private const string Ver = "2024";
#else
    private const string Ver = "2025";
#endif

    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            StartupLog.Write($"Revit{Ver}.App.OnStartup begin");
            AllO.Services.RevitServiceFactory.Creator = uiApp => new RevitService(uiApp);

            AppBootstrap.Initialize(application);
            StartupLog.Write($"Revit{Ver}.App.OnStartup succeeded");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            StartupLog.Write($"Revit{Ver}.App.OnStartup FAILED: {ex}");
            TaskDialog.Show("AllO - Error",
                $"Failed to initialize AllO Add-in.\n\n{ex.Message}\n\n{ex.StackTrace}");
            return Result.Failed;
        }
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        AppBootstrap.Shutdown(application);
        return Result.Succeeded;
    }
}
