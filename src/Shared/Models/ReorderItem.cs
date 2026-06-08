using AllO.Core;

namespace AllO.Models;

public class ReorderItem : ViewModelBase
{
    public int ElementId { get; set; }
    public string ElementName { get; set; } = "";
    public string Category { get; set; } = "";
    public string CurrentValue { get; set; } = "";

    private string _previewValue = "";
    public string PreviewValue
    {
        get => _previewValue;
        set => SetProperty(ref _previewValue, value);
    }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
