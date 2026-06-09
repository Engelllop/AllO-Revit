using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using AllO.Helpers;

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

            if (!MepUtils.IsMepElement(refElement))
            {
                TaskDialog.Show("AllO — Level Align", "Reference element must be an MEP element.");
                return Result.Cancelled;
            }

            double? refZ = MepUtils.GetMidElevation(refElement);
            if (refZ == null)
            {
                TaskDialog.Show("AllO — Level Align", "Could not determine elevation for the reference element.");
                return Result.Cancelled;
            }

            // 2. Select target element to align
            var ref2 = uidoc.Selection.PickObject(ObjectType.Element,
                "Select element to ALIGN to reference height");
            Element targetElement = doc.GetElement(ref2);

            if (!MepUtils.IsMepElement(targetElement))
            {
                TaskDialog.Show("AllO — Level Align", "Target element must be an MEP element.");
                return Result.Cancelled;
            }

            if (targetElement.Id == refElement.Id)
                return Result.Succeeded;

            double? targetZ = MepUtils.GetMidElevation(targetElement);
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
}
