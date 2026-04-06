using Autodesk.Revit.DB;

namespace AllO.Helpers;

/// <summary>
/// Collects instance parameter names and display values from a Revit element
/// (for sheet filtering in Publish — includes shared / project parameters on sheets).
/// </summary>
public static class ElementParameterHelper
{
    public static Dictionary<string, string> CollectParameters(Element element)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (element == null) return dict;

        foreach (Parameter p in element.Parameters)
        {
            if (p?.Definition == null) continue;
            try
            {
                if (!p.HasValue) continue;
            }
            catch
            {
                continue;
            }

            string name = p.Definition.Name;
            if (string.IsNullOrWhiteSpace(name)) continue;

            string val = p.AsValueString() ?? string.Empty;
            if (string.IsNullOrEmpty(val))
            {
                try
                {
                    val = p.AsString() ?? string.Empty;
                }
                catch
                {
                    val = string.Empty;
                }
            }

            dict[name] = val;
        }

        return dict;
    }
}
