using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;
using AllO.Models;
using AllO.UI.Views;
using AllO.UI.Toast;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class NetworkTreeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            Element start;
            var sel = uiDoc.Selection.GetElementIds();
            if (sel.Count == 1 && MepUtils.IsMepElement(doc.GetElement(sel.First())))
            {
                start = doc.GetElement(sel.First());
            }
            else
            {
                var pick = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                    new MepNetworkFilter(),
                    "Select a pipe, duct, conduit, cable tray, fitting or equipment to analyze its network");
                start = doc.GetElement(pick);
            }

            var (root, rootRun, count, truncated) = NetworkTraversal.Build(start);
            if (root.Children.Count == 0 && rootRun == null)
            {
                ToastHost.Show("Network Tree", "The selected element has no connected network.", ToastKind.Warning);
                return Result.Succeeded;
            }

            var summary = NetworkSummary.From(root, rootRun);
            string Fmt(double ft) => UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, ft, false);

            new NetworkTreeWindow(root, rootRun, summary, Fmt, truncated).ShowDialog();
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

    private class MepNetworkFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem) => MepUtils.IsMepElement(elem);
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
