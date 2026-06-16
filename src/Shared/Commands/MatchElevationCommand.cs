using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;
using AllO.UI.Toast;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class MatchElevationCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            // Pick reference element
            var refObj = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select reference element to match elevation");
            var refElem = doc.GetElement(refObj);
            var refZ = MepUtils.GetMidElevation(refElem) ?? 0;

            // Pick single target
            var targetObj = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select element to match elevation");
            var elem = doc.GetElement(targetObj);

            if (elem == null || elem.Id == refElem.Id)
                return Result.Cancelled;
            if (elem.Pinned)
            {
                ToastHost.Show("Match Elevation", "Target is pinned.", ToastKind.Warning);
                return Result.Cancelled;
            }

            using var tx = new Transaction(doc, "AllO - Match Elevation");
            tx.Start();
            bool moved = false;

            var loc = elem.Location as LocationPoint;
            if (loc != null)
            {
                var pt = loc.Point;
                loc.Point = new XYZ(pt.X, pt.Y, refZ);
                moved = true;
            }
            else if (elem.Location is LocationCurve curveLoc)
            {
                var curve = curveLoc.Curve;
                var deltaZ = refZ - ((curve.GetEndPoint(0).Z + curve.GetEndPoint(1).Z) / 2.0);
                if (Math.Abs(deltaZ) > 0.0001)
                {
                    var translation = Transform.CreateTranslation(new XYZ(0, 0, deltaZ));
                    curveLoc.Curve = curve.CreateTransformed(translation);
                    moved = true;
                }
            }
            tx.Commit();

            ToastHost.Show("Match Elevation",
                moved ? "Elevation matched." : "Element type not supported.",
                moved ? ToastKind.Success : ToastKind.Warning);
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
