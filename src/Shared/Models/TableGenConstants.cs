namespace AllO.Models;

/// <summary>
/// Output targets for TableGen import. Drawing paths keep the legacy TextNote + DetailCurve flow.
/// </summary>
public static class TableGenConstants
{
    public const string OutputDrafting = "Drafting View";
    public const string OutputLegend = "Legend";
    /// <summary>Revit key schedule (Generic Models category). Excel columns map to built-in parameters.</summary>
    public const string OutputKeySchedule = "Key Schedule (generic)";
}
