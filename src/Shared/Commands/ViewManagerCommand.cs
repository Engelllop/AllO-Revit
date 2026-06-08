using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ViewManagerCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var viewTypeInput = InputDialog.Show("View Manager",
                "View type to create:\nFloorPlan, CeilingPlan, Section, 3D", "FloorPlan");
            if (string.IsNullOrWhiteSpace(viewTypeInput)) return Result.Cancelled;

            var prefix = InputDialog.Show("View Manager", "View name prefix:", "VM - ");
            if (prefix == null) return Result.Cancelled;

            var templateName = InputDialog.Show("View Manager",
                "View Template name to apply (leave blank for none):", "");

            var createSheetsInput = InputDialog.Show("View Manager",
                "Create sheets and place views? (Yes / No)", "No");
            bool createSheets = createSheetsInput?.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase) == true;

            ElementId? templateId = null;
            if (!string.IsNullOrWhiteSpace(templateName))
            {
                templateId = new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .Cast<View>()
                    .FirstOrDefault(v => v.IsTemplate && v.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase))?.Id;
            }

            using var tx = new Transaction(doc, "AllO - View Manager");
            tx.Start();

            int createdViews = 0;
            int createdSheets = 0;

            if (viewTypeInput.Trim().Equals("3D", StringComparison.OrdinalIgnoreCase))
            {
                var vt3d = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == ViewFamily.ThreeDimensional);
                if (vt3d != null)
                {
                    var view3d = View3D.CreateIsometric(doc, vt3d.Id);
                    view3d.Name = prefix + "3D";
                    if (templateId != null) view3d.ViewTemplateId = templateId;
                    createdViews++;
                }
            }
            else
            {
                var viewTypeEnum = viewTypeInput.Trim().Equals("CeilingPlan", StringComparison.OrdinalIgnoreCase)
                    ? ViewFamily.CeilingPlan
                    : ViewFamily.FloorPlan;

                var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
                var vft = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>().FirstOrDefault(v => v.ViewFamily == viewTypeEnum);

                if (vft == null)
                {
                    TaskDialog.Show("View Manager", $"No ViewFamilyType found for {viewTypeInput}.");
                    tx.RollBack();
                    return Result.Failed;
                }

                foreach (var level in levels)
                {
                    var view = ViewPlan.Create(doc, vft.Id, level.Id);
                    view.Name = prefix + level.Name;
                    if (templateId != null) view.ViewTemplateId = templateId;
                    createdViews++;

                    if (createSheets)
                    {
                        var sheet = ViewSheet.Create(doc, ElementId.InvalidElementId);
                        sheet.Name = "Sheet - " + level.Name;
                        sheet.SheetNumber = "VM-" + createdViews.ToString("D2");
                        var vp = Viewport.Create(doc, sheet.Id, view.Id, new XYZ(0, 0, 0));
                        createdSheets++;
                    }
                }
            }

            tx.Commit();
            TaskDialog.Show("View Manager", $"Created {createdViews} view(s).\nSheets: {createdSheets}.");
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
