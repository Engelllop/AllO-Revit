using AllO.Core;

namespace AllO.Models;

public class GridInfo : ViewModelBase
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

    private string _newName = string.Empty;
    public string NewName
    {
        get => _newName;
        set => SetProperty(ref _newName, value);
    }

    private string _orientation = string.Empty;
    public string Orientation
    {
        get => _orientation;
        set => SetProperty(ref _orientation, value);
    }

    private double _length;
    public double Length
    {
        get => _length;
        set => SetProperty(ref _length, value);
    }

    public bool WillChange => !string.Equals(Name, NewName, StringComparison.Ordinal) && !string.IsNullOrEmpty(NewName);
}
