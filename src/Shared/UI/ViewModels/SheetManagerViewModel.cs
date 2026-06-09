using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class SheetManagerViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    // ==============================================================
    //  NAVIGATION
    // ==============================================================

    private int _selectedNavIndex;
    public int SelectedNavIndex
    {
        get => _selectedNavIndex;
        set
        {
            if (SetProperty(ref _selectedNavIndex, value))
            {
                OnPropertyChanged(nameof(IsSheetListVisible));
                OnPropertyChanged(nameof(IsViewListVisible));
                OnPropertyChanged(nameof(IsRevisionsVisible));
                UpdateStatusCounts();
            }
        }
    }

    public bool IsSheetListVisible => SelectedNavIndex == 0;
    public bool IsViewListVisible => SelectedNavIndex == 1;
    public bool IsRevisionsVisible => SelectedNavIndex == 2;

    private bool _showNav = true;
    public bool ShowNav
    {
        get => _showNav;
        set => SetProperty(ref _showNav, value);
    }

    public ICommand NavSheetListCommand { get; }
    public ICommand NavViewListCommand { get; }
    public ICommand NavRevisionsCommand { get; }

    // ==============================================================
    //  SHEETS
    // ==============================================================

    public ObservableCollection<SheetInfo> Sheets { get; } = new();
    public ObservableCollection<TitleBlockInfo> TitleBlocks { get; } = new();

    private ICollectionView? _sheetsView;
    public ICollectionView? SheetsView
    {
        get => _sheetsView;
        private set => SetProperty(ref _sheetsView, value);
    }

    // ==============================================================
    //  VIEWS
    // ==============================================================

    public ObservableCollection<ViewInfo> Views { get; } = new();

    private ICollectionView? _viewsView;
    public ICollectionView? ViewsView
    {
        get => _viewsView;
        private set => SetProperty(ref _viewsView, value);
    }

    private string _viewSearchText = string.Empty;
    public string ViewSearchText
    {
        get => _viewSearchText;
        set
        {
            if (SetProperty(ref _viewSearchText, value))
                ViewsView?.Refresh();
        }
    }

    private int _viewSelectedCount;
    public int ViewSelectedCount
    {
        get => _viewSelectedCount;
        set => SetProperty(ref _viewSelectedCount, value);
    }

    // ==============================================================
    //  REVISIONS
    // ==============================================================

    public ObservableCollection<RevisionInfo> Revisions { get; } = new();

    private string _newRevDate = string.Empty;
    public string NewRevDate
    {
        get => _newRevDate;
        set => SetProperty(ref _newRevDate, value);
    }

    private string _newRevDescription = string.Empty;
    public string NewRevDescription
    {
        get => _newRevDescription;
        set => SetProperty(ref _newRevDescription, value);
    }

    private string _newRevIssuedBy = string.Empty;
    public string NewRevIssuedBy
    {
        get => _newRevIssuedBy;
        set => SetProperty(ref _newRevIssuedBy, value);
    }

    private string _newRevIssuedTo = string.Empty;
    public string NewRevIssuedTo
    {
        get => _newRevIssuedTo;
        set => SetProperty(ref _newRevIssuedTo, value);
    }

    private bool _isCreateRevisionOpen;
    public bool IsCreateRevisionOpen
    {
        get => _isCreateRevisionOpen;
        set => SetProperty(ref _isCreateRevisionOpen, value);
    }

    private int _revSelectedCount;
    public int RevSelectedCount
    {
        get => _revSelectedCount;
        set => SetProperty(ref _revSelectedCount, value);
    }

    // ==============================================================
    //  HEADER
    // ==============================================================

    private string _documentName = string.Empty;
    public string DocumentName
    {
        get => _documentName;
        set => SetProperty(ref _documentName, value);
    }

    // ==============================================================
    //  STATUS BAR
    // ==============================================================

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
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

    private int _affectedCount;
    public int AffectedCount
    {
        get => _affectedCount;
        set => SetProperty(ref _affectedCount, value);
    }

    // -- Search / Filter (Sheets) --------------------------------

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                SheetsView?.Refresh();
        }
    }

    // -- Find & Replace ------------------------------------------

    private bool _isFindReplaceOpen;
    public bool IsFindReplaceOpen
    {
        get => _isFindReplaceOpen;
        set => SetProperty(ref _isFindReplaceOpen, value);
    }

    private string _findText = string.Empty;
    public string FindText
    {
        get => _findText;
        set
        {
            if (SetProperty(ref _findText, value))
                UpdatePreview();
        }
    }

    private string _replaceText = string.Empty;
    public string ReplaceText
    {
        get => _replaceText;
        set
        {
            if (SetProperty(ref _replaceText, value))
                UpdatePreview();
        }
    }

    // -- Create Sheet Panel --------------------------------------

    private bool _isCreatePanelOpen;
    public bool IsCreatePanelOpen
    {
        get => _isCreatePanelOpen;
        set => SetProperty(ref _isCreatePanelOpen, value);
    }

    private TitleBlockInfo? _selectedTitleBlock;
    public TitleBlockInfo? SelectedTitleBlock
    {
        get => _selectedTitleBlock;
        set => SetProperty(ref _selectedTitleBlock, value);
    }

    private string _newSheetNumber = string.Empty;
    public string NewSheetNumber
    {
        get => _newSheetNumber;
        set => SetProperty(ref _newSheetNumber, value);
    }

    private string _newSheetName = string.Empty;
    public string NewSheetName
    {
        get => _newSheetName;
        set => SetProperty(ref _newSheetName, value);
    }

    // -- Batch renumber ------------------------------------------
    private string _renumberPattern = "A-{nn}";
    public string RenumberPattern
    {
        get => _renumberPattern;
        set => SetProperty(ref _renumberPattern, value);
    }

    private int _renumberStart = 1;
    public int RenumberStart
    {
        get => _renumberStart;
        set => SetProperty(ref _renumberStart, value);
    }

    // ==============================================================
    //  COMMANDS -- Sheets
    // ==============================================================

    public ICommand RefreshCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand DuplicateSelectedCommand { get; }
    public ICommand ToggleFindReplaceCommand { get; }
    public ICommand ApplyRenameCommand { get; }
    public ICommand ToggleCreatePanelCommand { get; }
    public ICommand CreateSheetCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ChangeTitleBlockCommand { get; }
    public ICommand RenumberCommand { get; }
    public ICommand ExportIndexCommand { get; }
    public ICommand AssignRevisionCommand { get; }

    // ==============================================================
    //  COMMANDS -- Views
    // ==============================================================

    public ICommand ViewSelectAllCommand { get; }
    public ICommand ViewSelectNoneCommand { get; }
    public ICommand DeleteViewsCommand { get; }
    public ICommand RenameViewsCommand { get; }

    // ==============================================================
    //  COMMANDS -- Revisions
    // ==============================================================

    public ICommand ToggleCreateRevisionCommand { get; }
    public ICommand CreateRevisionCommand { get; }
    public ICommand DeleteRevisionsCommand { get; }
    public ICommand RevSelectAllCommand { get; }
    public ICommand RevSelectNoneCommand { get; }
    public ICommand ToggleRevisionIssuedCommand { get; }

    public Action? CloseAction { get; set; }

    // ==============================================================
    //  CONSTRUCTOR
    // ==============================================================

    public SheetManagerViewModel(IRevitService service, int initialPanel = 0)
    {
        _service = service;
        DocumentName = _service.GetDocumentName();
        SelectedNavIndex = initialPanel;

        // Navigation
        NavSheetListCommand = new RelayCommand(_ => SelectedNavIndex = 0);
        NavViewListCommand = new RelayCommand(_ => SelectedNavIndex = 1);
        NavRevisionsCommand = new RelayCommand(_ => SelectedNavIndex = 2);

        // Sheet commands
        RefreshCommand = new RelayCommand(_ => RefreshAll());
        SelectAllCommand = new RelayCommand(_ => SetAllSheetsSelected(true));
        SelectNoneCommand = new RelayCommand(_ => SetAllSheetsSelected(false));

        DeleteSelectedCommand = new RelayCommand(
            _ => ExecuteDeleteSelected(),
            _ => SelectedCount > 0);

        DuplicateSelectedCommand = new RelayCommand(
            _ => ExecuteDuplicateSelected(),
            _ => SelectedCount > 0);

        ToggleFindReplaceCommand = new RelayCommand(_ =>
        {
            IsFindReplaceOpen = !IsFindReplaceOpen;
            if (!IsFindReplaceOpen) ResetPreviews();
        });

        ApplyRenameCommand = new RelayCommand(
            _ => ExecuteApplyRename(),
            _ => AffectedCount > 0);

        ToggleCreatePanelCommand = new RelayCommand(_ =>
            IsCreatePanelOpen = !IsCreatePanelOpen);

        CreateSheetCommand = new RelayCommand(
            _ => ExecuteCreateSheet(),
            _ => SelectedTitleBlock != null
                 && !string.IsNullOrWhiteSpace(NewSheetNumber)
                 && !string.IsNullOrWhiteSpace(NewSheetName));

        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        ChangeTitleBlockCommand = new RelayCommand(param =>
        {
            if (param is SheetInfo sheet)
                ExecuteChangeTitleBlock(sheet);
        });

        RenumberCommand = new RelayCommand(_ => ExecuteRenumber(), _ => SelectedCount > 0);
        ExportIndexCommand = new RelayCommand(_ => ExecuteExportIndex(), _ => Sheets.Count > 0);
        AssignRevisionCommand = new RelayCommand(_ => ExecuteAssignRevision(),
            _ => SelectedCount > 0 && Revisions.Any(r => r.IsSelected));

        // View commands
        ViewSelectAllCommand = new RelayCommand(_ => SetAllViewsSelected(true));
        ViewSelectNoneCommand = new RelayCommand(_ => SetAllViewsSelected(false));
        DeleteViewsCommand = new RelayCommand(
            _ => ExecuteDeleteViews(),
            _ => ViewSelectedCount > 0);
        RenameViewsCommand = new RelayCommand(
            _ => ExecuteRenameViews(),
            _ => Views.Any(v => v.IsSelected));

        // Revision commands
        ToggleCreateRevisionCommand = new RelayCommand(_ =>
            IsCreateRevisionOpen = !IsCreateRevisionOpen);

        CreateRevisionCommand = new RelayCommand(
            _ => ExecuteCreateRevision(),
            _ => !string.IsNullOrWhiteSpace(NewRevDescription));

        DeleteRevisionsCommand = new RelayCommand(
            _ => ExecuteDeleteRevisions(),
            _ => RevSelectedCount > 0);

        RevSelectAllCommand = new RelayCommand(_ => SetAllRevisionsSelected(true));
        RevSelectNoneCommand = new RelayCommand(_ => SetAllRevisionsSelected(false));

        ToggleRevisionIssuedCommand = new RelayCommand(param =>
        {
            if (param is RevisionInfo rev)
                ExecuteToggleRevisionIssued(rev);
        });

        // Load data
        LoadTitleBlocks();
        LoadSheets();
        LoadViews();
        LoadRevisions();
    }

    // ==============================================================
    //  DATA LOADING
    // ==============================================================

    private void RefreshAll()
    {
        LoadSheets();
        LoadViews();
        LoadRevisions();
    }

    private void LoadSheets()
    {
        Sheets.Clear();
        foreach (var sheet in _service.GetAllSheets())
        {
            sheet.PropertyChanged += Sheet_PropertyChanged;
            Sheets.Add(sheet);
        }

        SheetsView = CollectionViewSource.GetDefaultView(Sheets);
        SheetsView.Filter = FilterSheet;

        if (IsSheetListVisible) UpdateStatusCounts();
        StatusMessage = $"{Sheets.Count} sheet(s) loaded";
    }

    private void LoadTitleBlocks()
    {
        TitleBlocks.Clear();
        foreach (var tb in _service.GetTitleBlocks())
            TitleBlocks.Add(tb);

        if (TitleBlocks.Count > 0)
            SelectedTitleBlock = TitleBlocks[0];
    }

    private void LoadViews()
    {
        Views.Clear();
        foreach (var view in _service.GetAllViews())
        {
            view.PropertyChanged += View_PropertyChanged;
            Views.Add(view);
        }

        ViewsView = CollectionViewSource.GetDefaultView(Views);
        ViewsView.Filter = FilterView;
        ViewSelectedCount = 0;

        if (IsViewListVisible) UpdateStatusCounts();
    }

    private void LoadRevisions()
    {
        Revisions.Clear();
        foreach (var rev in _service.GetAllRevisions())
        {
            rev.PropertyChanged += Rev_PropertyChanged;
            Revisions.Add(rev);
        }
        RevSelectedCount = 0;

        if (IsRevisionsVisible) UpdateStatusCounts();
    }

    private void UpdateStatusCounts()
    {
        if (IsSheetListVisible)
        {
            TotalCount = Sheets.Count;
            SelectedCount = Sheets.Count(s => s.IsSelected);
        }
        else if (IsViewListVisible)
        {
            TotalCount = Views.Count;
            SelectedCount = ViewSelectedCount;
        }
        else if (IsRevisionsVisible)
        {
            TotalCount = Revisions.Count;
            SelectedCount = RevSelectedCount;
        }
    }

    // ==============================================================
    //  BATCH: renumber / export / assign revision
    // ==============================================================

    private IEnumerable<SheetInfo> SelectedSheetsInOrder()
        => (SheetsView?.Cast<SheetInfo>() ?? Sheets).Where(s => s.IsSelected);

    private void ExecuteRenumber()
    {
        var selected = SelectedSheetsInOrder().ToList();
        if (selected.Count == 0) return;

        var renumbers = new Dictionary<int, string>();
        int n = RenumberStart;
        foreach (var s in selected)
            renumbers[s.ElementId] = Helpers.SheetNaming.ExpandNumberPattern(RenumberPattern, n++);

        int done = _service.RenumberSheets(renumbers);
        LoadSheets();
        StatusMessage = $"Renumbered {done} sheet(s).";
    }

    private void ExecuteExportIndex()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export sheet index",
            Filter = "CSV file|*.csv",
            FileName = "Sheet Index.csv"
        };
        if (dlg.ShowDialog() != true) return;

        var rows = (SheetsView?.Cast<SheetInfo>() ?? Sheets).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("Number,Name,Title Block,Designed By,Approved By,Issue Date");
        string Csv(string? v) => Helpers.SheetNaming.CsvEscape(v);
        foreach (var s in rows)
            sb.AppendLine(string.Join(",",
                Csv(s.SheetNumber), Csv(s.OriginalName), Csv(s.TitleBlockName),
                Csv(s.DesignedBy), Csv(s.ApprovedBy), Csv(s.SheetIssueDate)));

        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
        StatusMessage = $"Exported {rows.Count} sheet(s) to CSV.";
    }

    private void ExecuteAssignRevision()
    {
        var rev = Revisions.FirstOrDefault(r => r.IsSelected);
        var sheetIds = Sheets.Where(s => s.IsSelected).Select(s => s.ElementId).ToList();
        if (rev == null || sheetIds.Count == 0)
        {
            StatusMessage = "Select a revision and at least one sheet.";
            return;
        }
        int n = _service.AddRevisionToSheets(rev.ElementId, sheetIds);
        StatusMessage = $"Revision added to {n} sheet(s).";
    }

    // ==============================================================
    //  FILTERS
    // ==============================================================

    private bool FilterSheet(object obj)
    {
        if (obj is not SheetInfo sheet) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var search = SearchText.Trim();
        return sheet.SheetNumber.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || sheet.OriginalName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || sheet.TitleBlockName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool FilterView(object obj)
    {
        if (obj is not ViewInfo view) return false;
        if (string.IsNullOrWhiteSpace(ViewSearchText)) return true;
        var search = ViewSearchText.Trim();
        return view.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || view.ViewType.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || view.Discipline.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || view.SheetNumber.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // ==============================================================
    //  SELECTION
    // ==============================================================

    private void Sheet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SheetInfo.IsSelected))
        {
            SelectedCount = Sheets.Count(s => s.IsSelected);
            if (IsSheetListVisible) UpdateStatusCounts();
        }
    }

    private void View_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewInfo.IsSelected))
        {
            ViewSelectedCount = Views.Count(v => v.IsSelected);
            if (IsViewListVisible) UpdateStatusCounts();
        }
    }

    private void Rev_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RevisionInfo.IsSelected))
        {
            RevSelectedCount = Revisions.Count(r => r.IsSelected);
            if (IsRevisionsVisible) UpdateStatusCounts();
        }
    }

    private void SetAllSheetsSelected(bool selected)
    {
        foreach (var sheet in Sheets) sheet.IsSelected = selected;
    }

    private void SetAllViewsSelected(bool selected)
    {
        foreach (var view in Views) view.IsSelected = selected;
    }

    private void SetAllRevisionsSelected(bool selected)
    {
        foreach (var rev in Revisions) rev.IsSelected = selected;
    }

    // ==============================================================
    //  SHEET OPERATIONS
    // ==============================================================

    private void UpdatePreview()
    {
        if (string.IsNullOrEmpty(FindText))
        {
            ResetPreviews();
            return;
        }

        int affected = 0;
        foreach (var sheet in Sheets)
        {
            sheet.PreviewName = sheet.OriginalName.Replace(FindText, ReplaceText ?? "");
            if (sheet.WillChange) affected++;
        }
        AffectedCount = affected;
        StatusMessage = affected > 0
            ? $"{affected} sheet(s) to rename"
            : "No matches found";
    }

    private void ResetPreviews()
    {
        foreach (var sheet in Sheets)
            sheet.PreviewName = sheet.OriginalName;
        AffectedCount = 0;
        FindText = string.Empty;
        ReplaceText = string.Empty;
    }

    private void ExecuteApplyRename()
    {
        var renames = Sheets
            .Where(s => s.WillChange)
            .ToDictionary(s => s.ElementId, s => s.PreviewName);
        if (renames.Count == 0) return;

        int renamed = _service.RenameSheets(renames);
        StatusMessage = $"{renamed} sheet(s) renamed successfully";
        IsFindReplaceOpen = false;
        LoadSheets();
    }

    private void ExecuteCreateSheet()
    {
        if (SelectedTitleBlock == null) return;
        var requests = new List<SheetCreateRequest>
        {
            new() { Number = NewSheetNumber.Trim(), Name = NewSheetName.Trim() }
        };

        var ids = _service.CreateSheets(SelectedTitleBlock.TypeId, requests);
        StatusMessage = $"{ids.Count} sheet(s) created";
        NewSheetNumber = string.Empty;
        NewSheetName = string.Empty;
        IsCreatePanelOpen = false;
        LoadSheets();
    }

    private void ExecuteDuplicateSelected()
    {
        var ids = Sheets.Where(s => s.IsSelected).Select(s => s.ElementId).ToList();
        if (ids.Count == 0) return;
        int count = _service.DuplicateSheets(ids);
        StatusMessage = $"{count} sheet(s) duplicated";
        LoadSheets();
    }

    private void ExecuteDeleteSelected()
    {
        var ids = Sheets.Where(s => s.IsSelected).Select(s => s.ElementId).ToList();
        if (ids.Count == 0) return;
        int count = _service.DeleteSheets(ids);
        StatusMessage = $"{count} sheet(s) deleted";
        LoadSheets();
    }

    private void ExecuteChangeTitleBlock(SheetInfo sheet)
    {
        var tb = TitleBlocks.FirstOrDefault(t => t.TypeId == sheet.TitleBlockTypeId);
        if (tb == null) return;

        int result = _service.ChangeTitleBlock(sheet.ElementId, tb.TypeId);
        if (result > 0)
        {
            sheet.TitleBlockName = tb.DisplayName;
            StatusMessage = $"Title block changed on {sheet.SheetNumber}";
        }
    }

    // ==============================================================
    //  VIEW OPERATIONS
    // ==============================================================

    private void ExecuteDeleteViews()
    {
        var ids = Views.Where(v => v.IsSelected).Select(v => v.ElementId).ToList();
        if (ids.Count == 0) return;
        int count = _service.DeleteViews(ids);
        StatusMessage = $"{count} view(s) deleted";
        LoadViews();
    }

    private void ExecuteRenameViews()
    {
        // Collect views that have been renamed (Name changed from original load)
        // For now this is a placeholder - inline editing changes the Name property directly
        StatusMessage = "View rename not yet implemented";
    }

    // ==============================================================
    //  REVISION OPERATIONS
    // ==============================================================

    private void ExecuteCreateRevision()
    {
        var id = _service.CreateRevision(
            NewRevDate.Trim(),
            NewRevDescription.Trim(),
            NewRevIssuedBy.Trim(),
            NewRevIssuedTo.Trim());

        StatusMessage = id >= 0 ? "Revision created" : "Failed to create revision";
        NewRevDate = string.Empty;
        NewRevDescription = string.Empty;
        NewRevIssuedBy = string.Empty;
        NewRevIssuedTo = string.Empty;
        IsCreateRevisionOpen = false;
        LoadRevisions();
    }

    private void ExecuteDeleteRevisions()
    {
        var ids = Revisions.Where(r => r.IsSelected).Select(r => r.ElementId).ToList();
        if (ids.Count == 0) return;
        int count = _service.DeleteRevisions(ids);
        StatusMessage = $"{count} revision(s) deleted";
        LoadRevisions();
    }

    private void ExecuteToggleRevisionIssued(RevisionInfo rev)
    {
        int result = _service.ToggleRevisionIssued(rev.ElementId);
        if (result > 0)
        {
            rev.Status = rev.Status == "Issued" ? "Not Issued" : "Issued";
            StatusMessage = $"Revision #{rev.Sequence} status changed to {rev.Status}";
        }
    }
}
