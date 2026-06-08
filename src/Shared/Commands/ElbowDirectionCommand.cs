using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ElbowLeftCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        => ElbowHelper.PlaceElbow(commandData, ref message, XYZ.BasisX.Negate());
}

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ElbowRightCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        => ElbowHelper.PlaceElbow(commandData, ref message, XYZ.BasisX);
}

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ElbowUpCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        => ElbowHelper.PlaceElbow(commandData, ref message, XYZ.BasisZ);
}

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ElbowDownCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        => ElbowHelper.PlaceElbow(commandData, ref message, XYZ.BasisZ.Negate());
}

public static class ElbowHelper
{
    public static Result PlaceElbow(ExternalCommandData commandData, ref string message, XYZ direction)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var picked = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                new MepCurveFilter(), "Select a pipe or duct to add elbow to");
            var elem = doc.GetElement(picked);
            if (elem is not MEPCurve curve) return Result.Failed;

            var pt = uiDoc.Selection.PickPoint("Click point on the pipe/duct for the elbow");

            using var tx = new Transaction(doc, "AllO - Place Elbow");
            tx.Start();

            var loc = (curve.Location as LocationCurve)!;
            var line = loc.Curve as Line;
            if (line == null) { tx.RollBack(); return Result.Failed; }

            var p0 = line.GetEndPoint(0);
            var p1 = line.GetEndPoint(1);
            var project = line.Project(pt);
            var splitPt = line.Evaluate(project.Parameter, false);

            // Shorten original to split point
            loc.Curve = Line.CreateBound(p0, splitPt);

            // Determine elbow orientation based on current pipe direction and user direction
            var pipeDir = (p1 - p0).Normalize();
            var elbowEnd = splitPt + direction * 2.0; // 2 feet stub

            var stubLine = Line.CreateBound(splitPt, elbowEnd);
            var newElem = ElementTransformUtils.CopyElement(doc, curve.Id, XYZ.Zero).FirstOrDefault();
            if (newElem != null)
            {
                var stub = doc.GetElement(newElem) as MEPCurve;
                (stub!.Location as LocationCurve)!.Curve = stubLine;
            }

            tx.Commit();
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

    private class MepCurveFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem)
            => elem is Pipe || elem is Duct || elem is FlexPipe || elem is FlexDuct;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
