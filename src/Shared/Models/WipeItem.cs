using AllO.Core;

namespace AllO.Models;

/// <summary>
/// Represents a single element category that can be cleaned up by WipeCommand.
/// </summary>
public class WipeItem : ViewModelBase
{
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Category { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Count { get; set; }
    public string Description => $"{DisplayName} ({Count})";
}
