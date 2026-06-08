using AllO.Core;

namespace AllO.Models;

/// <summary>
/// Observable model representing a Revit Sheet.
/// </summary>
public class SheetInfo : ViewModelBase
{
    public int ElementId { get; set; }

    private string _sheetNumber = string.Empty;
    public string SheetNumber
    {
        get => _sheetNumber;
        set => SetProperty(ref _sheetNumber, value);
    }

    private string _originalName = string.Empty;
    public string OriginalName
    {
        get => _originalName;
        set => SetProperty(ref _originalName, value);
    }

    private string _previewName = string.Empty;
    public string PreviewName
    {
        get => _previewName;
        set
        {
            if (SetProperty(ref _previewName, value))
                OnPropertyChanged(nameof(WillChange));
        }
    }

    private int _titleBlockTypeId;
    public int TitleBlockTypeId
    {
        get => _titleBlockTypeId;
        set => SetProperty(ref _titleBlockTypeId, value);
    }

    private string _titleBlockName = string.Empty;
    public string TitleBlockName
    {
        get => _titleBlockName;
        set => SetProperty(ref _titleBlockName, value);
    }

    private string _approvedBy = string.Empty;
    public string ApprovedBy
    {
        get => _approvedBy;
        set => SetProperty(ref _approvedBy, value);
    }

    private string _designedBy = string.Empty;
    public string DesignedBy
    {
        get => _designedBy;
        set => SetProperty(ref _designedBy, value);
    }

    private string _sheetIssueDate = string.Empty;
    public string SheetIssueDate
    {
        get => _sheetIssueDate;
        set => SetProperty(ref _sheetIssueDate, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool WillChange => !string.Equals(OriginalName, PreviewName, StringComparison.Ordinal);

    // TODO: populate from RevitService (query viewports on this sheet)
    private string _placedViewsSummary = string.Empty;
    public string PlacedViewsSummary
    {
        get => _placedViewsSummary;
        set => SetProperty(ref _placedViewsSummary, value);
    }

    // TODO: populate from RevitService (query legends on this sheet)
    private string _placedLegendsSummary = string.Empty;
    public string PlacedLegendsSummary
    {
        get => _placedLegendsSummary;
        set => SetProperty(ref _placedLegendsSummary, value);
    }
}
