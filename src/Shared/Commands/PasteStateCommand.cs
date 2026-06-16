using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class PasteStateCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            if (CopyStateCommand.StoredState == null)
            {
                TaskDialog.Show("Paste State", "No state has been copied yet. Use Copy State first.");
                return Result.Failed;
            }

            var view = doc.ActiveView;
            if (view == null)
            {
                TaskDialog.Show("Paste State", "No active view found.");
                return Result.Failed;
            }

            var state = CopyStateCommand.StoredState;

            using var tx = new Transaction(doc, "AllO - Paste View State");
            tx.Start();

            int appliedWorksets = 0;
            int appliedCategories = 0;
            int appliedFilters = 0;

            // Apply workset visibility
            if (doc.IsWorkshared)
            {
                foreach (var kvp in state.WorksetVisibility)
                {
                    var wsId = new WorksetId(kvp.Key);
                    try
                    {
                        view.SetWorksetVisibility(wsId, kvp.Value);
                        appliedWorksets++;
                    }
                    catch { }
                }
            }

            // Apply category visibility
            foreach (var kvp in state.CategoryVisibility)
            {
                var catId = kvp.Key.ToElementId();
                try
                {
                    view.SetCategoryHidden(catId, kvp.Value);
                    appliedCategories++;
                }
                catch { }
            }

            // Apply filters
            foreach (long filterIdInt in state.ActiveFilters)
            {
                var filterId = filterIdInt.ToElementId();
                try
                {
                    if (!view.GetFilters().Contains(filterId))
                    {
                        view.AddFilter(filterId);
                    }
                    if (state.FilterOverrides.TryGetValue(filterIdInt, out var overrides))
                    {
                        view.SetFilterOverrides(filterId, overrides);
                    }
                    appliedFilters++;
                }
                catch { }
            }

            // Apply detail level and display style (antes el DisplayStyle se copiaba pero no se aplicaba)
            try { view.DetailLevel = state.DetailLevel; } catch { }
            try { view.DisplayStyle = state.DisplayStyle; } catch { }

            tx.Commit();

            TaskDialog.Show("Paste State", $"State pasted to '{view.Name}':\n" +
                $"  Worksets: {appliedWorksets}\n" +
                $"  Categories: {appliedCategories}\n" +
                $"  Filters: {appliedFilters}");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
