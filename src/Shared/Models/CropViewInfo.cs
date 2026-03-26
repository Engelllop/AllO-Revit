using AllO.Core;

namespace AllO.Models;

public class CropViewInfo : ViewModelBase
{
    public int ElementId { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

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

    private bool _hasCropRegion;
    public bool HasCropRegion
    {
        get => _hasCropRegion;
        set => SetProperty(ref _hasCropRegion, value);
    }

    private bool _isCropActive;
    public bool IsCropActive
    {
        get => _isCropActive;
        set => SetProperty(ref _isCropActive, value);
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}
