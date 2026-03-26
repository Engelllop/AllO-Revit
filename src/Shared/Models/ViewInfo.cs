using AllO.Core;

namespace AllO.Models;

/// <summary>
/// Represents a Revit View (floor plan, section, elevation, 3D, etc.).
/// </summary>
public class ViewInfo : ViewModelBase
{
    public int ElementId { get; set; }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _viewType = string.Empty;
    public string ViewType
    {
        get => _viewType;
        set => SetProperty(ref _viewType, value);
    }

    private string _discipline = string.Empty;
    public string Discipline
    {
        get => _discipline;
        set => SetProperty(ref _discipline, value);
    }

    private string _scale = string.Empty;
    public string Scale
    {
        get => _scale;
        set => SetProperty(ref _scale, value);
    }

    private string _viewTemplateName = string.Empty;
    public string ViewTemplateName
    {
        get => _viewTemplateName;
        set => SetProperty(ref _viewTemplateName, value);
    }

    private string _sheetNumber = string.Empty;
    public string SheetNumber
    {
        get => _sheetNumber;
        set => SetProperty(ref _sheetNumber, value);
    }

    public bool IsOnSheet => !string.IsNullOrEmpty(SheetNumber);

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
