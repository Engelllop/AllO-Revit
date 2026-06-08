using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.UI.ViewModels;
using AllO.UI.Views;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class OneFilterCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            // Collect all parameter names from the model for the dropdown
            var allParams = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements()
                .SelectMany(e => e.Parameters.Cast<Parameter>())
                .Where(p => p.Definition != null)
                .Select(p => p.Definition.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();

            bool hasSelection = uiDoc.Selection.GetElementIds().Count > 0;

            var vm = new OneFilterViewModel(allParams, hasSelection);
            var window = new OneFilterWindow(vm);
            var dialogResult = window.ShowDialogAndGetResult();

            if (dialogResult != true)
                return Result.Cancelled;

            var paramName = vm.SelectedParameter;
            var op = vm.SelectedOperator;
            var targetValue = vm.TargetValue;
            var useSelection = vm.UseSelection;

            var sourceElements = useSelection
                ? uiDoc.Selection.GetElementIds().Select(id => doc.GetElement(id)).Where(e => e != null).Cast<Element>()
                : new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements().Cast<Element>();

            var matched = new List<ElementId>();
            foreach (var elem in sourceElements)
            {
                var p = elem.LookupParameter(paramName);
                if (p == null) continue;

                string? val = p.StorageType switch
                {
                    StorageType.String => p.AsString(),
                    StorageType.Double => p.AsDouble().ToString(),
                    StorageType.Integer => p.AsInteger().ToString(),
                    StorageType.ElementId => p.AsElementId()?.ToString(),
                    _ => null
                };

                if (val == null) continue;

                bool ok = op.Trim().ToLower() switch
                {
                    "equals" or "=" => val.Equals(targetValue, StringComparison.OrdinalIgnoreCase),
                    "contains" => val.IndexOf(targetValue, StringComparison.OrdinalIgnoreCase) >= 0,
                    "greater" or ">" => double.TryParse(val, out var dv) && double.TryParse(targetValue, out var tv) && dv > tv,
                    "less" or "<" => double.TryParse(val, out var dv2) && double.TryParse(targetValue, out var tv2) && dv2 < tv2,
                    _ => val.Equals(targetValue, StringComparison.OrdinalIgnoreCase)
                };

                if (ok) matched.Add(elem.Id);
            }

            if (matched.Count > 0)
            {
                uiDoc.Selection.SetElementIds(matched);
                TaskDialog.Show("OneFilter", $"Selected {matched.Count} element(s).");
            }
            else
            {
                TaskDialog.Show("OneFilter", "No elements matched the criteria.");
            }
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
