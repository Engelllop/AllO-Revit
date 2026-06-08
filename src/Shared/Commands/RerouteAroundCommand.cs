using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class RerouteAroundCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var pipeRef = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                new MepCurveFilter(), "Select pipe/duct to reroute");
            var elem = doc.GetElement(pipeRef);
            if (elem is not MEPCurve curve) return Result.Failed;

            var pt1 = uiDoc.Selection.PickPoint("Pick first break point");
            var pt2 = uiDoc.Selection.PickPoint("Pick second break point");

            var dirInput = InputDialog.Show(
                "Reroute Direction", "Enter direction: Left, Right, Up, Down", "Right");
            if (string.IsNullOrWhiteSpace(dirInput)) return Result.Cancelled;

            var angleInput = InputDialog.Show(
                "Reroute Offset", "Enter offset distance in feet:", "2");
            if (!double.TryParse(angleInput, out double offset)) offset = 2.0;

            XYZ offsetDir = (dirInput ?? "right").Trim().ToLower() switch
            {
                "left" => XYZ.BasisX.Negate(),
                "right" => XYZ.BasisX,
                "up" => XYZ.BasisZ,
                "down" => XYZ.BasisZ.Negate(),
                _ => XYZ.BasisY
            };

            using var tx = new Transaction(doc, "AllO - Reroute Around");
            tx.Start();

            var loc = (curve.Location as LocationCurve)!;
            var line = loc.Curve as Line;
            if (line == null) { tx.RollBack(); return Result.Failed; }

            var start = line.GetEndPoint(0);
            var end = line.GetEndPoint(1);

            var proj1 = line.Project(pt1);
            var break1 = line.Evaluate(proj1.Parameter, false);
            var proj2 = line.Project(pt2);
            var break2 = line.Evaluate(proj2.Parameter, false);

            // Ensure break1 is closer to start
            if (proj1.Parameter > proj2.Parameter)
            {
                (break1, break2) = (break2, break1);
            }

            var corner1 = break1 + offsetDir * offset;
            var corner2 = break2 + offsetDir * offset;

            // Original pipe: start -> break1
            loc.Curve = Line.CreateBound(start, break1);

            // Create reroute segments
            CreateStub(doc, curve, Line.CreateBound(break1, corner1));
            CreateStub(doc, curve, Line.CreateBound(corner1, corner2));
            CreateStub(doc, curve, Line.CreateBound(corner2, break2));
            CreateStub(doc, curve, Line.CreateBound(break2, end));

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

    private static void CreateStub(Document doc, MEPCurve template, Line line)
    {
        var newId = ElementTransformUtils.CopyElement(doc, template.Id, XYZ.Zero).FirstOrDefault();
        if (newId == null) return;
        var stub = doc.GetElement(newId) as MEPCurve;
        if (stub == null) return;
        (stub.Location as LocationCurve)!.Curve = line;
    }

    private class MepCurveFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem)
            => elem is Pipe || elem is Duct || elem is FlexPipe || elem is FlexDuct;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
