using AllO.Core;

namespace AllO.Models;

/// <summary>
/// Represents a single Excel cell's data for drawing in Revit.
/// </summary>
public class ExcelCellData
{
    public int Row { get; set; }
    public int Col { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColSpan { get; set; } = 1;
    public string Text { get; set; } = string.Empty;
    public int HAlign { get; set; } // -4131=Left, -4108=Center, -4152=Right
    public int VAlign { get; set; } // -4160=Top, -4108=Center, -4107=Bottom
    public bool BorderTop { get; set; }
    public bool BorderBottom { get; set; }
    public bool BorderLeft { get; set; }
    public bool BorderRight { get; set; }
}

/// <summary>
/// Full table data extracted from an Excel range, ready to draw in Revit.
/// </summary>
public class ExcelTableData
{
    public List<double> ColWidths { get; set; } = new();
    public List<double> RowHeights { get; set; } = new();
    public List<ExcelCellData> Cells { get; set; } = new();
}

/// <summary>
/// Represents one Excel file loaded for import configuration.
/// </summary>
public class ExcelImportTaskInfo : ViewModelBase
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    /// <summary>Sheet name → { range name → range address }</summary>
    public Dictionary<string, Dictionary<string, string>> SheetsData { get; set; } = new();

    private string _selectedSheet = string.Empty;
    public string SelectedSheet
    {
        get => _selectedSheet;
        set { if (SetProperty(ref _selectedSheet, value)) OnPropertyChanged(nameof(AvailableRanges)); }
    }

    private string _selectedRange = "Used Range";
    public string SelectedRange
    {
        get => _selectedRange;
        set => SetProperty(ref _selectedRange, value);
    }

    private string _selectedViewType = TableGenConstants.OutputDrafting;
    public string SelectedViewType
    {
        get => _selectedViewType;
        set => SetProperty(ref _selectedViewType, value);
    }

    private string _status = "Ready";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public List<string> AvailableSheets =>
        SheetsData.Keys.OrderBy(k => k).ToList();

    public List<string> AvailableRanges =>
        !string.IsNullOrEmpty(SelectedSheet) && SheetsData.ContainsKey(SelectedSheet)
            ? SheetsData[SelectedSheet].Keys.OrderBy(k => k).ToList()
            : new List<string>();

    /// <summary>Gets the actual cell range address for the current selection.</summary>
    public string GetRangeAddress()
    {
        if (!string.IsNullOrEmpty(SelectedSheet) && SheetsData.ContainsKey(SelectedSheet))
        {
            var ranges = SheetsData[SelectedSheet];
            if (!string.IsNullOrEmpty(SelectedRange) && ranges.ContainsKey(SelectedRange))
                return ranges[SelectedRange];
        }
        return "A1:E10";
    }

    public override string ToString() => FileName;
}

/// <summary>
/// An existing imported Excel table view in the project.
/// </summary>
public class ExistingTableViewInfo : ViewModelBase
{
    public int ElementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExcelPath { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DisplayInfo => string.IsNullOrEmpty(ExcelPath)
        ? Name
        : $"{Name}  →  {System.IO.Path.GetFileName(ExcelPath)} [{SheetName}]";

    /// <summary>Drawing (drafting/legend) vs Schedule (key schedule).</summary>
    public string ViewKind { get; set; } = "Drawing";
}
