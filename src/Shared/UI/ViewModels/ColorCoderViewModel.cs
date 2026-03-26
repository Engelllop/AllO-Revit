using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using AllO.Core;
using AllO.Models;
using Autodesk.Revit.UI;

namespace AllO.UI.ViewModels;

public class ColorCoderViewModel : ViewModelBase
{
    private readonly UIApplication _uiApp;

    // Predefined color palette (vibrant, distinguishable)
    private static readonly Color[] Palette = new[]
    {
        Color.FromRgb(0x6C, 0x63, 0xFF), // Purple (AllO accent)
        Color.FromRgb(0x34, 0xD3, 0x99), // Green
        Color.FromRgb(0xFB, 0x71, 0x85), // Pink/Red
        Color.FromRgb(0xFB, 0xBF, 0x24), // Amber
        Color.FromRgb(0x38, 0xBD, 0xF8), // Sky Blue
        Color.FromRgb(0xF4, 0x72, 0xB6), // Magenta
        Color.FromRgb(0xA7, 0x8B, 0xFA), // Lavender
        Color.FromRgb(0x2D, 0xD4, 0xBF), // Teal
        Color.FromRgb(0xFF, 0x84, 0x4B), // Orange
        Color.FromRgb(0x22, 0xD3, 0xEE), // Cyan
    };

    // Available preset colors for the color picker
    public ObservableCollection<Color> PresetColors { get; } = new();

    public ObservableCollection<DocumentColorInfo> Documents { get; } = new();

    // ── Display Mode ──────────────────────────────────────────

    private bool _modeFill = true;
    public bool ModeFill
    {
        get => _modeFill;
        set
        {
            if (SetProperty(ref _modeFill, value) && value)
            {
                ModeBorder = false;
                ModeLine = false;
                OnPropertyChanged(nameof(DisplayModeDescription));
            }
        }
    }

    private bool _modeBorder;
    public bool ModeBorder
    {
        get => _modeBorder;
        set
        {
            if (SetProperty(ref _modeBorder, value) && value)
            {
                ModeFill = false;
                ModeLine = false;
                OnPropertyChanged(nameof(DisplayModeDescription));
            }
        }
    }

    private bool _modeLine;
    public bool ModeLine
    {
        get => _modeLine;
        set
        {
            if (SetProperty(ref _modeLine, value) && value)
            {
                ModeFill = false;
                ModeBorder = false;
                OnPropertyChanged(nameof(DisplayModeDescription));
            }
        }
    }

    public string DisplayModeDescription
    {
        get
        {
            if (ModeFill) return "Full colored bar across the top of each view window";
            if (ModeBorder) return "Thin colored border around the view window edges";
            return "Subtle colored line at the bottom of each view";
        }
    }

    // ── Opacity ───────────────────────────────────────────────

    private double _opacity = 0.85;
    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

    // ── Bar Thickness ─────────────────────────────────────────

    private double _barThickness = 4.0;
    public double BarThickness
    {
        get => _barThickness;
        set => SetProperty(ref _barThickness, value);
    }

    // ── Status ────────────────────────────────────────────────

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    // ── Commands ──────────────────────────────────────────────

    public ICommand RefreshCommand { get; }
    public ICommand RandomizeColorsCommand { get; }
    public ICommand CloseCommand { get; }

    public Action? CloseAction { get; set; }

    public ColorCoderViewModel(UIApplication uiApp)
    {
        _uiApp = uiApp;

        // Fill preset colors
        foreach (var c in Palette)
            PresetColors.Add(c);

        RefreshCommand = new RelayCommand(_ => LoadDocuments());
        RandomizeColorsCommand = new RelayCommand(_ => RandomizeColors());
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        LoadDocuments();
    }

    private void LoadDocuments()
    {
        Documents.Clear();
        int colorIdx = 0;

        try
        {
            var app = _uiApp.Application;

            foreach (Autodesk.Revit.DB.Document doc in app.Documents)
            {
                if (doc.IsFamilyDocument) continue;

                string name = string.IsNullOrEmpty(doc.Title) ? "Untitled" : doc.Title;
                string path = string.IsNullOrEmpty(doc.PathName) ? "(not saved)" : doc.PathName;
                bool isActive = _uiApp.ActiveUIDocument?.Document?.Title == doc.Title;

                // Count open views for this document
                int viewCount = 0;
                try
                {
                    var uiDoc = new UIDocument(doc);
                    var openViews = uiDoc.GetOpenUIViews();
                    viewCount = openViews?.Count ?? 0;
                }
                catch { viewCount = 0; }

                Documents.Add(new DocumentColorInfo
                {
                    DocumentName = name,
                    FilePath = path,
                    AssignedColor = Palette[colorIdx % Palette.Length],
                    IsActive = isActive,
                    IsEnabled = true,
                    ViewCount = viewCount
                });

                colorIdx++;
            }

            StatusMessage = $"{Documents.Count} document(s) found";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void RandomizeColors()
    {
        var rng = new Random();
        var shuffled = Palette.OrderBy(_ => rng.Next()).ToArray();
        int i = 0;
        foreach (var doc in Documents)
        {
            doc.AssignedColor = shuffled[i % shuffled.Length];
            i++;
        }
        StatusMessage = "Colors randomized";
    }
}
