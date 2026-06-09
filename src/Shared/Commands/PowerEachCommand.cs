using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using AllO.UI.Toast;

namespace AllO.Commands;

/// <summary>Crea UN circuito de potencia POR elemento seleccionado (Revit nativo mete
/// toda la selección en un solo circuito) y los asigna todos al tablero elegido.</summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class PowerEachCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var targets = uiDoc.Selection.GetElementIds()
                .Select(doc.GetElement)
                .Where(IsCircuitable)
                .ToList();

            if (targets.Count == 0)
            {
                var picks = uiDoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element,
                    new CircuitableFilter(),
                    "Select the elements to power (one circuit will be created per element)");
                targets = picks.Select(p => doc.GetElement(p)).ToList();
            }

            var panelRef = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                new PanelFilter(), "Select the panel");
            var panel = (FamilyInstance)doc.GetElement(panelRef);

            int created = 0, skipped = 0;
            using var tx = new Transaction(doc, "AllO - Power Each");
            tx.Start();
            foreach (var el in targets)
            {
                try
                {
                    var sys = ElectricalSystem.Create(doc, new List<ElementId> { el.Id },
                        ElectricalSystemType.PowerCircuit);
                    sys.SelectPanel(panel);
                    created++;
                }
                catch
                {
                    skipped++;
                }
            }
            tx.Commit();

            var msg = $"Created {created} circuit(s) on {panel.Name}.";
            if (skipped > 0) msg += $" {skipped} skipped (already circuited or no power connector).";
            ToastHost.Show("Power Each", msg, skipped == 0 ? ToastKind.Success : ToastKind.Warning);
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

    private static bool IsCircuitable(Element? el)
        => el is FamilyInstance fi && fi.MEPModel != null
           && el.Category?.BuiltInCategory != BuiltInCategory.OST_ElectricalEquipment;

    private class CircuitableFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem) => IsCircuitable(elem);
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    private class PanelFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem)
            => elem is FamilyInstance && elem.Category?.BuiltInCategory == BuiltInCategory.OST_ElectricalEquipment;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
