using AllO.Models;

namespace AllO.Helpers;

/// <summary>
/// Flattens <see cref="ExcelTableData"/> to a rectangular string grid for schedule population.
/// Merged cells: only the merge origin cell contributes text (same as Excel top-left).
/// </summary>
public static class TableGenGridConverter
{
    public static string[,] BuildGrid(ExcelTableData data)
    {
        if (data.Cells.Count == 0)
            return new string[0, 0];

        int maxR = 0, maxC = 0;
        foreach (var cell in data.Cells)
        {
            maxR = Math.Max(maxR, cell.Row + cell.RowSpan);
            maxC = Math.Max(maxC, cell.Col + cell.ColSpan);
        }

        var grid = new string[maxR, maxC];
        for (int r = 0; r < maxR; r++)
            for (int c = 0; c < maxC; c++)
                grid[r, c] = string.Empty;

        foreach (var cell in data.Cells)
            grid[cell.Row, cell.Col] = cell.Text ?? string.Empty;

        return grid;
    }

    /// <summary>Row values trimmed to <paramref name="maxCols"/>; overflow columns joined into the last cell.</summary>
    public static string[] GetRowForSchedule(string[,] grid, int rowIndex, int maxCols)
    {
        int cols = grid.GetLength(1);
        if (maxCols <= 0) return Array.Empty<string>();

        var row = new string[maxCols];
        for (int c = 0; c < maxCols; c++)
            row[c] = string.Empty;

        if (cols <= maxCols)
        {
            for (int c = 0; c < cols; c++)
                row[c] = grid[rowIndex, c]?.Trim() ?? string.Empty;
            return row;
        }

        for (int c = 0; c < maxCols - 1; c++)
            row[c] = grid[rowIndex, c]?.Trim() ?? string.Empty;

        var tail = new List<string>();
        for (int c = maxCols - 1; c < cols; c++)
        {
            string t = grid[rowIndex, c]?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(t)) tail.Add(t);
        }
        row[maxCols - 1] = string.Join(" | ", tail);
        return row;
    }
}
