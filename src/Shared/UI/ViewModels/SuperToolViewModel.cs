using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class SuperToolViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    public string DocumentName { get; }

    // ── Active Tab ──────────────────────────────────────────
    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsCropTab));
                OnPropertyChanged(nameof(IsFamilyTab));
                OnPropertyChanged(nameof(IsGridTab));
                OnPropertyChanged(nameof(IsLevelTab));
                LoadCurrentTab();
            }
        }
    }

    public bool IsCropTab => SelectedTabIndex == 0;
    public bool IsFamilyTab => SelectedTabIndex == 1;
    public bool IsGridTab => SelectedTabIndex == 2;
    public bool IsLevelTab => SelectedTabIndex == 3;

    // ── Status ──────────────────────────────────────────────
    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ═══════════════════════════════════════════════════════════
    // COPY CROP
    // ═══════════════════════════════════════════════════════════
    public ObservableCollection<CropViewInfo> CropViews { get; } = new();
    private ICollectionView? _cropViewsView;
    public ICollectionView? CropViewsView
    {
        get => _cropViewsView;
        private set => SetProperty(ref _cropViewsView, value);
    }

    private CropViewInfo? _cropSourceView;
    public CropViewInfo? CropSourceView
    {
        get => _cropSourceView;
        set => SetProperty(ref _cropSourceView, value);
    }

    private string _cropSearch = string.Empty;
    public string CropSearch
    {
        get => _cropSearch;
        set { if (SetProperty(ref _cropSearch, value)) CropViewsView?.Refresh(); }
    }

    public ICommand CopyCropCommand { get; }
    public ICommand CropSelectAllCommand { get; }
    public ICommand CropSelectNoneCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // FAMILIES
    // ═══════════════════════════════════════════════════════════
    public ObservableCollection<FamilyInfo> Families { get; } = new();
    private ICollectionView? _familiesView;
    public ICollectionView? FamiliesView
    {
        get => _familiesView;
        private set => SetProperty(ref _familiesView, value);
    }

    private string _familySearch = string.Empty;
    public string FamilySearch
    {
        get => _familySearch;
        set { if (SetProperty(ref _familySearch, value)) FamiliesView?.Refresh(); }
    }

    private bool _showUnusedOnly;
    public bool ShowUnusedOnly
    {
        get => _showUnusedOnly;
        set { if (SetProperty(ref _showUnusedOnly, value)) FamiliesView?.Refresh(); }
    }

    public ICommand DeleteFamiliesCommand { get; }
    public ICommand SelectUnusedCommand { get; }
    public ICommand FamilySelectAllCommand { get; }
    public ICommand FamilySelectNoneCommand { get; }

    private int _familyCount;
    public int FamilyCount
    {
        get => _familyCount;
        set => SetProperty(ref _familyCount, value);
    }

    private int _unusedFamilyCount;
    public int UnusedFamilyCount
    {
        get => _unusedFamilyCount;
        set => SetProperty(ref _unusedFamilyCount, value);
    }

    // ═══════════════════════════════════════════════════════════
    // GRIDS
    // ═══════════════════════════════════════════════════════════
    public ObservableCollection<GridInfo> Grids { get; } = new();
    private ICollectionView? _gridsView;
    public ICollectionView? GridsView
    {
        get => _gridsView;
        private set => SetProperty(ref _gridsView, value);
    }

    private string _gridSearch = string.Empty;
    public string GridSearch
    {
        get => _gridSearch;
        set { if (SetProperty(ref _gridSearch, value)) GridsView?.Refresh(); }
    }

    private string _gridPrefix = string.Empty;
    public string GridPrefix
    {
        get => _gridPrefix;
        set => SetProperty(ref _gridPrefix, value);
    }

    private string _gridSuffix = string.Empty;
    public string GridSuffix
    {
        get => _gridSuffix;
        set => SetProperty(ref _gridSuffix, value);
    }

    public ICommand RenameGridsCommand { get; }
    public ICommand DeleteGridsCommand { get; }
    public ICommand GridSelectAllCommand { get; }
    public ICommand GridSelectNoneCommand { get; }
    public ICommand ApplyGridPrefixSuffixCommand { get; }

    // ═══════════════════════════════════════════════════════════
    // LEVELS
    // ═══════════════════════════════════════════════════════════
    public ObservableCollection<LevelInfo> Levels { get; } = new();
    private ICollectionView? _levelsView;
    public ICollectionView? LevelsView
    {
        get => _levelsView;
        private set => SetProperty(ref _levelsView, value);
    }

    private string _levelSearch = string.Empty;
    public string LevelSearch
    {
        get => _levelSearch;
        set { if (SetProperty(ref _levelSearch, value)) LevelsView?.Refresh(); }
    }

    private double _elevationOffset;
    public double ElevationOffset
    {
        get => _elevationOffset;
        set => SetProperty(ref _elevationOffset, value);
    }

    public ICommand RenameLevelsCommand { get; }
    public ICommand MoveLevelsCommand { get; }
    public ICommand DeleteLevelsCommand { get; }
    public ICommand LevelSelectAllCommand { get; }
    public ICommand LevelSelectNoneCommand { get; }
    public ICommand ApplyElevationOffsetCommand { get; }

    // ── Common ──────────────────────────────────────────────
    public ICommand RefreshCommand { get; }
    public ICommand CloseCommand { get; }
    public Action? CloseAction { get; set; }

    public SuperToolViewModel(IRevitService service)
    {
        _service = service;
        DocumentName = _service.GetDocumentName();

        // CopyCrop
        CopyCropCommand = new RelayCommand(_ => ExecuteCopyCrop(),
            _ => CropSourceView != null && CropViews.Any(v => v.IsSelected));
        CropSelectAllCommand = new RelayCommand(_ => { foreach (var v in CropViews) v.IsSelected = true; });
        CropSelectNoneCommand = new RelayCommand(_ => { foreach (var v in CropViews) v.IsSelected = false; });

        // Families
        DeleteFamiliesCommand = new RelayCommand(_ => ExecuteDeleteFamilies(),
            _ => Families.Any(f => f.IsSelected));
        SelectUnusedCommand = new RelayCommand(_ =>
        {
            foreach (var f in Families) f.IsSelected = f.IsUnused;
        });
        FamilySelectAllCommand = new RelayCommand(_ => { foreach (var f in Families) f.IsSelected = true; });
        FamilySelectNoneCommand = new RelayCommand(_ => { foreach (var f in Families) f.IsSelected = false; });

        // Grids
        RenameGridsCommand = new RelayCommand(_ => ExecuteRenameGrids(),
            _ => Grids.Any(g => g.WillChange));
        DeleteGridsCommand = new RelayCommand(_ => ExecuteDeleteGrids(),
            _ => Grids.Any(g => g.IsSelected));
        GridSelectAllCommand = new RelayCommand(_ => { foreach (var g in Grids) g.IsSelected = true; });
        GridSelectNoneCommand = new RelayCommand(_ => { foreach (var g in Grids) g.IsSelected = false; });
        ApplyGridPrefixSuffixCommand = new RelayCommand(_ => ApplyGridPrefixSuffix());

        // Levels
        RenameLevelsCommand = new RelayCommand(_ => ExecuteRenameLevels(),
            _ => Levels.Any(l => l.WillRename));
        MoveLevelsCommand = new RelayCommand(_ => ExecuteMoveLevels(),
            _ => Levels.Any(l => l.WillMove));
        DeleteLevelsCommand = new RelayCommand(_ => ExecuteDeleteLevels(),
            _ => Levels.Any(l => l.IsSelected));
        LevelSelectAllCommand = new RelayCommand(_ => { foreach (var l in Levels) l.IsSelected = true; });
        LevelSelectNoneCommand = new RelayCommand(_ => { foreach (var l in Levels) l.IsSelected = false; });
        ApplyElevationOffsetCommand = new RelayCommand(_ => ApplyElevationOffset());

        // Common
        RefreshCommand = new RelayCommand(_ => LoadCurrentTab());
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        LoadCurrentTab();
    }

    private void LoadCurrentTab()
    {
        switch (SelectedTabIndex)
        {
            case 0: LoadCropViews(); break;
            case 1: LoadFamilies(); break;
            case 2: LoadGrids(); break;
            case 3: LoadLevels(); break;
        }
    }

    // ── CopyCrop ────────────────────────────────────────────

    private void LoadCropViews()
    {
        CropViews.Clear();
        CropSourceView = null;
        foreach (var v in _service.GetCroppableViews())
            CropViews.Add(v);

        CropViewsView = CollectionViewSource.GetDefaultView(CropViews);
        CropViewsView.Filter = o =>
        {
            if (o is not CropViewInfo v) return false;
            if (string.IsNullOrWhiteSpace(CropSearch)) return true;
            return v.Name.IndexOf(CropSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        };
        StatusMessage = $"{CropViews.Count} croppable views loaded";
    }

    private void ExecuteCopyCrop()
    {
        if (CropSourceView == null) return;
        var targets = CropViews.Where(v => v.IsSelected && v.ElementId != CropSourceView.ElementId)
            .Select(v => v.ElementId).ToList();
        if (targets.Count == 0) return;

        int count = _service.CopyCropRegion(CropSourceView.ElementId, targets);

        foreach (var v in CropViews.Where(v => targets.Contains(v.ElementId)))
            v.Status = "Copied";

        StatusMessage = $"Crop region copied to {count} view(s)";
    }

    // ── Families ────────────────────────────────────────────

    private void LoadFamilies()
    {
        Families.Clear();
        foreach (var f in _service.GetAllFamilies())
            Families.Add(f);

        FamiliesView = CollectionViewSource.GetDefaultView(Families);
        FamiliesView.Filter = o =>
        {
            if (o is not FamilyInfo f) return false;
            if (ShowUnusedOnly && !f.IsUnused) return false;
            if (string.IsNullOrWhiteSpace(FamilySearch)) return true;
            var s = FamilySearch.Trim();
            return f.FamilyName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                || f.Category.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;
        };

        FamilyCount = Families.Count;
        UnusedFamilyCount = Families.Count(f => f.IsUnused);
        StatusMessage = $"{FamilyCount} families loaded ({UnusedFamilyCount} unused)";
    }

    private void ExecuteDeleteFamilies()
    {
        var ids = Families.Where(f => f.IsSelected).Select(f => f.ElementId).ToList();
        int count = _service.DeleteFamilies(ids);
        StatusMessage = $"{count} family(ies) deleted";
        LoadFamilies();
    }

    // ── Grids ───────────────────────────────────────────────

    private void LoadGrids()
    {
        Grids.Clear();
        foreach (var g in _service.GetAllGrids())
        {
            g.NewName = g.Name;
            Grids.Add(g);
        }

        GridsView = CollectionViewSource.GetDefaultView(Grids);
        GridsView.Filter = o =>
        {
            if (o is not GridInfo g) return false;
            if (string.IsNullOrWhiteSpace(GridSearch)) return true;
            return g.Name.IndexOf(GridSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        };
        StatusMessage = $"{Grids.Count} grids loaded";
    }

    private void ApplyGridPrefixSuffix()
    {
        foreach (var g in Grids.Where(g => g.IsSelected))
            g.NewName = $"{GridPrefix}{g.Name}{GridSuffix}";
    }

    private void ExecuteRenameGrids()
    {
        var renames = Grids.Where(g => g.WillChange)
            .ToDictionary(g => g.ElementId, g => g.NewName);
        int count = _service.RenameGrids(renames);
        StatusMessage = $"{count} grid(s) renamed";
        LoadGrids();
    }

    private void ExecuteDeleteGrids()
    {
        var ids = Grids.Where(g => g.IsSelected).Select(g => g.ElementId).ToList();
        int count = _service.DeleteGrids(ids);
        StatusMessage = $"{count} grid(s) deleted";
        LoadGrids();
    }

    // ── Levels ──────────────────────────────────────────────

    private void LoadLevels()
    {
        Levels.Clear();
        foreach (var l in _service.GetAllLevels())
        {
            l.NewName = l.Name;
            l.NewElevation = l.Elevation;
            Levels.Add(l);
        }

        LevelsView = CollectionViewSource.GetDefaultView(Levels);
        LevelsView.Filter = o =>
        {
            if (o is not LevelInfo l) return false;
            if (string.IsNullOrWhiteSpace(LevelSearch)) return true;
            return l.Name.IndexOf(LevelSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        };
        StatusMessage = $"{Levels.Count} levels loaded";
    }

    private void ApplyElevationOffset()
    {
        foreach (var l in Levels.Where(l => l.IsSelected))
            l.NewElevation = l.Elevation + ElevationOffset;
    }

    private void ExecuteRenameLevels()
    {
        var renames = Levels.Where(l => l.WillRename)
            .ToDictionary(l => l.ElementId, l => l.NewName);
        int count = _service.RenameLevels(renames);
        StatusMessage = $"{count} level(s) renamed";
        LoadLevels();
    }

    private void ExecuteMoveLevels()
    {
        var moves = Levels.Where(l => l.WillMove)
            .ToDictionary(l => l.ElementId, l => l.NewElevation);
        int count = _service.MoveLevels(moves);
        StatusMessage = $"{count} level(s) moved";
        LoadLevels();
    }

    private void ExecuteDeleteLevels()
    {
        var ids = Levels.Where(l => l.IsSelected).Select(l => l.ElementId).ToList();
        int count = _service.DeleteLevels(ids);
        StatusMessage = $"{count} level(s) deleted";
        LoadLevels();
    }
}
