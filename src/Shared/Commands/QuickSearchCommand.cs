using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AllO.Services;
using AllO.UI.Views;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class QuickSearchCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIApplication uiApp = commandData.Application;
            if (uiApp.ActiveUIDocument?.Document == null)
            { TaskDialog.Show("AllO", "No document is currently open."); return Result.Cancelled; }
            IRevitService service = RevitServiceFactory.Create(uiApp);
            new QuickSearchWindow(service).ShowDialog();
            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
        catch (Exception ex) { message = ex.Message; return Result.Failed; }
    }
}
