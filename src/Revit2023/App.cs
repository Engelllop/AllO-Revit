using Autodesk.Revit.UI;
using AllO.Core;

namespace AllO.Revit2023;

public class App : IExternalApplication
{
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            AllO.Services.RevitServiceFactory.Creator = uiApp =>
                new AllO.Revit2023.Services.RevitService(uiApp);

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
