using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class AutoSectionBoxCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var refs = uiDoc.Selection.PickObjects(
                Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select element(s) to focus the 3D section box on");
            if (refs == null || refs.Count == 0) return Result.Cancelled;

            var selectedElements = refs.Select(r => doc.GetElement(r)).Where(e => e != null).ToList();
            if (selectedElements.Count == 0)
            {
                TaskDialog.Show("Auto Section Box", "No valid elements selected.");
                return Result.Failed;
            }

            // Compute bounding box in project coordinates
            BoundingBoxXYZ? combined = null;
            foreach (var elem in selectedElements)
            {
                var bbox = elem.get_BoundingBox(null);
                if (bbox == null) continue;

                // get_BoundingBox(null) ya devuelve coordenadas de modelo (mundo).
                // NO se aplica fi.GetTransform(): causaba doble transformación y dejaba
                // el section box en la posición equivocada. (HasSpatialElementCalculationPoint
                // además no tiene relación con tener un transform.)
                var min = bbox.Min;
                var max = bbox.Max;

                if (combined == null)
                {
                    combined = new BoundingBoxXYZ { Min = min, Max = max };
                }
                else
                {
                    combined.Min = new XYZ(
                        Math.Min(combined.Min.X, min.X),
                        Math.Min(combined.Min.Y, min.Y),
                        Math.Min(combined.Min.Z, min.Z));
                    combined.Max = new XYZ(
                        Math.Max(combined.Max.X, max.X),
                        Math.Max(combined.Max.Y, max.Y),
                        Math.Max(combined.Max.Z, max.Z));
                }
            }

            if (combined == null)
            {
                TaskDialog.Show("Auto Section Box", "Could not compute bounding box for selected elements.");
                return Result.Failed;
            }

            // Add padding (10%)
            var size = combined.Max - combined.Min;
            var padding = size * 0.1;
            combined.Min -= padding;
            combined.Max += padding;

            using var tx = new Transaction(doc, "AllO - Auto Section Box");
            tx.Start();

            // Find or create default 3D view
            View3D? view3d = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(v => !v.IsTemplate && v.Name.Contains("{3D}"));

            if (view3d == null)
            {
                var vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);
                if (vft == null)
                {
                    TaskDialog.Show("Auto Section Box", "No 3D ViewFamilyType found.");
                    tx.RollBack();
                    return Result.Failed;
                }
                view3d = View3D.CreateIsometric(doc, vft.Id);
                view3d.Name = "{3D} - Auto Section Box";
            }

            view3d.IsSectionBoxActive = true;
            view3d.SetSectionBox(combined);

            tx.Commit();

            // Open the view
            uiDoc.ActiveView = view3d;
            uiDoc.RefreshActiveView();

            // Zoom to fit
            UIView? uiview = null;
            foreach (UIView uv in uiDoc.GetOpenUIViews())
            {
                if (uv.ViewId == view3d.Id)
                {
                    uiview = uv;
                    break;
                }
            }
            uiview?.ZoomToFit();

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
