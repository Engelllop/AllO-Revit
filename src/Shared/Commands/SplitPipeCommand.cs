using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class SplitPipeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var picked = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                new MepUtils.MepCurveFilter(), "Select a pipe or duct to split");
            var elem = doc.GetElement(picked);
            if (elem is not MEPCurve curve) return Result.Failed;

            var optWin = new AllO.UI.Views.SplitOptionsWindow();
            if (optWin.ShowDialog() != true) return Result.Cancelled;
            if (!double.TryParse(optWin.Gap, out double gap)) gap = 0;

            var pt = uiDoc.Selection.PickPoint("Click point along the pipe/duct to split");

            using var tx = new Transaction(doc, "AllO - Split Pipe/Duct");
            tx.Start();

            var locCurve = curve.Location as LocationCurve;
            if (locCurve == null) { tx.RollBack(); return Result.Failed; }

            var originalCurve = locCurve.Curve as Line;
            if (originalCurve == null) { tx.RollBack(); return Result.Failed; }

            var p0 = originalCurve.GetEndPoint(0);
            var p1 = originalCurve.GetEndPoint(1);
            var param = originalCurve.Project(pt).Parameter;
            var splitPt = originalCurve.Evaluate(param, false);
            var dir = (p1 - p0).Normalize();

            // Capturar lo que está conectado al extremo p1 ANTES de cortar,
            // para reconectarlo al 2º tramo (si no, la red aguas abajo queda suelta).
            Connector? downstream = null;
            var endConn = ConnectorAt(curve, p1);
            if (endConn != null && endConn.IsConnected)
            {
                foreach (Connector r in endConn.AllRefs)
                    if (r.Owner.Id != curve.Id && r.ConnectorType == ConnectorType.End) { downstream = r; break; }
            }

            var seg1 = Line.CreateBound(p0, splitPt);
            var seg2Start = gap > 0 ? splitPt + dir * gap : splitPt;
            var seg2 = Line.CreateBound(seg2Start, p1);

            // 1er tramo = curva original recortada
            locCurve.Curve = seg1;

            // 2º tramo = copia de la original
            var newElem = ElementTransformUtils.CopyElement(doc, curve.Id, XYZ.Zero).FirstOrDefault();
            var newCurve = newElem != null ? doc.GetElement(newElem) as MEPCurve : null;
            if (newCurve == null) { tx.Commit(); return Result.Succeeded; }
            (newCurve.Location as LocationCurve)!.Curve = seg2;

            // Reconexiones: unir los dos tramos en el corte (si no hay gap) y
            // re-enganchar la red aguas abajo al 2º tramo.
            if (gap <= 0)
                TryConnect(ConnectorAt(curve, splitPt), ConnectorAt(newCurve, seg2Start));
            TryConnect(ConnectorAt(newCurve, p1), downstream);

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

    private static void TryConnect(Connector? a, Connector? b)
    {
        if (a == null || b == null) return;
        try { if (!a.IsConnectedTo(b)) a.ConnectTo(b); }
        catch { /* connectors incompatibles: se deja sin conectar */ }
    }
}
