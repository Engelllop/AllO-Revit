using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;

namespace AllO.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class BloomCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDoc = commandData.Application.ActiveUIDocument;
        var doc = uiDoc.Document;

        try
        {
            var equipRef = uiDoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element,
                "Select mechanical equipment or terminal (fan coil, sink, panel, etc.)");
            var equip = doc.GetElement(equipRef);
            if (equip == null) return Result.Failed;

            var connectors = GetConnectors(equip);
            if (connectors.Count == 0)
            {
                TaskDialog.Show("Bloom", "Selected element has no MEP connectors.");
                return Result.Failed;
            }

            using var tx = new Transaction(doc, "AllO - Bloom");
            tx.Start();
            int created = 0;
            foreach (var c in connectors)
            {
                if (c.IsConnected) continue;
                var dir = c.CoordinateSystem.BasisZ.Normalize();
                var start = c.Origin;
                var end = start + dir * 2.0; // 2-foot stub

                if (c.Domain == Domain.DomainPiping)
                {
                    var pipeType = new FilteredElementCollector(doc).OfClass(typeof(PipeType)).FirstOrDefault() as PipeType;
                    var sysType = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType)).FirstOrDefault() as PipingSystemType;
                    var level = doc.ActiveView.GenLevel;
                    if (pipeType != null && sysType != null && level != null)
                    {
                        Pipe.Create(doc, sysType.Id, pipeType.Id, level.Id, start, end);
                        created++;
                    }
                }
                else if (c.Domain == Domain.DomainHvac)
                {
                    var ductType = new FilteredElementCollector(doc).OfClass(typeof(DuctType)).FirstOrDefault() as DuctType;
                    var sysType = new FilteredElementCollector(doc).OfClass(typeof(MechanicalSystemType)).FirstOrDefault() as MechanicalSystemType;
                    var level = doc.ActiveView.GenLevel;
                    if (ductType != null && sysType != null && level != null)
                    {
                        Duct.Create(doc, sysType.Id, ductType.Id, level.Id, start, end);
                        created++;
                    }
                }
            }
            tx.Commit();

            TaskDialog.Show("Bloom", $"Created {created} MEP stub(s) from equipment connectors.");
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

    private static List<Connector> GetConnectors(Element elem)
    {
        var result = new List<Connector>();
        if (elem is FamilyInstance fi && fi.MEPModel != null)
        {
            var set = fi.MEPModel.ConnectorManager?.Connectors;
            if (set != null)
                foreach (Connector c in set) result.Add(c);
        }
        else if (elem is MEPCurve mep)
        {
            var set = mep.ConnectorManager?.Connectors;
            if (set != null)
                foreach (Connector c in set) result.Add(c);
        }
        return result;
    }
}
