using AllO.Core;

namespace AllO.Models;

/// <summary>
/// Represents a sheet to be published (exported to PDF/DWG).
/// </summary>
public class PublishSheetItem : ViewModelBase
{
    public int ElementId { get; set; }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private string _sheetNumber = string.Empty;
    public string SheetNumber
    {
        get => _sheetNumber;
        set => SetProperty(ref _sheetNumber, value);
    }

    private string _sheetName = string.Empty;
    public string SheetName
    {
        get => _sheetName;
        set => SetProperty(ref _sheetName, value);
    }

    private string _status = "Pending";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string FileName => $"{SheetNumber} - {SheetName}";

    /// <summary>
    /// Instance parameter names and display values (e.g. shared / project params on the sheet).
    /// Used by Publish filter.
    /// </summary>
    public Dictionary<string, string> ParameterValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
