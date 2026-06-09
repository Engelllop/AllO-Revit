using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using AllO.Models;
using AllO.UI.ViewModels;
using AllO.UI.Views;
using ToastHost = AllO.UI.Toast.ToastHost;
using ToastKind = AllO.UI.Toast.ToastKind;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ReOrderingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            // 1. Select elements to renumber
            var elemRefs = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select elements to renumber");
            if (elemRefs == null || elemRefs.Count == 0) return Result.Cancelled;

            // 2. Pick a path (model curve or detail line)
            var pathRef = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                new CurveElementFilter(), "Select a line/path to define ordering");
            var pathElem = doc.GetElement(pathRef) as CurveElement;
            if (pathElem == null)
            {
                TaskDialog.Show("ReOrdering", "Selected element is not a line.");
                return Result.Failed;
            }
            var pathCurve = pathElem.GeometryCurve;

            // 3. Project each element onto the path and sort
            var ordered = elemRefs
                .Select(r => doc.GetElement(r))
                .Where(e => e != null)
                .Select(e =>
                {
                    var loc = e!.Location;
                    XYZ? pt = null;
                    if (loc is LocationPoint lp) pt = lp.Point;
                    else if (loc is LocationCurve lc) pt = (lc.Curve.GetEndPoint(0) + lc.Curve.GetEndPoint(1)) / 2;
                    else
                    {
                        var bbox = e.get_BoundingBox(null);
                        if (bbox != null) pt = (bbox.Min + bbox.Max) / 2;
                    }

                    double param = 0;
                    if (pt != null)
                    {
                        try { param = pathCurve.Project(pt).Parameter; }
                        catch { param = 0; }
                    }

                    string currentVal = "";
                    var markParam = e.LookupParameter("Mark");
                    if (markParam != null)
                    {
                        currentVal = markParam.StorageType == StorageType.String
                            ? markParam.AsString() ?? ""
                            : markParam.AsInteger().ToString();
                    }

                    return new ReorderItem
                    {
                        ElementId = e.Id.Value,
                        ElementName = e.Name,
                        Category = e.Category?.Name ?? "",
                        CurrentValue = currentVal,
                        SortKey = param,
                        IsSelected = true
                    };
                })
                .OrderBy(x => x.SortKey)
                .ToList();

            if (ordered.Count == 0)
            {
                TaskDialog.Show("ReOrdering", "No valid elements found.");
                return Result.Failed;
            }

            // 4. Show preview window
            var vm = new ReorderViewModel(ordered);
            var window = new ReorderWindow(vm);
            var dialogResult = window.ShowDialogAndGetResult();

            if (dialogResult != true)
                return Result.Cancelled;

            var paramName = vm.ParameterName;
            if (string.IsNullOrWhiteSpace(paramName))
            {
                TaskDialog.Show("ReOrdering", "Parameter name is required.");
                return Result.Cancelled;
            }

            var selectedItems = vm.Items.Where(i => i.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                TaskDialog.Show("ReOrdering", "No elements selected.");
                return Result.Cancelled;
            }

            // 5. Apply numbering
            using var tx = new Transaction(doc, "AllO - ReOrdering");
            tx.Start();
            int current = vm.StartNumber;
            foreach (var item in selectedItems)
            {
                var elem = doc.GetElement(new ElementId(item.ElementId));
                if (elem == null) continue;

                var p = elem.LookupParameter(paramName);
                if (p != null && !p.IsReadOnly)
                {
                    string newVal = $"{vm.Prefix}{current}{vm.Suffix}";
                    if (p.StorageType == StorageType.String)
                        p.Set(newVal);
                    else if (p.StorageType == StorageType.Integer && int.TryParse(newVal, out var iv))
                        p.Set(iv);
                    current++;
                }
            }
            tx.Commit();

            ToastHost.Show("Re-Ordering", $"Renumbered {selectedItems.Count} element(s).", ToastKind.Success);
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

    private class CurveElementFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is ModelCurve || elem is DetailCurve;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
