using System.Windows.Media;
using AllO.Core;

namespace AllO.Models;

/// <summary>
/// Represents an open document with its assigned color for view identification.
/// </summary>
public class DocumentColorInfo : ViewModelBase
{
    public string FilePath { get; set; } = string.Empty;

    private string _documentName = string.Empty;
    public string DocumentName
    {
        get => _documentName;
        set => SetProperty(ref _documentName, value);
    }

    private Color _assignedColor = Colors.DodgerBlue;
    public Color AssignedColor
    {
        get => _assignedColor;
        set
        {
            if (SetProperty(ref _assignedColor, value))
            {
                OnPropertyChanged(nameof(ColorBrush));
                OnPropertyChanged(nameof(ColorHex));
            }
        }
    }

    public SolidColorBrush ColorBrush => new(AssignedColor);
    public string ColorHex => $"#{AssignedColor.R:X2}{AssignedColor.G:X2}{AssignedColor.B:X2}";

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    private int _viewCount;
    public int ViewCount
    {
        get => _viewCount;
        set => SetProperty(ref _viewCount, value);
    }
}
