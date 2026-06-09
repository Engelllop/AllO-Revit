using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class PublishViewModel : ViewModelBase
{
    public const string FilterParamNoneLabel = "— Parameter —";
    public const string FilterValueAnyLabel = "— Any value —";
    public const string SheetSetNoneLabel = "— Sheet Set —";

    private readonly IRevitService _service;

    public ObservableCollection<PublishSheetItem> Sheets { get; } = new();

    public ObservableCollection<string> FilterParameterNames { get; } = new();
    public ObservableCollection<string> FilterValueOptions { get; } = new();
    public ObservableCollection<string> SheetSets { get; } = new();

    private string? _selectedSheetSet = SheetSetNoneLabel;
    /// <summary>Conjunto de hojas guardado (ViewSheetSet) cuyo contenido marca la selección.</summary>
    public string? SelectedSheetSet
    {
        get => _selectedSheetSet;
        set { if (SetProperty(ref _selectedSheetSet, value)) ApplySheetSet(); }
    }

    private void ApplySheetSet()
    {
        if (string.IsNullOrEmpty(_selectedSheetSet) || _selectedSheetSet == SheetSetNoneLabel) return;
        var nums = new HashSet<string>(_service.GetSheetNumbersInSet(_selectedSheetSet!), StringComparer.OrdinalIgnoreCase);
        if (nums.Count == 0) return;
        foreach (var s in Sheets) s.IsSelected = nums.Contains(s.SheetNumber);
        SelectedCount = Sheets.Count(s => s.IsSelected);
        StatusMessage = $"Set '{_selectedSheetSet}': {SelectedCount} sheet(s) selected";
    }

    private ICollectionView? _sheetsView;
    public ICollectionView? SheetsView
    {
        get => _sheetsView;
        private set => SetProperty(ref _sheetsView, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                RefreshSheetView();
        }
    }

    private string? _selectedFilterParameter = FilterParamNoneLabel;
    /// <summary>Parameter definition name to filter by, or <see cref="FilterParamNoneLabel"/> for no filter.</summary>
    public string? SelectedFilterParameter
    {
        get => _selectedFilterParameter;
        set
        {
            if (!SetProperty(ref _selectedFilterParameter, value))
                return;
            OnPropertyChanged(nameof(HasParameterFilter));
            RebuildFilterValueOptions();
            RefreshSheetView();
        }
    }

    private string? _selectedFilterValue = FilterValueAnyLabel;
    public string? SelectedFilterValue
    {
        get => _selectedFilterValue;
        set
        {
            if (SetProperty(ref _selectedFilterValue, value))
                RefreshSheetView();
        }
    }

    private bool _isFilterPopupOpen;
    public bool IsFilterPopupOpen
    {
        get => _isFilterPopupOpen;
        set => SetProperty(ref _isFilterPopupOpen, value);
    }

    public bool HasParameterFilter =>
        !string.IsNullOrEmpty(SelectedFilterParameter)
        && !string.Equals(SelectedFilterParameter, FilterParamNoneLabel, StringComparison.Ordinal);

    private string _documentName = string.Empty;
    public string DocumentName
    {
        get => _documentName;
        set => SetProperty(ref _documentName, value);
    }

    // ── Export format ────────────────────────────────────────

    private bool _exportPdf = true;
    public bool ExportPdf
    {
        get => _exportPdf;
        set => SetProperty(ref _exportPdf, value);
    }

    private bool _exportDwg;
    public bool ExportDwg
    {
        get => _exportDwg;
        set => SetProperty(ref _exportDwg, value);
    }

    // ── PDF Settings ────────────────────────────────────────

    public List<string> ColorModes { get; } = new() { "Color", "Black & White", "Grayscale" };
    private string _selectedColorMode = "Color";
    public string SelectedColorMode
    {
        get => _selectedColorMode;
        set => SetProperty(ref _selectedColorMode, value);
    }

    public List<string> Qualities { get; } = new() { "High (300 DPI)", "Medium (150 DPI)", "Low (72 DPI)" };
    private string _selectedQuality = "High (300 DPI)";
    public string SelectedQuality
    {
        get => _selectedQuality;
        set => SetProperty(ref _selectedQuality, value);
    }

    public List<string> PaperSizes { get; } = new() { "Auto (from sheet)", "A0", "A1", "A2", "A3", "A4", "Letter", "Legal", "Tabloid" };
    private string _selectedPaperSize = "Auto (from sheet)";
    public string SelectedPaperSize
    {
        get => _selectedPaperSize;
        set => SetProperty(ref _selectedPaperSize, value);
    }

    private bool _combinePdf;
    public bool CombinePdf
    {
        get => _combinePdf;
        set => SetProperty(ref _combinePdf, value);
    }

    // ── DWG Settings ────────────────────────────────────────

    public List<string> DwgUnits { get; } = new() { "Meters", "Centimeters", "Millimeters", "Feet", "Inches" };
    private string _selectedDwgUnit = "Meters";
    public string SelectedDwgUnit
    {
        get => _selectedDwgUnit;
        set => SetProperty(ref _selectedDwgUnit, value);
    }

    public List<string> DwgVersions { get; } = new() { "AutoCAD 2018", "AutoCAD 2013", "AutoCAD 2010", "AutoCAD 2007" };
    private string _selectedDwgVersion = "AutoCAD 2018";
    public string SelectedDwgVersion
    {
        get => _selectedDwgVersion;
        set => SetProperty(ref _selectedDwgVersion, value);
    }

    // ── Output ──────────────────────────────────────────────

    private string _outputFolder = string.Empty;
    public string OutputFolder
    {
        get => _outputFolder;
        set => SetProperty(ref _outputFolder, value);
    }

    private string _namingPattern = "{number} - {name}";
    public string NamingPattern
    {
        get => _namingPattern;
        set => SetProperty(ref _namingPattern, value);
    }

    private bool _createSubfolders;
    public bool CreateSubfolders
    {
        get => _createSubfolders;
        set => SetProperty(ref _createSubfolders, value);
    }

    private bool _subfolderByDiscipline;
    public bool SubfolderByDiscipline
    {
        get => _subfolderByDiscipline;
        set => SetProperty(ref _subfolderByDiscipline, value);
    }

    private string _newSetName = string.Empty;
    public string NewSetName
    {
        get => _newSetName;
        set => SetProperty(ref _newSetName, value);
    }

    // ── Status / Progress ───────────────────────────────────

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private int _totalInDocument;
    /// <summary>Sheets in the document (unfiltered).</summary>
    public int TotalInDocument
    {
        get => _totalInDocument;
        set => SetProperty(ref _totalInDocument, value);
    }

    private int _visibleSheetCount;
    /// <summary>Sheets visible in the grid after search + parameter filter.</summary>
    public int VisibleSheetCount
    {
        get => _visibleSheetCount;
        set => SetProperty(ref _visibleSheetCount, value);
    }

    private int _selectedCount;
    public int SelectedCount
    {
        get => _selectedCount;
        set => SetProperty(ref _selectedCount, value);
    }

    private bool _isExporting;
    public bool IsExporting
    {
        get => _isExporting;
        set => SetProperty(ref _isExporting, value);
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

    private bool _showProgress;
    public bool ShowProgress
    {
        get => _showProgress;
        set => SetProperty(ref _showProgress, value);
    }

    private int _exportedCount;
    public int ExportedCount
    {
        get => _exportedCount;
        set => SetProperty(ref _exportedCount, value);
    }

    private int _failedCount;
    public int FailedCount
    {
        get => _failedCount;
        set => SetProperty(ref _failedCount, value);
    }

    // ── Commands ────────────────────────────────────────────

    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand BrowseFolderCommand { get; }
    public ICommand PublishCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ToggleFilterPopupCommand { get; }
    public ICommand ClearSheetFilterCommand { get; }
    public ICommand CancelExportCommand { get; }
    public ICommand SaveSheetSetCommand { get; }

    // Cancelación cooperativa del export en curso (el loop la consulta entre sheets).
    private volatile bool _cancelRequested;

    public Action? CloseAction { get; set; }

    // Dispatcher for UI thread updates
    private readonly Dispatcher _dispatcher;

    public PublishViewModel(IRevitService service)
    {
        _service = service;
        _dispatcher = Dispatcher.CurrentDispatcher;

        DocumentName = _service.GetDocumentName();
        var lastFolder = AllOSettings.Current.LastPublishFolder;
        OutputFolder = !string.IsNullOrWhiteSpace(lastFolder) && System.IO.Directory.Exists(lastFolder)
            ? lastFolder
            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        SelectAllCommand = new RelayCommand(_ => SetAllSelected(true));
        SelectNoneCommand = new RelayCommand(_ => SetAllSelected(false));
        BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
        SaveSheetSetCommand = new RelayCommand(_ => SaveSheetSet(), _ => Sheets.Any(s => s.IsSelected));

        PublishCommand = new RelayCommand(
            _ => ExecutePublish(),
            _ => SelectedCount > 0 && (ExportPdf || ExportDwg)
                 && !string.IsNullOrWhiteSpace(OutputFolder) && !IsExporting);

        CancelExportCommand = new RelayCommand(_ => _cancelRequested = true, _ => IsExporting);

        OpenFolderCommand = new RelayCommand(
            _ => OpenOutputFolder(),
            _ => !string.IsNullOrWhiteSpace(OutputFolder));

        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        ToggleFilterPopupCommand = new RelayCommand(_ =>
        {
            IsFilterPopupOpen = !IsFilterPopupOpen;
            if (IsFilterPopupOpen)
                RefreshFilterParameterNames();
        });

        ClearSheetFilterCommand = new RelayCommand(_ =>
        {
            SelectedFilterParameter = FilterParamNoneLabel;
            _selectedFilterValue = FilterValueAnyLabel;
            OnPropertyChanged(nameof(SelectedFilterValue));
            IsFilterPopupOpen = false;
            RefreshSheetView();
        });

        LoadSheets();
    }

    private void LoadSheets()
    {
        Sheets.Clear();
        foreach (var sheet in _service.GetSheetsForPublish())
        {
            sheet.PropertyChanged += Sheet_PropertyChanged;
            Sheets.Add(sheet);
        }

        SheetsView = CollectionViewSource.GetDefaultView(Sheets);
        SheetsView.Filter = FilterSheet;

        TotalInDocument = Sheets.Count;

        SheetSets.Clear();
        SheetSets.Add(SheetSetNoneLabel);
        foreach (var setName in _service.GetSheetSetNames()) SheetSets.Add(setName);
        _selectedSheetSet = SheetSetNoneLabel;
        OnPropertyChanged(nameof(SelectedSheetSet));

        RefreshFilterParameterNames();
        _selectedFilterParameter = FilterParamNoneLabel;
        _selectedFilterValue = FilterValueAnyLabel;
        OnPropertyChanged(nameof(SelectedFilterParameter));
        OnPropertyChanged(nameof(SelectedFilterValue));
        OnPropertyChanged(nameof(HasParameterFilter));
        RebuildFilterValueOptions();
        RefreshSheetView();

        SelectedCount = Sheets.Count(s => s.IsSelected);
        StatusMessage = $"{Sheets.Count} sheet(s) loaded";
    }

    private void RefreshFilterParameterNames()
    {
        FilterParameterNames.Clear();
        FilterParameterNames.Add(FilterParamNoneLabel);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in Sheets)
        {
            foreach (var key in s.ParameterValues.Keys)
                names.Add(key);
        }
        foreach (var n in names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            FilterParameterNames.Add(n);
    }

    private void RebuildFilterValueOptions()
    {
        FilterValueOptions.Clear();
        FilterValueOptions.Add(FilterValueAnyLabel);

        if (string.IsNullOrEmpty(SelectedFilterParameter)
            || string.Equals(SelectedFilterParameter, FilterParamNoneLabel, StringComparison.Ordinal))
        {
            if (!string.Equals(_selectedFilterValue, FilterValueAnyLabel, StringComparison.Ordinal))
            {
                _selectedFilterValue = FilterValueAnyLabel;
                OnPropertyChanged(nameof(SelectedFilterValue));
            }
            return;
        }

        var vals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in Sheets)
        {
            if (s.ParameterValues.TryGetValue(SelectedFilterParameter!, out var pv))
                vals.Add(pv);
        }
        foreach (var v in vals.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            FilterValueOptions.Add(v);

        _selectedFilterValue = FilterValueAnyLabel;
        OnPropertyChanged(nameof(SelectedFilterValue));
    }

    private void RefreshSheetView()
    {
        SheetsView?.Refresh();
        UpdateVisibleCount();
    }

    private void UpdateVisibleCount()
    {
        if (SheetsView == null)
        {
            VisibleSheetCount = 0;
            return;
        }
        int n = 0;
        foreach (var _ in SheetsView)
            n++;
        VisibleSheetCount = n;
    }

    private bool FilterSheet(object obj)
    {
        if (obj is not PublishSheetItem sheet) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            bool textMatch = sheet.SheetNumber.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || sheet.SheetName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
            if (!textMatch) return false;
        }

        string? filterParam = SelectedFilterParameter;
        if (!string.IsNullOrEmpty(filterParam)
            && !string.Equals(filterParam, FilterParamNoneLabel, StringComparison.Ordinal))
        {
            string key = filterParam!; // non-null: guarded by IsNullOrEmpty above
            if (!sheet.ParameterValues.TryGetValue(key, out var pv))
                return false;

            if (!string.IsNullOrEmpty(SelectedFilterValue)
                && !string.Equals(SelectedFilterValue, FilterValueAnyLabel, StringComparison.Ordinal))
            {
                if (!string.Equals(pv.Trim(), SelectedFilterValue?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        return true;
    }

    private void Sheet_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PublishSheetItem.IsSelected))
            SelectedCount = Sheets.Count(s => s.IsSelected);
    }

    private void SetAllSelected(bool selected)
    {
        foreach (var sheet in Sheets) sheet.IsSelected = selected;
    }

    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select output folder (just click Save)",
            FileName = "Select Folder",
            Filter = "Folder|*.folder",
            CheckFileExists = false,
            CheckPathExists = true,
            InitialDirectory = string.IsNullOrEmpty(OutputFolder) ? "" : OutputFolder
        };

        if (dialog.ShowDialog() == true)
        {
            string? folder = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(folder))
            {
                OutputFolder = folder;
                AllOSettings.Current.LastPublishFolder = folder;
                AllOSettings.Current.Save();
            }
        }
    }

    private void OpenOutputFolder()
    {
        try
        {
            if (System.IO.Directory.Exists(OutputFolder))
                System.Diagnostics.Process.Start("explorer.exe", OutputFolder);
        }
        catch { }
    }

    /// <summary>
    /// Force the WPF UI to process pending render updates.
    /// </summary>
    private void ForceUiUpdate()
    {
        // Background procesa también input (clic en Cancelar) durante el bombeo síncrono.
        _dispatcher.Invoke(() => { }, DispatcherPriority.Background);
    }

    private void ExecutePublish()
    {
        var selected = Sheets.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0) return;

        IsExporting = true;
        ShowProgress = true;
        ProgressValue = 0;
        ExportedCount = 0;
        FailedCount = 0;
        _cancelRequested = false;

        AllOSettings.Current.LastPublishFolder = OutputFolder;
        AllOSettings.Current.Save();

        int totalSteps = 0;
        if (ExportPdf) totalSteps += selected.Count;
        if (ExportDwg) totalSteps += selected.Count;
        ProgressMax = totalSteps;

        int currentStep = 0;

        try
        {
            string pdfFolder = OutputFolder;
            string dwgFolder = OutputFolder;

            if (CreateSubfolders)
            {
                if (ExportPdf)
                {
                    pdfFolder = System.IO.Path.Combine(OutputFolder, "PDF");
                    System.IO.Directory.CreateDirectory(pdfFolder);
                }
                if (ExportDwg)
                {
                    dwgFolder = System.IO.Path.Combine(OutputFolder, "DWG");
                    System.IO.Directory.CreateDirectory(dwgFolder);
                }
            }

            // ── PDF export (sheet by sheet) ──
            if (ExportPdf)
            {
                foreach (var sheet in selected)
                {
                    if (_cancelRequested) break;
                    currentStep++;
                    sheet.Status = $"PDF ({currentStep}/{totalSteps})...";
                    StatusMessage = $"Exporting PDF: {sheet.SheetNumber} — {currentStep} of {totalSteps}";
                    ProgressValue = currentStep;
                    ForceUiUpdate();

                    var targetPdf = SubfolderByDiscipline ? EnsureDisciplineFolder(pdfFolder, sheet) : pdfFolder;
                    bool ok = _service.ExportSingleToPdf(sheet.ElementId, targetPdf, NamingPattern, CombinePdf);
                    if (ok)
                    {
                        sheet.Status = ExportDwg ? "PDF ✓" : "Done";
                        ExportedCount++;
                    }
                    else
                    {
                        sheet.Status = "PDF Failed";
                        FailedCount++;
                    }
                    ForceUiUpdate();
                }
            }

            // ── DWG export (sheet by sheet) ──
            if (ExportDwg && !_cancelRequested)
            {
                foreach (var sheet in selected)
                {
                    if (_cancelRequested) break;
                    currentStep++;
                    sheet.Status = $"DWG ({currentStep}/{totalSteps})...";
                    StatusMessage = $"Exporting DWG: {sheet.SheetNumber} — {currentStep} of {totalSteps}";
                    ProgressValue = currentStep;
                    ForceUiUpdate();

                    var targetDwg = SubfolderByDiscipline ? EnsureDisciplineFolder(dwgFolder, sheet) : dwgFolder;
                    bool ok = _service.ExportSingleToDwg(sheet.ElementId, targetDwg, NamingPattern);
                    if (ok)
                    {
                        sheet.Status = "Done";
                        ExportedCount++;
                    }
                    else
                    {
                        sheet.Status = sheet.Status.Contains("PDF") ? "DWG Failed" : "Failed";
                        FailedCount++;
                    }
                    ForceUiUpdate();
                }
            }

            ProgressValue = ProgressMax;
            string summary = _cancelRequested
                ? $"Export cancelled: {ExportedCount} exported"
                : $"Export complete: {ExportedCount} exported";
            if (FailedCount > 0)
                summary += $", {FailedCount} failed";
            StatusMessage = summary;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
            foreach (var sheet in selected)
                if (sheet.Status != "Done") sheet.Status = "Failed";
        }
        finally
        {
            IsExporting = false;
        }
    }

    private static readonly string[] DisciplineKeys =
        { "Discipline", "View Discipline", "SISTEMA", "Sub-Discipline" };

    private string EnsureDisciplineFolder(string baseFolder, PublishSheetItem sheet)
    {
        string disc = "General";
        foreach (var key in DisciplineKeys)
            if (sheet.ParameterValues != null
                && sheet.ParameterValues.TryGetValue(key, out var v)
                && !string.IsNullOrWhiteSpace(v))
            { disc = v; break; }

        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            disc = disc.Replace(c, '_');

        var folder = System.IO.Path.Combine(baseFolder, disc);
        System.IO.Directory.CreateDirectory(folder);
        return folder;
    }

    private void SaveSheetSet()
    {
        var ids = Sheets.Where(s => s.IsSelected).Select(s => s.ElementId).ToList();
        if (ids.Count == 0) return;

        var name = Helpers.InputDialog.Show("Save Sheet Set",
            "Name for the new sheet set:", string.IsNullOrWhiteSpace(NewSetName) ? "AllO Set" : NewSetName);
        if (string.IsNullOrWhiteSpace(name)) return;

        bool ok = _service.CreateSheetSet(name!, ids);
        if (ok && !SheetSets.Contains(name!)) SheetSets.Add(name!);
        StatusMessage = ok ? $"Sheet set '{name}' saved ({ids.Count} sheets)." : "Could not save sheet set.";
    }
}
