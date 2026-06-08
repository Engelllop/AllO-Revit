using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Data;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class SuperToolViewModel : ViewModelBase
{
    private readonly IRevitService _service;
    private readonly Dispatcher _dispatcher;

    public string DocumentName { get; }

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsCropTab));
                OnPropertyChanged(nameof(IsGridTab));
                OnPropertyChanged(nameof(IsLevelTab));
                LoadCurrentTab();
            }
        }
    }

    private bool _showNav = true;
    public bool ShowNav
    {
        get => _showNav;
        set => SetProperty(ref _showNav, value);
    }

    public bool IsCropTab => SelectedTabIndex == 0;
    public bool IsGridTab => SelectedTabIndex == 1;
    public bool IsLevelTab => SelectedTabIndex == 2;

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private bool _showProgress;
    public bool ShowProgress
    {
        get => _showProgress;
        set => SetProperty(ref _showProgress, value);
    }

    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    private double _progressMax = 100;
    public double ProgressMax
    {
        get => _progressMax;
        set => SetProperty(ref _progressMax, value);
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
        set
        {
            if (SetProperty(ref _cropSourceView, value))
                CommandManager.InvalidateRequerySuggested();
        }
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
    // GRIDS
    // ═══════════════════════════════════════════════════════════
    public ObservableCollection<GridInfo> Grids { get; } = new();
    public ObservableCollection<LinkDocumentInfo> ReferenceLinks { get; } = new();

    private LinkDocumentInfo? _selectedReferenceLink;
    /// <summary>Linked model used as reference for grid/level copy and mismatch checks.</summary>
    public LinkDocumentInfo? SelectedReferenceLink
    {
        get => _selectedReferenceLink;
        set
        {
            if (SetProperty(ref _selectedReferenceLink, value))
            {
                if (IsGridTab) ApplyGridSyncWarnings();
                else if (IsLevelTab) ApplyLevelSyncWarnings();
            }
        }
    }

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
    public ICommand CopyGridsFromLinkCommand { get; }
    public ICommand CopyNewGridsOnlyCommand { get; }
    public ICommand SyncGridsFromLinkCommand { get; }

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
    public ICommand CopyLevelsFromLinkCommand { get; }
    public ICommand CopyNewLevelsOnlyCommand { get; }
    public ICommand SyncLevelsFromLinkCommand { get; }

    public ICommand RefreshCommand { get; }
    public ICommand CloseCommand { get; }
    public Action? CloseAction { get; set; }

    public SuperToolViewModel(IRevitService service)
    {
        _service = service;
        _dispatcher = Dispatcher.CurrentDispatcher;
        DocumentName = _service.GetDocumentName();

        CopyCropCommand = new RelayCommand(_ => ExecuteCopyCrop(),
            _ => CropSourceView != null && CropViews.Any(v => v.IsSelected && v.ElementId != CropSourceView.ElementId));
        CropSelectAllCommand = new RelayCommand(_ => { foreach (var v in CropViews) v.IsSelected = true; });
        CropSelectNoneCommand = new RelayCommand(_ => { foreach (var v in CropViews) v.IsSelected = false; });

        RenameGridsCommand = new RelayCommand(_ => ExecuteRenameGrids(),
            _ => Grids.Any(g => g.WillChange));
        DeleteGridsCommand = new RelayCommand(_ => ExecuteDeleteGrids(),
            _ => Grids.Any(g => g.IsSelected));
        GridSelectAllCommand = new RelayCommand(_ => { foreach (var g in Grids) g.IsSelected = true; });
        GridSelectNoneCommand = new RelayCommand(_ => { foreach (var g in Grids) g.IsSelected = false; });
        ApplyGridPrefixSuffixCommand = new RelayCommand(_ => ApplyGridPrefixSuffix());

        CopyGridsFromLinkCommand = new RelayCommand(_ => ExecuteCopyGridsFromLink(false),
            _ => SelectedReferenceLink != null && SelectedReferenceLink.IsLoaded);
        CopyNewGridsOnlyCommand = new RelayCommand(_ => ExecuteCopyGridsFromLink(true),
            _ => SelectedReferenceLink != null && SelectedReferenceLink.IsLoaded);
        SyncGridsFromLinkCommand = new RelayCommand(_ => ExecuteSyncGridsFromLink(),
            _ => SelectedReferenceLink != null && SelectedReferenceLink.IsLoaded);

        RenameLevelsCommand = new RelayCommand(_ => ExecuteRenameLevels(),
            _ => Levels.Any(l => l.WillRename));
        MoveLevelsCommand = new RelayCommand(_ => ExecuteMoveLevels(),
            _ => Levels.Any(l => l.WillMove));
        DeleteLevelsCommand = new RelayCommand(_ => ExecuteDeleteLevels(),
            _ => Levels.Any(l => l.IsSelected));
        LevelSelectAllCommand = new RelayCommand(_ => { foreach (var l in Levels) l.IsSelected = true; });
        LevelSelectNoneCommand = new RelayCommand(_ => { foreach (var l in Levels) l.IsSelected = false; });
        ApplyElevationOffsetCommand = new RelayCommand(_ => ApplyElevationOffset());

        CopyLevelsFromLinkCommand = new RelayCommand(_ => ExecuteCopyLevelsFromLink(false),
            _ => SelectedReferenceLink != null && SelectedReferenceLink.IsLoaded);
        CopyNewLevelsOnlyCommand = new RelayCommand(_ => ExecuteCopyLevelsFromLink(true),
            _ => SelectedReferenceLink != null && SelectedReferenceLink.IsLoaded);
        SyncLevelsFromLinkCommand = new RelayCommand(_ => ExecuteSyncLevelsFromLink(),
            _ => SelectedReferenceLink != null && SelectedReferenceLink.IsLoaded);

        RefreshCommand = new RelayCommand(_ => LoadCurrentTab());
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        LoadCurrentTab();
    }

    private void ForceUiUpdate()
    {
        _dispatcher.Invoke(() => { }, DispatcherPriority.Render);
    }

    private void LoadReferenceLinks()
    {
        int? keepId = SelectedReferenceLink?.LinkInstanceId;
        ReferenceLinks.Clear();
        foreach (var link in _service.GetLinkedDocuments())
            ReferenceLinks.Add(link);

        if (keepId.HasValue)
        {
            SelectedReferenceLink = ReferenceLinks.FirstOrDefault(l => l.LinkInstanceId == keepId.Value);
        }
        if (SelectedReferenceLink == null && ReferenceLinks.Count > 0)
            SelectedReferenceLink = ReferenceLinks.FirstOrDefault(l => l.IsLoaded) ?? ReferenceLinks[0];
    }

    private void LoadCurrentTab()
    {
        switch (SelectedTabIndex)
        {
            case 0: LoadCropViews(); break;
            case 1: LoadGrids(); break;
            case 2: LoadLevels(); break;
        }
    }

    private void LoadCropViews()
    {
        CropViews.Clear();
        CropSourceView = null;
        foreach (var v in _service.GetCroppableViews())
        {
            v.Status = string.Empty;
            CropViews.Add(v);
        }

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
        var targets = CropViews.Where(v => v.IsSelected && v.ElementId != CropSourceView.ElementId).ToList();
        if (targets.Count == 0) return;

        foreach (var v in targets)
            v.Status = string.Empty;

        ShowProgress = true;
        ProgressMax = targets.Count;
        ProgressValue = 0;
        StatusMessage = "Applying crop region...";
        ForceUiUpdate();

        int okCount = 0;
        int step = 0;
        try
        {
            foreach (var v in targets)
            {
                step++;
                v.Status = "Applying...";
                StatusMessage = $"Copy crop: {v.Name} ({step}/{targets.Count})";
                ProgressValue = step - 1;
                ForceUiUpdate();

                bool ok = _service.CopyCropToSingleView(CropSourceView.ElementId, v.ElementId);
                v.Status = ok ? "Done" : "Failed";
                if (ok) okCount++;
                ProgressValue = step;
                ForceUiUpdate();
            }

            ProgressValue = ProgressMax;
            StatusMessage = okCount == targets.Count
                ? $"Crop applied to {okCount} view(s)."
                : $"Crop applied to {okCount} of {targets.Count} view(s); some failed.";
        }
        finally
        {
            ShowProgress = false;
        }
    }

    private void LoadGrids()
    {
        LoadReferenceLinks();
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
        ApplyGridSyncWarnings();
        StatusMessage = $"{Grids.Count} grids loaded";
    }

    private void ApplyGridSyncWarnings()
    {
        if (SelectedReferenceLink == null || !SelectedReferenceLink.IsLoaded)
        {
            foreach (var g in Grids)
            {
                g.SyncWarning = null;
                g.HasSyncMismatch = false;
            }
            return;
        }

        _service.PopulateGridSyncWarnings(SelectedReferenceLink.LinkInstanceId, Grids.ToList());
    }

    private void ExecuteCopyGridsFromLink(bool onlyNewNames)
    {
        if (SelectedReferenceLink == null || !SelectedReferenceLink.IsLoaded) return;
        int n = _service.CopyGridsFromLink(SelectedReferenceLink.LinkInstanceId, onlyNewNames);
        StatusMessage = onlyNewNames
            ? $"Copied {n} new grid(s) from link (names not in host)."
            : $"Copied {n} grid(s) from link.";
        LoadGrids();
    }

    private void ExecuteSyncGridsFromLink()
    {
        if (SelectedReferenceLink == null || !SelectedReferenceLink.IsLoaded) return;
        int n = _service.SyncGridsFromLink(SelectedReferenceLink.LinkInstanceId);
        StatusMessage = n > 0
            ? $"Synced {n} grid(s) with reference link (geometry updated)."
            : "No grid geometry changes were needed.";
        LoadGrids();
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

    private void LoadLevels()
    {
        LoadReferenceLinks();
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
        ApplyLevelSyncWarnings();
        StatusMessage = $"{Levels.Count} levels loaded";
    }

    private void ApplyLevelSyncWarnings()
    {
        if (SelectedReferenceLink == null || !SelectedReferenceLink.IsLoaded)
        {
            foreach (var l in Levels)
            {
                l.SyncWarning = null;
                l.HasSyncMismatch = false;
            }
            return;
        }

        _service.PopulateLevelSyncWarnings(SelectedReferenceLink.LinkInstanceId, Levels.ToList());
    }

    private void ExecuteCopyLevelsFromLink(bool onlyNewNames)
    {
        if (SelectedReferenceLink == null || !SelectedReferenceLink.IsLoaded) return;
        int n = _service.CopyLevelsFromLink(SelectedReferenceLink.LinkInstanceId, onlyNewNames);
        StatusMessage = onlyNewNames
            ? $"Copied {n} new level(s) from link (names not in host)."
            : $"Copied {n} level(s) from link.";
        LoadLevels();
    }

    private void ExecuteSyncLevelsFromLink()
    {
        if (SelectedReferenceLink == null || !SelectedReferenceLink.IsLoaded) return;
        int n = _service.SyncLevelsFromLink(SelectedReferenceLink.LinkInstanceId);
        StatusMessage = n > 0
            ? $"Moved {n} level(s) to match reference link elevations."
            : "No level elevation changes were needed.";
        LoadLevels();
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
