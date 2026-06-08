using Autodesk.Revit.UI;
using AllO.Core;

namespace AllO.Revit2024;

public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            AllO.Helpers.StartupLog.Write("Revit2024.App.OnStartup begin");
            AllO.Services.RevitServiceFactory.Creator = uiApp =>
                new AllO.Revit2024.Services.RevitService(uiApp);

            AppBootstrap.Initialize(application);
            AllO.Helpers.StartupLog.Write("Revit2024.App.OnStartup succeeded");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            AllO.Helpers.StartupLog.Write($"Revit2024.App.OnStartup FAILED: {ex}");
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
