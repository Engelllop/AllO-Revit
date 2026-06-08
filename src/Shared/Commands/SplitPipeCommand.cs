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
                new MepCurveFilter(), "Select a pipe or duct to split");
            var elem = doc.GetElement(picked);
            if (elem is not MEPCurve curve) return Result.Failed;

            var pt = uiDoc.Selection.PickPoint("Click point along the pipe/duct to split");

            var gapInput = InputDialog.Show(
                "Split Pipe/Duct", "Enter gap distance in feet (0 = split without gap):", "0");
            if (!double.TryParse(gapInput, out double gap)) gap = 0;

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

            var seg1 = Line.CreateBound(p0, splitPt);
            var seg2 = Line.CreateBound(splitPt, p1);

            if (gap > 0)
            {
                var dir = (p1 - p0).Normalize();
                seg2 = Line.CreateBound(splitPt + dir * gap, p1);
            }

            locCurve.Curve = seg1;

            var newElem = ElementTransformUtils.CopyElement(doc, curve.Id, XYZ.Zero).FirstOrDefault();
            if (newElem != null)
            {
                var newCurve = doc.GetElement(newElem) as MEPCurve;
                if (newCurve != null)
                {
                    (newCurve.Location as LocationCurve)!.Curve = seg2;
                }
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
