using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Core;
using AllO.Services;
using AllO.UI.Toast;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class MultiConnectCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;
        var service = RevitServiceFactory.Create(commandData.Application);

        try
        {
            var mainRef = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select main pipe/duct/branch");
            var mainId = doc.GetElement(mainRef).Id.Value;

            var termRefs = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select terminal elements to connect to main");

            if (termRefs == null || termRefs.Count == 0) return Result.Cancelled;

            var termIds = termRefs.Select(r => doc.GetElement(r).Id.Value).ToList();
            int connected = service.ConnectElementsBatch(mainId, termIds);

            ToastHost.Show("Multi Connect", $"Connected {connected} of {termIds.Count} element(s).", ToastKind.Success);
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
