using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class AlignViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    public ObservableCollection<AlignableElementInfo> Elements { get; } = new();
    public ObservableCollection<GridInfo> Grids { get; } = new();
    public ObservableCollection<LevelInfo> Levels { get; } = new();

    private ICollectionView? _elementsView;
    public ICollectionView? ElementsView
    {
        get => _elementsView;
        private set => SetProperty(ref _elementsView, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) ElementsView?.Refresh(); }
    }

    private GridInfo? _selectedGrid;
    public GridInfo? SelectedGrid
    {
        get => _selectedGrid;
        set => SetProperty(ref _selectedGrid, value);
    }

    private LevelInfo? _selectedLevel;
    public LevelInfo? SelectedLevel
    {
        get => _selectedLevel;
        set => SetProperty(ref _selectedLevel, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    private int _selectedCount;
    public int SelectedCount
    {
        get => _selectedCount;
        set => SetProperty(ref _selectedCount, value);
    }

    private string _statusMessage = "Select elements in Revit, then click Refresh";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // Alignment commands
    public ICommand AlignLeftCommand { get; }
    public ICommand AlignCenterCommand { get; }
    public ICommand AlignRightCommand { get; }
    public ICommand AlignTopCommand { get; }
    public ICommand AlignMiddleCommand { get; }
    public ICommand AlignBottomCommand { get; }
    public ICommand DistributeHCommand { get; }
    public ICommand DistributeVCommand { get; }
    public ICommand AlignToGridCommand { get; }
    public ICommand AlignToLevelCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand CloseCommand { get; }

    public Action? CloseAction { get; set; }

    public AlignViewModel(IRevitService service)
    {
        _service = service;

        AlignLeftCommand = new RelayCommand(_ => DoAlign("Left"), _ => SelectedCount > 0);
        AlignCenterCommand = new RelayCommand(_ => DoAlign("Center"), _ => SelectedCount > 0);
        AlignRightCommand = new RelayCommand(_ => DoAlign("Right"), _ => SelectedCount > 0);
        AlignTopCommand = new RelayCommand(_ => DoAlign("Top"), _ => SelectedCount > 0);
        AlignMiddleCommand = new RelayCommand(_ => DoAlign("Middle"), _ => SelectedCount > 0);
        AlignBottomCommand = new RelayCommand(_ => DoAlign("Bottom"), _ => SelectedCount > 0);
        DistributeHCommand = new RelayCommand(_ => DoDistribute("Horizontal"), _ => SelectedCount >= 3);
        DistributeVCommand = new RelayCommand(_ => DoDistribute("Vertical"), _ => SelectedCount >= 3);
        AlignToGridCommand = new RelayCommand(_ => DoAlignToGrid(), _ => SelectedCount > 0 && SelectedGrid != null);
        AlignToLevelCommand = new RelayCommand(_ => DoAlignToLevel(), _ => SelectedCount > 0 && SelectedLevel != null);
        RefreshCommand = new RelayCommand(_ => LoadAll());
        SelectAllCommand = new RelayCommand(_ => SetAll(true));
        SelectNoneCommand = new RelayCommand(_ => SetAll(false));
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        LoadAll();
    }

    private void LoadAll()
    {
        Elements.Clear();
        foreach (var el in _service.GetSelectedElements())
        {
            el.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AlignableElementInfo.IsSelected))
                    SelectedCount = Elements.Count(x => x.IsSelected);
            };
            Elements.Add(el);
        }
        ElementsView = CollectionViewSource.GetDefaultView(Elements);
        ElementsView.Filter = o => o is AlignableElementInfo e &&
            (string.IsNullOrWhiteSpace(SearchText) ||
             e.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
             e.Category.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);

        TotalCount = Elements.Count;
        SelectedCount = Elements.Count(x => x.IsSelected);

        Grids.Clear();
        foreach (var g in _service.GetAllGrids()) Grids.Add(g);

        Levels.Clear();
        foreach (var l in _service.GetAllLevels()) Levels.Add(l);

        StatusMessage = Elements.Count > 0
            ? $"{Elements.Count} element(s) loaded from selection"
            : "No elements selected — select elements in Revit and click Refresh";
    }

    private void SetAll(bool val) { foreach (var e in Elements) e.IsSelected = val; }

    private List<int> GetSelectedIds() =>
        Elements.Where(e => e.IsSelected).Select(e => e.ElementId).ToList();

    private void DoAlign(string mode)
    {
        int count = _service.AlignElements(GetSelectedIds(), mode);
        StatusMessage = $"Aligned {count} element(s) — {mode}";
    }

    private void DoDistribute(string dir)
    {
        int count = _service.DistributeElements(GetSelectedIds(), dir);
        StatusMessage = $"Distributed {count} element(s) — {dir}";
    }

    private void DoAlignToGrid()
    {
        if (SelectedGrid == null) return;
        int count = _service.AlignToGrid(GetSelectedIds(), SelectedGrid.ElementId);
        StatusMessage = $"Aligned {count} element(s) to Grid {SelectedGrid.Name}";
    }

    private void DoAlignToLevel()
    {
        if (SelectedLevel == null) return;
        int count = _service.AlignToLevel(GetSelectedIds(), SelectedLevel.ElementId);
        StatusMessage = $"Aligned {count} element(s) to Level {SelectedLevel.Name}";
    }
}
