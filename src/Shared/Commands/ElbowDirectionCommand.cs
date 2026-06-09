using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using AllO.Helpers;

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
                new MepUtils.MepCurveFilter(), "Select a pipe or duct to add elbow to");
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

            // El nuevo tramo conserva la longitud que tenía el tubo desde el punto
            // hasta su extremo (antes se descartaba y se forzaba a 2 ft fijos).
            double stubLen = splitPt.DistanceTo(p1);
            if (stubLen < 0.1) stubLen = 2.0;
            var elbowEnd = splitPt + direction.Normalize() * stubLen;

            // Recorta el original hasta el punto y crea el tramo girado como copia.
            loc.Curve = Line.CreateBound(p0, splitPt);
            var newElem = ElementTransformUtils.CopyElement(doc, curve.Id, XYZ.Zero).FirstOrDefault();
            var stub = newElem != null ? doc.GetElement(newElem) as MEPCurve : null;
            if (stub == null) { tx.Commit(); return Result.Succeeded; }
            (stub.Location as LocationCurve)!.Curve = Line.CreateBound(splitPt, elbowEnd);
            doc.Regenerate();

            // Inserta un codo real entre ambos tramos (antes quedaban sueltos).
            TryElbow(doc, ConnectorAt(curve, splitPt), ConnectorAt(stub, splitPt));

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

    private static Connector? ConnectorAt(MEPCurve curve, XYZ pt)
    {
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
        catch
        {
            // Si no hay familia de codo aplicable, al menos conéctalos.
            try { if (!a.IsConnectedTo(b)) a.ConnectTo(b); } catch { }
        }
    }
}
