using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace AllO.Commands;

/// <summary>
/// Level Align: Select a reference MEP element, then a target element.
/// The target is moved vertically (Z) to match the reference elevation.
/// Direct command, no WPF window.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class AlignCommand : IExternalCommand
{
    private static readonly HashSet<BuiltInCategory> MepCategories = new()
    {
        BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_Conduit, BuiltInCategory.OST_CableTray,
        BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_ConduitFitting, BuiltInCategory.OST_CableTrayFitting,
        BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_DuctTerminal, BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_ElectricalEquipment, BuiltInCategory.OST_ElectricalFixtures,
        BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_Sprinklers
    };

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        if (uidoc?.Document == null)
        {
            TaskDialog.Show("AllO", "No document is currently open.");
            return Result.Cancelled;
        }
        Document doc = uidoc.Document;

        try
        {
            // 1. Select reference element (target height)
            var ref1 = uidoc.Selection.PickObject(ObjectType.Element,
                "Select REFERENCE element (target height)");
            Element refElement = doc.GetElement(ref1);

            if (!IsMepElement(refElement))
            {
                TaskDialog.Show("AllO — Level Align", "Reference element must be an MEP element.");
                return Result.Cancelled;
            }

            double? refZ = GetMidElevation(refElement);
            if (refZ == null)
            {
                TaskDialog.Show("AllO — Level Align", "Could not determine elevation for the reference element.");
                return Result.Cancelled;
            }

            // 2. Select target element to align
            var ref2 = uidoc.Selection.PickObject(ObjectType.Element,
                "Select element to ALIGN to reference height");
            Element targetElement = doc.GetElement(ref2);

            if (!IsMepElement(targetElement))
            {
                TaskDialog.Show("AllO — Level Align", "Target element must be an MEP element.");
                return Result.Cancelled;
            }

            if (targetElement.Id == refElement.Id)
                return Result.Succeeded;

            double? targetZ = GetMidElevation(targetElement);
            if (targetZ == null)
            {
                TaskDialog.Show("AllO — Level Align", "Could not determine elevation for the target element.");
                return Result.Cancelled;
            }

            double diff = refZ.Value - targetZ.Value;
            if (Math.Abs(diff) < 0.0001)
                return Result.Succeeded; // Already aligned

            // 3. Move vertically
            using (var tx = new Transaction(doc, "AllO Level Align"))
            {
                tx.Start();

                if (targetElement.Pinned)
                {
                    TaskDialog.Show("AllO — Level Align", "The target element is pinned and cannot be moved.");
                    tx.RollBack();
                    return Result.Cancelled;
                }

                ElementTransformUtils.MoveElement(doc, targetElement.Id, new XYZ(0, 0, diff));
                tx.Commit();
            }

            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
        catch (Exception ex) { message = ex.Message; return Result.Failed; }
    }

    private static bool IsMepElement(Element el)
    {
        if (el?.Category == null) return false;
        var bic = (BuiltInCategory)el.Category.Id.GetHashCode();
        return MepCategories.Contains(bic);
    }

    private static double? GetMidElevation(Element el)
    {
        try
        {
            var loc = el.Location;
            if (loc is LocationPoint lp)
                return lp.Point.Z;
            if (loc is LocationCurve lc)
            {
                var p1 = lc.Curve.GetEndPoint(0);
                var p2 = lc.Curve.GetEndPoint(1);
                return (p1.Z + p2.Z) / 2.0;
            }

            // Fallback: bounding box center
            var bbox = el.get_BoundingBox(null);
            if (bbox != null)
                return (bbox.Min.Z + bbox.Max.Z) / 2.0;

            return null;
        }
        catch { return null; }
    }
}
