using System.Windows.Input;
using Autodesk.Revit.UI;
using AllO.Core;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class ColorCoderViewModel : ViewModelBase
{
    private readonly UIApplication _uiApp;

    private bool _modeTabs = true;
    public bool ModeTabs
    {
        get => _modeTabs;
        set
        {
            if (!SetProperty(ref _modeTabs, value)) return;
            if (!value) return;
            _modeFill = false;
            _modeBorder = false;
            _modeLine = false;
            OnPropertyChanged(nameof(ModeFill));
            OnPropertyChanged(nameof(ModeBorder));
            OnPropertyChanged(nameof(ModeLine));
            ColorCoderState.DisplayMode = ColorCoderDisplayMode.Tabs;
            OnPropertyChanged(nameof(DisplayModeDescription));
            ColorCoderOverlayHost.RefreshIfActive();
        }
    }

    private bool _modeFill;
    public bool ModeFill
    {
        get => _modeFill;
        set
        {
            if (!SetProperty(ref _modeFill, value)) return;
            if (!value) return;
            _modeTabs = false;
            _modeBorder = false;
            _modeLine = false;
            OnPropertyChanged(nameof(ModeTabs));
            OnPropertyChanged(nameof(ModeBorder));
            OnPropertyChanged(nameof(ModeLine));
            ColorCoderState.DisplayMode = ColorCoderDisplayMode.FillBar;
            OnPropertyChanged(nameof(DisplayModeDescription));
            ColorCoderOverlayHost.RefreshIfActive();
        }
    }

    private bool _modeBorder;
    public bool ModeBorder
    {
        get => _modeBorder;
        set
        {
            if (!SetProperty(ref _modeBorder, value)) return;
            if (!value) return;
            _modeTabs = false;
            _modeFill = false;
            _modeLine = false;
            OnPropertyChanged(nameof(ModeTabs));
            OnPropertyChanged(nameof(ModeFill));
            OnPropertyChanged(nameof(ModeLine));
            ColorCoderState.DisplayMode = ColorCoderDisplayMode.Border;
            OnPropertyChanged(nameof(DisplayModeDescription));
            ColorCoderOverlayHost.RefreshIfActive();
        }
    }

    private bool _modeLine;
    public bool ModeLine
    {
        get => _modeLine;
        set
        {
            if (!SetProperty(ref _modeLine, value)) return;
            if (!value) return;
            _modeTabs = false;
            _modeFill = false;
            _modeBorder = false;
            OnPropertyChanged(nameof(ModeTabs));
            OnPropertyChanged(nameof(ModeFill));
            OnPropertyChanged(nameof(ModeBorder));
            ColorCoderState.DisplayMode = ColorCoderDisplayMode.BottomLine;
            OnPropertyChanged(nameof(DisplayModeDescription));
            ColorCoderOverlayHost.RefreshIfActive();
        }
    }

    public string DisplayModeDescription
    {
        get
        {
            if (ModeTabs) return "Paints the native view tabs. Opacity controls the tint; inactive tabs are dimmed automatically.";
            if (ModeFill) return "Soft glow from the top edge (diffuse light wash, not a hard line).";
            if (ModeBorder) return "Outline around the entire view window.";
            return "Soft glow from the bottom edge.";
        }
    }

    private double _opacity;
    public double Opacity
    {
        get => _opacity;
        set
        {
            if (!SetProperty(ref _opacity, value)) return;
            ColorCoderState.Opacity = value;
            ColorCoderOverlayHost.RefreshIfActive();
        }
    }

    private double _barThickness;
    public double BarThickness
    {
        get => _barThickness;
        set
        {
            if (!SetProperty(ref _barThickness, value)) return;
            ColorCoderState.BarThicknessDip = value;
            ColorCoderOverlayHost.RefreshIfActive();
        }
    }

    public string StatusMessage { get; private set; } = string.Empty;

    public ICommand CloseCommand { get; }
    public ICommand ResetCommand { get; }

    public Action? CloseAction { get; set; }

    public ColorCoderViewModel(UIApplication uiApp)
    {
        _uiApp = uiApp;

        _modeTabs = ColorCoderState.DisplayMode == ColorCoderDisplayMode.Tabs;
        _modeFill = ColorCoderState.DisplayMode == ColorCoderDisplayMode.FillBar;
        _modeBorder = ColorCoderState.DisplayMode == ColorCoderDisplayMode.Border;
        _modeLine = ColorCoderState.DisplayMode == ColorCoderDisplayMode.BottomLine;

        _opacity = ColorCoderState.Opacity;
        _barThickness = ColorCoderState.BarThicknessDip;

        StatusMessage = "Adjust how each open document is highlighted, then Close. Reset turns Color Coder off and removes all tints.";

        CloseCommand = new RelayCommand(_ =>
        {
            ColorCoderOverlayHost.Refresh(_uiApp);
            CloseAction?.Invoke();
        });

        ResetCommand = new RelayCommand(_ =>
        {
            ColorCoderState.DeactivateAndClear();
            ColorCoderOverlayHost.SetTimerEnabled(false);
            ColorCoderOverlayHost.ClearOverlays();
            CloseAction?.Invoke();
        });
    }
}
