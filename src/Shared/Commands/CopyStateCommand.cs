using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Helpers;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class CopyStateCommand : IExternalCommand
{
    public static ViewState? StoredState { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var view = doc.ActiveView;
            if (view == null)
            {
                TaskDialog.Show("Copy State", "No active view found.");
                return Result.Failed;
            }

            var state = new ViewState();

            // Workset visibility
            if (doc.IsWorkshared)
            {
                var worksets = new FilteredWorksetCollector(doc)
                    .OfKind(WorksetKind.UserWorkset)
                    .ToWorksets();

                foreach (var ws in worksets)
                {
                    var visibility = view.GetWorksetVisibility(ws.Id);
                    state.WorksetVisibility[ws.Id.IntegerValue] = visibility;
                }
            }

            // Category visibility
            foreach (Category cat in doc.Settings.Categories)
            {
                try
                {
                    state.CategoryVisibility[cat.Id.IntegerValue] = view.GetCategoryHidden(cat.Id);
                }
                catch { }
            }

            // View filters and overrides
            foreach (ElementId filterId in view.GetFilters())
            {
                state.ActiveFilters.Add(filterId.IntegerValue);
                var overrides = view.GetFilterOverrides(filterId);
                state.FilterOverrides[filterId.IntegerValue] = overrides;
            }

            // View detail level and display style
            state.DetailLevel = view.DetailLevel;
            try { state.DisplayStyle = view.DisplayStyle; } catch { }

            // Import visibility
            var imports = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)).ToElementIds();
            foreach (ElementId id in imports)
            {
                state.ImportVisibility[id.IntegerValue] = view.IsElementVisibleInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate, id);
            }

            StoredState = state;

            TaskDialog.Show("Copy State", $"State copied from '{view.Name}':\n" +
                $"  Worksets: {state.WorksetVisibility.Count}\n" +
                $"  Categories: {state.CategoryVisibility.Count}\n" +
                $"  Filters: {state.ActiveFilters.Count}");

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}

public class ViewState
{
    public Dictionary<int, WorksetVisibility> WorksetVisibility { get; } = new();
    public Dictionary<int, bool> CategoryVisibility { get; } = new();
    public List<int> ActiveFilters { get; } = new();
    public Dictionary<int, OverrideGraphicSettings> FilterOverrides { get; } = new();
    public ViewDetailLevel DetailLevel { get; set; }
    public DisplayStyle DisplayStyle { get; set; }
    public Dictionary<int, bool> ImportVisibility { get; } = new();
}
