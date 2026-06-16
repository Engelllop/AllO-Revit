namespace AllO.Models;

/// <summary>
/// Output targets for TableGen import. Drawing paths keep the legacy TextNote + DetailCurve flow.
/// </summary>
public static class TableGenConstants
{
    public const string OutputDrafting = "Drafting View";
    public const string OutputLegend = "Legend";
    /// <summary>Schedule sin campos cuyo HEADER se usa como lienzo: SetCellText + MergeCells +
    /// SetCellStyle reproducen el formato del Excel (técnica estilo DiRoots).</summary>
    public const string OutputSchedule = "Schedule";
}
