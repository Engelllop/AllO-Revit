using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI.Selection;

namespace AllO.Helpers;

/// <summary>Utilidades MEP compartidas (antes duplicadas en Align/Connector/MatchElevation/Elbow/Reroute/SplitPipe).</summary>
public static class MepUtils
{
    public static readonly HashSet<BuiltInCategory> MepCategories = new()
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

    /// <summary>Detecta si el elemento pertenece a una categoría MEP. Usa Category.BuiltInCategory
    /// (no GetHashCode, que no es el valor del ElementId).</summary>
    public static bool IsMepElement(Element? el)
    {
        if (el?.Category == null) return false;
        return MepCategories.Contains(el.Category.BuiltInCategory);
    }

    public static double? GetMidElevation(Element el)
    {
        try
        {
            switch (el.Location)
            {
                case LocationPoint lp:
                    return lp.Point.Z;
                case LocationCurve lc:
                    return (lc.Curve.GetEndPoint(0).Z + lc.Curve.GetEndPoint(1).Z) / 2.0;
            }
            var bbox = el.get_BoundingBox(null);
            return bbox != null ? (bbox.Min.Z + bbox.Max.Z) / 2.0 : (double?)null;
        }
        catch { return null; }
    }

    /// <summary>Filtro de selección para curvas MEP: tubería, ducto (rígidos/flex), conduit y cable tray.</summary>
    public class MepCurveFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
            => elem is Pipe || elem is Duct || elem is FlexPipe || elem is FlexDuct
               || elem is Conduit || elem is CableTray;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
