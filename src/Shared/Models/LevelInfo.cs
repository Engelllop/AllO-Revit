using AllO.Core;

namespace AllO.Models;

public class LevelInfo : ViewModelBase
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

    private double _elevation;
    public double Elevation
    {
        get => _elevation;
        set => SetProperty(ref _elevation, value);
    }

    private double _newElevation;
    public double NewElevation
    {
        get => _newElevation;
        set => SetProperty(ref _newElevation, value);
    }

    private bool _isStructural;
    public bool IsStructural
    {
        get => _isStructural;
        set => SetProperty(ref _isStructural, value);
    }

    public bool WillRename => !string.Equals(Name, NewName, StringComparison.Ordinal) && !string.IsNullOrEmpty(NewName);
    public bool WillMove => Math.Abs(Elevation - NewElevation) > 0.0001;

    private string? _syncWarning;
    public string? SyncWarning
    {
        get => _syncWarning;
        set => SetProperty(ref _syncWarning, value);
    }

    private bool _hasSyncMismatch;
    public bool HasSyncMismatch
    {
        get => _hasSyncMismatch;
        set => SetProperty(ref _hasSyncMismatch, value);
    }
}
