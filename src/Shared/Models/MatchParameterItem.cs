using AllO.Core;

namespace AllO.Models;

public class MatchParameterItem : ViewModelBase
{
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Name { get; set; } = "";
    public string StorageType { get; set; } = "";
    public string CurrentValue { get; set; } = "";
    public bool IsSafe { get; set; } = true; // false for ElementId, UniqueId, etc.
    public string Warning => IsSafe ? "" : "May be invalid in target context";
}
