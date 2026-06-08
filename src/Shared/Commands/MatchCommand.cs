using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Models;
using AllO.UI.ViewModels;
using AllO.UI.Views;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class MatchCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            // 1. Pick source element
            var sourceRef = uiDoc.Selection.PickObject(
                Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select source element to copy properties from");
            var source = doc.GetElement(sourceRef);
            if (source == null)
            {
                TaskDialog.Show("Match", "Source element not found.");
                return Result.Failed;
            }

            // 2. Get all writable parameters from source
            var sourceParams = source.Parameters
                .Cast<Parameter>()
                .Where(p => !p.IsReadOnly && p.Definition != null)
                .OrderBy(p => p.Definition.Name)
                .ToList();

            if (sourceParams.Count == 0)
            {
                TaskDialog.Show("Match", "Source element has no writable parameters.");
                return Result.Failed;
            }

            // 3. Build parameter items for the picker window
            var paramItems = sourceParams.Select(p => new MatchParameterItem
            {
                Name = p.Definition.Name,
                StorageType = p.StorageType.ToString(),
                CurrentValue = GetParameterValue(p),
                IsSafe = p.StorageType != StorageType.ElementId,
                IsSelected = p.StorageType != StorageType.ElementId // auto-select safe ones
            }).ToList();

            var vm = new MatchParameterViewModel(paramItems);
            var window = new MatchParameterWindow(vm);
            var dialogResult = window.ShowDialogAndGetResult();

            if (dialogResult != true)
                return Result.Cancelled;

            var paramsToCopy = vm.Parameters.Where(p => p.IsSelected).Select(p => p.Name).ToList();
            if (paramsToCopy.Count == 0)
            {
                TaskDialog.Show("Match", "No parameters selected.");
                return Result.Cancelled;
            }

            // 4. Pick target elements
            var targetRefs = uiDoc.Selection.PickObjects(
                Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select target elements to paste properties to");
            if (targetRefs == null || targetRefs.Count == 0) return Result.Cancelled;

            using var tx = new Transaction(doc, "AllO - Match Properties");
            tx.Start();

            int matchedTargets = 0;
            int matchedParams = 0;
            int skippedPinned = 0;

            foreach (var targetRef in targetRefs)
            {
                var target = doc.GetElement(targetRef);
                if (target == null || target.Id == source.Id) continue;
                if (target.Pinned)
                {
                    skippedPinned++;
                    continue;
                }

                bool anyMatch = false;
                foreach (var sourceParam in sourceParams.Where(p => paramsToCopy.Contains(p.Definition.Name)))
                {
                    var targetParam = target.LookupParameter(sourceParam.Definition.Name);
                    if (targetParam == null || targetParam.IsReadOnly) continue;
                    if (targetParam.StorageType != sourceParam.StorageType) continue;

                    switch (sourceParam.StorageType)
                    {
                        case StorageType.String:
                            targetParam.Set(sourceParam.AsString() ?? "");
                            break;
                        case StorageType.Double:
                            targetParam.Set(sourceParam.AsDouble());
                            break;
                        case StorageType.Integer:
                            targetParam.Set(sourceParam.AsInteger());
                            break;
                        case StorageType.ElementId:
                            targetParam.Set(sourceParam.AsElementId());
                            break;
                    }

                    matchedParams++;
                    anyMatch = true;
                }

                if (anyMatch) matchedTargets++;
            }

            tx.Commit();

            var msg = $"Copied {matchedParams} parameter value(s) to {matchedTargets} element(s).";
            if (skippedPinned > 0) msg += $"\nSkipped {skippedPinned} pinned element(s).";
            TaskDialog.Show("Match", msg);
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

    private static string GetParameterValue(Parameter p)
    {
        return p.StorageType switch
        {
            StorageType.String => p.AsString() ?? "",
            StorageType.Double => p.AsValueString() ?? p.AsDouble().ToString("F3"),
            StorageType.Integer => p.AsInteger().ToString(),
            StorageType.ElementId => $"Id:{p.AsElementId().Value}",
            _ => ""
        };
    }
}
