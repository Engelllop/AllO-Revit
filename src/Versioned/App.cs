using Autodesk.Revit.UI;
using AllO.Core;
using AllO.Versioned.Services;

namespace AllO;

/// <summary>
/// Entry point del add-in, compartido entre Revit 2023/2024/2025.
/// El .addin de cada versión apunta a esta clase (AllO.App). El RevitService
/// concreto vive en AllO.Versioned.Services con las diferencias por versión
/// resueltas vía #if REVIT2023.
/// </summary>
public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            AllO.Services.RevitServiceFactory.Creator = uiApp => new RevitService(uiApp);

            AppBootstrap.Initialize(application);
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
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
