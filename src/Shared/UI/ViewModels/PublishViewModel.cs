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
    private readonly IRevitService _service;

    public ObservableCollection<PublishSheetItem> Sheets { get; } = new();

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
                SheetsView?.Refresh();
        }
    }

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

    // ── Status / Progress ───────────────────────────────────

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

    public Action? CloseAction { get; set; }

    // Dispatcher for UI thread updates
    private readonly Dispatcher _dispatcher;

    public PublishViewModel(IRevitService service)
    {
        _service = service;
        _dispatcher = Dispatcher.CurrentDispatcher;

        DocumentName = _service.GetDocumentName();
        OutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        SelectAllCommand = new RelayCommand(_ => SetAllSelected(true));
        SelectNoneCommand = new RelayCommand(_ => SetAllSelected(false));
        BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());

        PublishCommand = new RelayCommand(
            _ => ExecutePublish(),
            _ => SelectedCount > 0 && (ExportPdf || ExportDwg)
                 && !string.IsNullOrWhiteSpace(OutputFolder) && !IsExporting);

        OpenFolderCommand = new RelayCommand(
            _ => OpenOutputFolder(),
            _ => !string.IsNullOrWhiteSpace(OutputFolder));

        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

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

        TotalCount = Sheets.Count;
        SelectedCount = Sheets.Count(s => s.IsSelected);
        StatusMessage = $"{Sheets.Count} sheet(s) loaded";
    }

    private bool FilterSheet(object obj)
    {
        if (obj is not PublishSheetItem sheet) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        var search = SearchText.Trim();
        return sheet.SheetNumber.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
            || sheet.SheetName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
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
                OutputFolder = folder;
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
        _dispatcher.Invoke(() => { }, DispatcherPriority.Render);
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
                    currentStep++;
                    sheet.Status = $"PDF ({currentStep}/{totalSteps})...";
                    StatusMessage = $"Exporting PDF: {sheet.SheetNumber} — {currentStep} of {totalSteps}";
                    ProgressValue = currentStep;
                    ForceUiUpdate();

                    bool ok = _service.ExportSingleToPdf(sheet.ElementId, pdfFolder, NamingPattern, CombinePdf);
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
            if (ExportDwg)
            {
                foreach (var sheet in selected)
                {
                    currentStep++;
                    sheet.Status = $"DWG ({currentStep}/{totalSteps})...";
                    StatusMessage = $"Exporting DWG: {sheet.SheetNumber} — {currentStep} of {totalSteps}";
                    ProgressValue = currentStep;
                    ForceUiUpdate();

                    bool ok = _service.ExportSingleToDwg(sheet.ElementId, dwgFolder, NamingPattern);
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
            string summary = $"Export complete: {ExportedCount} exported";
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
}
