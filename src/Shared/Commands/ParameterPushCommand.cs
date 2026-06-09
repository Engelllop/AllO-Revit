using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Services;
using AllO.Helpers;
using AllO.UI.Toast;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ParameterPushCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            // Pick source element
            var srcRef = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select source element with the parameter to copy");
            var srcElem = doc.GetElement(srcRef);

            // Choose parameter from the source element's own parameters
            var paramNames = srcElem.Parameters.Cast<Parameter>()
                .Where(p => p.Definition != null)
                .Select(p => p.Definition.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n);
            var pwin = new AllO.UI.Views.ParameterPushWindow(paramNames);
            if (pwin.ShowDialog() != true) return Result.Cancelled;
            var paramName = pwin.SelectedParameter;
            if (string.IsNullOrWhiteSpace(paramName)) return Result.Cancelled;

            var srcParam = srcElem.LookupParameter(paramName);
            if (srcParam == null)
            {
                TaskDialog.Show("Parameter Push", $"Parameter '{paramName}' not found on source element.");
                return Result.Failed;
            }

            string? valueToPush = srcParam.StorageType switch
            {
                StorageType.String => srcParam.AsString(),
                StorageType.Double => srcParam.AsDouble().ToString(),
                StorageType.Integer => srcParam.AsInteger().ToString(),
                StorageType.ElementId => srcParam.AsElementId()?.Value.ToString(),
                _ => null
            };

            if (valueToPush == null)
            {
                TaskDialog.Show("Parameter Push", "Unsupported parameter storage type.");
                return Result.Failed;
            }

            // Pick target elements
            var targetRefs = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select target elements to receive the parameter value");

            if (targetRefs == null || targetRefs.Count == 0) return Result.Cancelled;

            using var tx = new Transaction(doc, "AllO - Parameter Push");
            tx.Start();
            int applied = 0;
            foreach (var r in targetRefs)
            {
                var target = doc.GetElement(r);
                if (target == null) continue;
                var tp = target.LookupParameter(paramName);
                if (tp == null || tp.IsReadOnly) continue;

                switch (tp.StorageType)
                {
                    case StorageType.String: tp.Set(valueToPush); applied++; break;
                    case StorageType.Double when double.TryParse(valueToPush, out var dv): tp.Set(dv); applied++; break;
                    case StorageType.Integer when int.TryParse(valueToPush, out var iv): tp.Set(iv); applied++; break;
                    case StorageType.ElementId when long.TryParse(valueToPush, out var ev): tp.Set(new ElementId(ev)); applied++; break;
                }
            }
            tx.Commit();

            ToastHost.Show("Parameter Push", $"Applied to {applied} element(s).", ToastKind.Success);
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
