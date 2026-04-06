using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AllO.Services;
using AllO.UI.Views;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ColorCoderCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
        try
        {
            UIApplication uiApp = commandData.Application;
            ColorCoderOverlayHost.LastUiApp = uiApp;

            if (!ColorCoderState.IsActive)
            {
                ColorCoderState.IsActive = true;
                ColorCoderState.AssignDefaultColors(uiApp.Application);
                ColorCoderOverlayHost.Refresh(uiApp);
                // Periodic refresh recreates WPF windows and causes visible flicker on the canvas;
                // ViewActivated / document events already reposition strips when needed.
                ColorCoderOverlayHost.SetTimerEnabled(false);
                return Result.Succeeded;
            }

            var window = new ColorCoderWindow(uiApp);
            window.ShowDialog();

            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
