using Autodesk.Revit.DB;
using AllO.Models;

namespace AllO.Helpers;

/// <summary>Recorre una red MEP conectada (pipe/duct/conduit/cable tray) desde un elemento
/// y la convierte en un árbol de tramos (NetNode/NetRun) agrupando entre bifurcaciones.</summary>
public static class NetworkTraversal
{
    private const int MaxElements = 5000;

    public static (NetNode Root, NetRun? RootRun, int ElementCount, bool Truncated) Build(Element start)
    {
        var visited = new HashSet<long> { start.Id.ToLong() };
        int count = 1, runIndex = 0;
        bool truncated = false;

        var rootRun = new NetRun();
        if (start is MEPCurve) Classify(start, rootRun);

        var root = new NetNode { Kind = NetNodeKind.Root, Label = ElementLabel(start) };
        foreach (var n in Neighbors(start, visited))
            root.Children.Add(Walk(n, visited, ref count, ref runIndex, ref truncated));

        return (root, rootRun.LengthFt > 0 ? rootRun : null, count, truncated);
    }

    private static NetBranch Walk(Element first, HashSet<long> visited,
        ref int count, ref int runIndex, ref bool truncated)
    {
        var run = new NetRun { Index = ++runIndex };
        var current = first;

        while (true)
        {
            visited.Add(current.Id.ToLong());
            count++;
            if (count >= MaxElements) truncated = true;

            var next = truncated ? new List<Element>() : Neighbors(current, visited).ToList();
            bool isEquipment = IsTerminalCategory(current);
            bool isFork = next.Count >= 2;

            // Las curvas siempre suman al tramo (un tap convierte la curva en bifurcación,
            // pero su longitud pertenece al tramo entrante). Los fittings solo si son de paso.
            if (current is MEPCurve || (!isFork && !isEquipment))
                Classify(current, run);

            if (isEquipment || isFork)
            {
                var node = new NetNode
                {
                    Kind = isEquipment ? (next.Count > 0 ? NetNodeKind.Equipment : NetNodeKind.Terminal)
                                       : NetNodeKind.Fork,
                    Label = ElementLabel(current)
                };
                foreach (var n in next)
                    node.Children.Add(Walk(n, visited, ref count, ref runIndex, ref truncated));
                return new NetBranch { Run = run, Node = node };
            }

            if (next.Count == 0)
                return new NetBranch
                {
                    Run = run,
                    Node = new NetNode { Kind = NetNodeKind.End, Label = ElementLabel(current) }
                };

            current = next[0];
        }
    }

    private static void Classify(Element el, NetRun run)
    {
        if (el is MEPCurve curve)
        {
            run.SegmentCount++;
            if (curve.Location is LocationCurve lc) run.LengthFt += lc.Curve.Length;
            if (string.IsNullOrEmpty(run.Size)) run.Size = SizeOf(curve);
            return;
        }
        if (el is FamilyInstance fi)
        {
            if (GetPartType(fi) == PartType.Elbow) run.ElbowCount++;
            else run.OtherFittingCount++;
        }
    }

    private static IEnumerable<Element> Neighbors(Element el, HashSet<long> visited)
    {
        var cm = el switch
        {
            MEPCurve c => c.ConnectorManager,
            FamilyInstance fi => fi.MEPModel?.ConnectorManager,
            _ => null
        };
        if (cm == null) yield break;

        var seen = new HashSet<long>();
        foreach (Connector c in cm.Connectors)
        {
            if (c.ConnectorType == ConnectorType.Logical) continue;
            // IsConnected/AllRefs solo existen en conectores PhysicalConn; otros tipos
            // no-físicos (visto en equipos VRF) lanzan InvalidOperationException.
            bool connected;
            try { connected = c.IsConnected; }
            catch { continue; }
            if (!connected) continue;

            ConnectorSet refs;
            try { refs = c.AllRefs; }
            catch { continue; }

            foreach (Connector r in refs)
            {
                if (r.ConnectorType == ConnectorType.Logical) continue;
                var owner = r.Owner;
                if (owner == null || owner.Id == el.Id) continue;
                if (owner is not MEPCurve && owner is not FamilyInstance) continue;
                if (visited.Contains(owner.Id.ToLong()) || !seen.Add(owner.Id.ToLong())) continue;
                yield return owner;
            }
        }
    }

    private static PartType GetPartType(FamilyInstance fi)
    {
        var p = fi.Symbol?.Family?.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
        return p != null ? (PartType)p.AsInteger() : PartType.Normal;
    }

    private static bool IsTerminalCategory(Element el)
    {
        if (el.Category == null) return false;
        return el.Category.BuiltInCategory is BuiltInCategory.OST_MechanicalEquipment
            or BuiltInCategory.OST_DuctTerminal
            or BuiltInCategory.OST_PlumbingFixtures
            or BuiltInCategory.OST_Sprinklers
            or BuiltInCategory.OST_ElectricalEquipment
            or BuiltInCategory.OST_ElectricalFixtures;
    }

    private static string SizeOf(MEPCurve curve)
    {
        foreach (var bip in new[]
        {
            BuiltInParameter.RBS_PIPE_DIAMETER_PARAM,
            BuiltInParameter.RBS_CURVE_DIAMETER_PARAM,
            BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM
        })
        {
            var v = curve.get_Parameter(bip)?.AsValueString();
            if (!string.IsNullOrEmpty(v)) return "Ø" + v;
        }
        return curve.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE)?.AsValueString() ?? "";
    }

    private static string ElementLabel(Element el)
    {
        var name = el.Name;
        if (el is FamilyInstance fi)
        {
            var pt = GetPartType(fi);
            if (pt is PartType.Tee or PartType.Wye or PartType.Cross
                or PartType.TapAdjustable or PartType.TapPerpendicular)
                return pt + (string.IsNullOrEmpty(name) ? "" : $" ({name})");
        }
        return string.IsNullOrEmpty(name) ? el.Category?.Name ?? "Element" : name;
    }
}
