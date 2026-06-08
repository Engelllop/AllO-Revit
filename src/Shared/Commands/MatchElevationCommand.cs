using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Services;

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
            var refZ = GetElementZ(refElem);

            // Pick targets
            var targets = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select elements to match elevation");

            using var tx = new Transaction(doc, "AllO - Match Elevation");
            tx.Start();
            int moved = 0;
            int skippedPinned = 0;
            int skippedUnsupported = 0;
            foreach (var t in targets)
            {
                var elem = doc.GetElement(t);
                if (elem == null || elem.Id == refElem.Id) continue;
                if (elem.Pinned)
                {
                    skippedPinned++;
                    continue;
                }

                var loc = elem.Location as LocationPoint;
                if (loc != null)
                {
                    var pt = loc.Point;
                    loc.Point = new XYZ(pt.X, pt.Y, refZ);
                    moved++;
                    continue;
                }

                var curveLoc = elem.Location as LocationCurve;
                if (curveLoc != null)
                {
                    var curve = curveLoc.Curve;
                    var deltaZ = refZ - ((curve.GetEndPoint(0).Z + curve.GetEndPoint(1).Z) / 2.0);
                    if (Math.Abs(deltaZ) > 0.0001)
                    {
                        var translation = Transform.CreateTranslation(new XYZ(0, 0, deltaZ));
                        curveLoc.Curve = curve.CreateTransformed(translation);
                        moved++;
                    }
                    continue;
                }

                skippedUnsupported++;
            }
            tx.Commit();

            var msg = $"Matched elevation on {moved} element(s).";
            if (skippedPinned > 0) msg += $"\nSkipped {skippedPinned} pinned element(s).";
            if (skippedUnsupported > 0) msg += $"\nSkipped {skippedUnsupported} unsupported element(s).";
            TaskDialog.Show("Match Elevation", msg);
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

    private static double GetElementZ(Element elem)
    {
        if (elem.Location is LocationPoint lp) return lp.Point.Z;
        if (elem.Location is LocationCurve lc) return (lc.Curve.GetEndPoint(0).Z + lc.Curve.GetEndPoint(1).Z) / 2;
        var bbox = elem.get_BoundingBox(null);
        return bbox != null ? (bbox.Min.Z + bbox.Max.Z) / 2 : 0;
    }
}
