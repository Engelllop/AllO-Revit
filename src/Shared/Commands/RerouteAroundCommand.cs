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
                new MepUtils.MepCurveFilter(), "Select pipe/duct to reroute");
            var elem = doc.GetElement(pipeRef);
            if (elem is not MEPCurve curve) return Result.Failed;

            var optWin = new AllO.UI.Views.RerouteOptionsWindow();
            if (optWin.ShowDialog() != true) return Result.Cancelled;
            if (!double.TryParse(optWin.Offset, out double offset)) offset = 2.0;

            var pt1 = uiDoc.Selection.PickPoint("Pick first break point");
            var pt2 = uiDoc.Selection.PickPoint("Pick second break point");

            XYZ offsetDir = optWin.SelectedDirection.Trim().ToLower() switch
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

            // Crea los 4 tramos del rodeo.
            var segA = CreateStub(doc, curve, Line.CreateBound(break1, corner1));
            var segB = CreateStub(doc, curve, Line.CreateBound(corner1, corner2));
            var segC = CreateStub(doc, curve, Line.CreateBound(corner2, break2));
            var segD = CreateStub(doc, curve, Line.CreateBound(break2, end));
            doc.Regenerate();

            // Une los tramos con codos en cada vértice (antes quedaban sueltos = red rota).
            TryElbow(doc, ConnectorAt(curve, break1), ConnectorAt(segA, break1));
            TryElbow(doc, ConnectorAt(segA, corner1), ConnectorAt(segB, corner1));
            TryElbow(doc, ConnectorAt(segB, corner2), ConnectorAt(segC, corner2));
            TryElbow(doc, ConnectorAt(segC, break2), ConnectorAt(segD, break2));

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

    private static MEPCurve? CreateStub(Document doc, MEPCurve template, Line line)
    {
        var newId = ElementTransformUtils.CopyElement(doc, template.Id, XYZ.Zero).FirstOrDefault();
        if (newId == null) return null;
        var stub = doc.GetElement(newId) as MEPCurve;
        if (stub == null) return null;
        (stub.Location as LocationCurve)!.Curve = line;
        return stub;
    }

    private static Connector? ConnectorAt(MEPCurve? curve, XYZ pt)
    {
        if (curve == null) return null;
        Connector? best = null;
        double bd = double.MaxValue;
        foreach (Connector c in curve.ConnectorManager.Connectors)
        {
            if (c.ConnectorType != ConnectorType.End) continue;
            double d = c.Origin.DistanceTo(pt);
            if (d < bd) { bd = d; best = c; }
        }
        return best;
    }

    private static void TryElbow(Document doc, Connector? a, Connector? b)
    {
        if (a == null || b == null) return;
        try { doc.Create.NewElbowFitting(a, b); }
        catch { try { if (!a.IsConnectedTo(b)) a.ConnectTo(b); } catch { } }
    }
}
