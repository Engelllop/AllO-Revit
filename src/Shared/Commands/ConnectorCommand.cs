using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace AllO.Commands;

/// <summary>
/// Select 2 MEP elements → finds best connector pair → moves element 2 to align → connects them.
/// Direct command, no WPF window.
/// </summary>
[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public class ConnectorCommand : IExternalCommand
{
    private static readonly HashSet<BuiltInCategory> MepCategories = new()
    {
        BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_Conduit, BuiltInCategory.OST_CableTray,
        BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_ConduitFitting, BuiltInCategory.OST_CableTrayFitting,
        BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_DuctTerminal, BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_ElectricalEquipment, BuiltInCategory.OST_ElectricalFixtures,
        BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_Sprinklers
    };

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
        if (uidoc?.Document == null)
        {
            TaskDialog.Show("AllO", "No document is currently open.");
            return Result.Cancelled;
        }
        Document doc = uidoc.Document;

        try
        {
            // 1. Pick two MEP elements
            var ref1 = uidoc.Selection.PickObject(ObjectType.Element, "Select FIRST MEP element");
            Element el1 = doc.GetElement(ref1);

            var ref2 = uidoc.Selection.PickObject(ObjectType.Element, "Select SECOND MEP element (will be moved)");
            Element el2 = doc.GetElement(ref2);

            if (!IsMepElement(el1) || !IsMepElement(el2))
            {
                TaskDialog.Show("AllO — Connector", "Both elements must be MEP elements\n(Ducts, Pipes, Conduit, Cable Tray, Fittings, etc.)");
                return Result.Cancelled;
            }

            // 2. Get connectors
            var connectors1 = GetEndConnectors(el1);
            var connectors2 = GetEndConnectors(el2);

            if (connectors1.Count == 0 || connectors2.Count == 0)
            {
                TaskDialog.Show("AllO — Connector", "Could not find available connectors on one or both elements.");
                return Result.Cancelled;
            }

            // 3. Find best pair (closest, compatible, unconnected)
            var bestPair = FindBestPair(connectors1, connectors2);
            if (bestPair == null)
            {
                TaskDialog.Show("AllO — Connector", "No compatible unconnected connectors found between these elements.");
                return Result.Cancelled;
            }

            var (fixedConn, movingConn) = bestPair.Value;

            // 4. Move + Connect inside transaction
            using (var tx = new Transaction(doc, "AllO Connect MEP"))
            {
                tx.Start();

                // Move element 2 so connectors align
                XYZ translation = fixedConn.Origin - movingConn.Origin;
                if (translation.GetLength() > 0.0001 && !el2.Pinned)
                {
                    ElementTransformUtils.MoveElement(doc, el2.Id, translation);
                    doc.Regenerate();
                }

                // Connect
                try
                {
                    fixedConn.ConnectTo(movingConn);
                }
                catch
                {
                    // Regenerate connectors after move and retry
                    var newConns2 = GetEndConnectors(el2);
                    var retryPair = FindBestPair(connectors1, newConns2);
                    if (retryPair != null)
                    {
                        try { retryPair.Value.Item1.ConnectTo(retryPair.Value.Item2); }
                        catch { }
                    }
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
        catch (Exception ex) { message = ex.Message; return Result.Failed; }
    }

    private static bool IsMepElement(Element el)
    {
        if (el?.Category == null) return false;
        var bic = (BuiltInCategory)el.Category.Id.GetHashCode();
        return MepCategories.Contains(bic);
    }

    private static List<Connector> GetEndConnectors(Element el)
    {
        var result = new List<Connector>();
        ConnectorManager? cm = null;

        if (el is MEPCurve mep)
            cm = mep.ConnectorManager;
        else if (el is FamilyInstance fi && fi.MEPModel?.ConnectorManager != null)
            cm = fi.MEPModel.ConnectorManager;

        if (cm == null) return result;

        foreach (Connector c in cm.Connectors)
        {
            if (c.ConnectorType == ConnectorType.End)
                result.Add(c);
        }
        return result;
    }

    private static (Connector, Connector)? FindBestPair(List<Connector> list1, List<Connector> list2)
    {
        (Connector, Connector)? best = null;
        double bestDist = double.MaxValue;
        (Connector, Connector)? fallback = null;
        double fallbackDist = double.MaxValue;

        foreach (var c1 in list1)
        {
            if (c1.IsConnected) continue;
            foreach (var c2 in list2)
            {
                if (c2.IsConnected) continue;
                if (c1.Owner.Id == c2.Owner.Id) continue;

                double dist = c1.Origin.DistanceTo(c2.Origin);

                // Compatible = same domain or one is undefined
                bool compatible = c1.Domain == c2.Domain ||
                                  c1.Domain == Domain.DomainUndefined ||
                                  c2.Domain == Domain.DomainUndefined;

                if (compatible && dist < bestDist)
                {
                    bestDist = dist;
                    best = (c1, c2);
                }
                if (dist < fallbackDist)
                {
                    fallbackDist = dist;
                    fallback = (c1, c2);
                }
            }
        }
        return best ?? fallback;
    }
}
