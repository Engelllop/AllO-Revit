using AllO.Core;

namespace AllO.Models;

public class SearchResultInfo : ViewModelBase
{
    public int ElementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FamilyType { get; set; } = string.Empty;
    public string LevelName { get; set; } = string.Empty;
    public string MatchedOn { get; set; } = string.Empty; // "Name", "ID", "Category", "Parameter", "Family"

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
