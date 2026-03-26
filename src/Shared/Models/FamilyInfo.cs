using AllO.Core;

namespace AllO.Models;

public class FamilyInfo : ViewModelBase
{
    public int ElementId { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private string _familyName = string.Empty;
    public string FamilyName
    {
        get => _familyName;
        set => SetProperty(ref _familyName, value);
    }

    private string _category = string.Empty;
    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    private int _typeCount;
    public int TypeCount
    {
        get => _typeCount;
        set => SetProperty(ref _typeCount, value);
    }

    private int _instanceCount;
    public int InstanceCount
    {
        get => _instanceCount;
        set => SetProperty(ref _instanceCount, value);
    }

    private bool _isInPlace;
    public bool IsInPlace
    {
        get => _isInPlace;
        set => SetProperty(ref _isInPlace, value);
    }

    public bool IsUnused => InstanceCount == 0;
}
