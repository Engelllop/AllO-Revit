using System.Runtime.InteropServices;
using AllO.Models;

namespace AllO.Services;

/// <summary>
/// Reads Excel files via late-bound COM interop (requires Excel installed).
/// All COM objects are properly released to avoid zombie processes.
/// </summary>
public static class ExcelReader
{
    // Excel alignment constants
    private const int XL_H_CENTER = -4108;
    private const int XL_H_LEFT = -4131;
    private const int XL_H_RIGHT = -4152;
    private const int XL_V_CENTER = -4108;
    private const int XL_V_TOP = -4160;
    private const int XL_V_BOTTOM = -4107;
    private const int XL_LINE_NONE = -4142;

    public const string USED_RANGE = "Used Range";
    public const string PRINT_AREA = "Print Area";

    /// <summary>Opens an Excel file and returns sheet names + range data for each sheet.</summary>
    public static Dictionary<string, Dictionary<string, string>> GetSheetsAndRanges(string path)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        if (!File.Exists(path)) return result;

        dynamic? xlApp = null;
        dynamic? xlWb = null;
        try
        {
            xlApp = CreateExcelApp();
            if (xlApp == null) return result;

            xlApp.Visible = false;
            xlApp.DisplayAlerts = false;
            xlWb = xlApp.Workbooks.Open(path);

            int sheetCount = xlWb.Sheets.Count;
            for (int i = 1; i <= sheetCount; i++)
            {
                dynamic ws = xlWb.Sheets[i];
                string sheetName = ws.Name;
                var ranges = new Dictionary<string, string>();

                // Named ranges
                try
                {
                    foreach (dynamic name in xlWb.Names)
                    {
                        try
                        {
                            dynamic rng = name.RefersToRange;
                            string parentName = rng.Parent.Name;
                            if (parentName == sheetName)
                            {
                                string addr = ((string)rng.Address).Replace("$", "");
                                ranges[name.Name] = addr;
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Print area
                try
                {
                    string pa = ws.PageSetup.PrintArea;
                    if (!string.IsNullOrEmpty(pa))
                        ranges[PRINT_AREA] = pa.Replace("$", "");
                }
                catch { }

                // Used range
                try
                {
                    string ur = ((string)ws.UsedRange.Address).Replace("$", "");
                    ranges[USED_RANGE] = ur;
                }
                catch
                {
                    ranges[USED_RANGE] = "A1:E10";
                }

                result[sheetName] = ranges;
                ReleaseCom(ws);
            }
        }
        catch { }
        finally
        {
            CleanupExcel(xlApp, xlWb, null);
        }

        return result;
    }

    /// <summary>Reads cell data from an Excel range for drawing in Revit.</summary>
    public static ExcelTableData? ReadTableData(string path, string sheetName, string cellRange)
    {
        if (!File.Exists(path)) return null;
        if (string.IsNullOrEmpty(cellRange)) cellRange = "A1:E10";

        dynamic? xlApp = null;
        dynamic? xlWb = null;
        dynamic? xlWs = null;

        try
        {
            xlApp = CreateExcelApp();
            if (xlApp == null) return null;

            xlApp.Visible = false;
            xlApp.DisplayAlerts = false;
            xlWb = xlApp.Workbooks.Open(path);

            try { xlWs = xlWb.Sheets[sheetName]; }
            catch { xlWs = xlWb.ActiveSheet; }

            dynamic rng = xlWs.Range[cellRange];
            int rows = rng.Rows.Count;
            int cols = rng.Columns.Count;

            // Column widths
            var colWidths = new List<double>();
            for (int c = 1; c <= cols; c++)
            {
                try { colWidths.Add((double)rng.Columns[c].Width); }
                catch { colWidths.Add(50.0); }
            }

            // Row heights
            var rowHeights = new List<double>();
            for (int r = 1; r <= rows; r++)
            {
                try { rowHeights.Add((double)rng.Rows[r].RowHeight); }
                catch { rowHeights.Add(15.0); }
            }

            // Cells
            var cells = new List<ExcelCellData>();
            for (int r = 1; r <= rows; r++)
            {
                for (int c = 1; c <= cols; c++)
                {
                    try
                    {
                        dynamic cell = rng.Cells[r, c];

                        bool isMerged = false;
                        int rSpan = 1, cSpan = 1;
                        try { isMerged = (bool)cell.MergeCells; } catch { }

                        if (isMerged)
                        {
                            dynamic ma = cell.MergeArea;
                            int cellRow = (int)cell.Row;
                            int maRow = (int)ma.Row;
                            int cellCol = (int)cell.Column;
                            int maCol = (int)ma.Column;

                            // Skip non-origin cells of a merge
                            if (cellRow != maRow || cellCol != maCol) continue;

                            rSpan = (int)ma.Rows.Count;
                            cSpan = (int)ma.Columns.Count;
                        }

                        if ((c - 1) + cSpan > cols) cSpan = cols - (c - 1);
                        if ((r - 1) + rSpan > rows) rSpan = rows - (r - 1);

                        // Value
                        string text = "";
                        try
                        {
                            object? val = cell.Value2;
                            if (val != null)
                            {
                                if (val is double d && d % 1 == 0)
                                    text = ((int)d).ToString();
                                else
                                    text = val.ToString() ?? "";
                                text = text.Replace("\r\n", "\n").Replace("\r", "\n");
                            }
                        }
                        catch { }

                        // Alignment
                        int hAlign = XL_H_LEFT;
                        int vAlign = XL_V_TOP;
                        try { hAlign = (int)cell.HorizontalAlignment; } catch { }
                        try { vAlign = (int)cell.VerticalAlignment; } catch { }

                        // Borders
                        bool bTop = false, bBottom = false, bLeft = false, bRight = false;
                        try
                        {
                            dynamic area = isMerged ? cell.MergeArea : cell;
                            dynamic borders = area.Borders;
                            try { bLeft = (int)borders[7].LineStyle != XL_LINE_NONE; } catch { }
                            try { bTop = (int)borders[8].LineStyle != XL_LINE_NONE; } catch { }
                            try { bBottom = (int)borders[9].LineStyle != XL_LINE_NONE; } catch { }
                            try { bRight = (int)borders[10].LineStyle != XL_LINE_NONE; } catch { }
                        }
                        catch { }

                        cells.Add(new ExcelCellData
                        {
                            Row = r - 1, Col = c - 1,
                            RowSpan = rSpan, ColSpan = cSpan,
                            Text = text,
                            HAlign = hAlign, VAlign = vAlign,
                            BorderTop = bTop, BorderBottom = bBottom,
                            BorderLeft = bLeft, BorderRight = bRight
                        });
                    }
                    catch { }
                }
            }

            return new ExcelTableData
            {
                ColWidths = colWidths,
                RowHeights = rowHeights,
                Cells = cells
            };
        }
        catch
        {
            return null;
        }
        finally
        {
            CleanupExcel(xlApp, xlWb, xlWs);
        }
    }

    private static dynamic? CreateExcelApp()
    {
        try
        {
            var type = Type.GetTypeFromProgID("Excel.Application");
            if (type != null)
                return Activator.CreateInstance(type);
        }
        catch { }
        return null;
    }

    private static void CleanupExcel(dynamic? xlApp, dynamic? xlWb, dynamic? xlWs)
    {
        try { if (xlWs != null) Marshal.ReleaseComObject(xlWs); } catch { }
        try { if (xlWb != null) { xlWb.Close(false); Marshal.ReleaseComObject(xlWb); } } catch { }
        try { if (xlApp != null) { xlApp.Quit(); Marshal.ReleaseComObject(xlApp); } } catch { }
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private static void ReleaseCom(object? obj)
    {
        if (obj != null)
        {
            try { Marshal.ReleaseComObject(obj); } catch { }
        }
    }
}
