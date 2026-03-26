using AllO.Core;

namespace AllO.Models;

/// <summary>
/// Represents a Revit Revision.
/// </summary>
public class RevisionInfo : ViewModelBase
{
    public int ElementId { get; set; }

    private int _sequence;
    public int Sequence
    {
        get => _sequence;
        set => SetProperty(ref _sequence, value);
    }

    private string _date = string.Empty;
    public string Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private string _issuedBy = string.Empty;
    public string IssuedBy
    {
        get => _issuedBy;
        set => SetProperty(ref _issuedBy, value);
    }

    private string _issuedTo = string.Empty;
    public string IssuedTo
    {
        get => _issuedTo;
        set => SetProperty(ref _issuedTo, value);
    }

    private string _status = "Not Issued";
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
