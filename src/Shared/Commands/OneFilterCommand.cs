using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.UI.ViewModels;
using AllO.UI.Views;
using ToastHost = AllO.UI.Toast.ToastHost;
using ToastKind = AllO.UI.Toast.ToastKind;

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

                var ci = System.Globalization.CultureInfo.InvariantCulture;
                string? val = p.StorageType switch
                {
                    StorageType.String => p.AsString(),
                    StorageType.Double => p.AsDouble().ToString(ci),
                    StorageType.Integer => p.AsInteger().ToString(ci),
                    StorageType.ElementId => p.AsElementId()?.Value.ToString(ci),
                    _ => null
                };

                if (val == null) continue;

                bool Num(Func<double, double, bool> cmp)
                    => double.TryParse(val, System.Globalization.NumberStyles.Any, ci, out var a)
                       && double.TryParse(targetValue, System.Globalization.NumberStyles.Any, ci, out var b)
                       && cmp(a, b);

                bool ok = op.Trim().ToLower() switch
                {
                    "equals" or "=" => val.Equals(targetValue, StringComparison.OrdinalIgnoreCase),
                    "not equals" or "!=" => !val.Equals(targetValue, StringComparison.OrdinalIgnoreCase),
                    "contains" => val.IndexOf(targetValue, StringComparison.OrdinalIgnoreCase) >= 0,
                    "greater" or ">" => Num((a, b) => a > b),
                    "greater or equal" or ">=" => Num((a, b) => a >= b),
                    "less" or "<" => Num((a, b) => a < b),
                    "less or equal" or "<=" => Num((a, b) => a <= b),
                    _ => val.Equals(targetValue, StringComparison.OrdinalIgnoreCase)
                };

                if (ok) matched.Add(elem.Id);
            }

            if (matched.Count > 0)
            {
                uiDoc.Selection.SetElementIds(matched);
                ToastHost.Show("One Filter", $"Selected {matched.Count} element(s).", ToastKind.Success);
            }
            else
            {
                ToastHost.Show("One Filter", "No elements matched the criteria.", ToastKind.Info);
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
