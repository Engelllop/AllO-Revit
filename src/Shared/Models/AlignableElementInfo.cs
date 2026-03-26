using AllO.Core;

namespace AllO.Models;

public class AlignableElementInfo : ViewModelBase
{
    public int ElementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class DisconnectedConnectorInfo : ViewModelBase
{
    public int ElementId { get; set; }
    public string ElementName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public int DisconnectedCount { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    private bool _isSelected = true;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}
