using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class TableGenViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    // ── Import tab ─────────────────────────────────────────────

    public ObservableCollection<ExcelImportTaskInfo> ImportTasks { get; } = new();

    private ExcelImportTaskInfo? _selectedTask;
    public ExcelImportTaskInfo? SelectedTask
    {
        get => _selectedTask;
        set
        {
            if (SetProperty(ref _selectedTask, value))
            {
                OnPropertyChanged(nameof(ShowConfig));
                OnPropertyChanged(nameof(AvailableSheets));
                OnPropertyChanged(nameof(AvailableRanges));
            }
        }
    }

    public bool ShowConfig => SelectedTask != null;

    public List<string> AvailableSheets => SelectedTask?.AvailableSheets ?? new();
    public List<string> AvailableRanges => SelectedTask?.AvailableRanges ?? new();

    public List<string> ViewTypeOptions { get; } = new() { "Drafting View", "Legend" };

    // ── Manage tab ─────────────────────────────────────────────

    public ObservableCollection<ExistingTableViewInfo> ExistingViews { get; } = new();

    // ── Active tab ─────────────────────────────────────────────

    private bool _showImport = true;
    public bool ShowImport
    {
        get => _showImport;
        set => SetProperty(ref _showImport, value);
    }

    private bool _showManage;
    public bool ShowManage
    {
        get => _showManage;
        set => SetProperty(ref _showManage, value);
    }

    // ── Progress ───────────────────────────────────────────────

    private bool _showProgress;
    public bool ShowProgress
    {
        get => _showProgress;
        set => SetProperty(ref _showProgress, value);
    }

    private int _progressValue;
    public int ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    private int _progressMax = 1;
    public int ProgressMax
    {
        get => _progressMax;
        set => SetProperty(ref _progressMax, value);
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ── Commands ───────────────────────────────────────────────

    public ICommand ShowImportTabCommand { get; }
    public ICommand ShowManageTabCommand { get; }
    public ICommand AddFilesCommand { get; }
    public ICommand RemoveFileCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand CloseCommand { get; }

    public Action? CloseAction { get; set; }

    public TableGenViewModel(IRevitService service)
    {
        _service = service;

        ShowImportTabCommand = new RelayCommand(_ => { ShowImport = true; ShowManage = false; });
        ShowManageTabCommand = new RelayCommand(_ => { ShowImport = false; ShowManage = true; LoadExisting(); });

        AddFilesCommand = new RelayCommand(_ => DoAddFiles());
        RemoveFileCommand = new RelayCommand(_ => DoRemoveFile(), _ => SelectedTask != null);
        ImportCommand = new RelayCommand(_ => DoImport(), _ => ImportTasks.Count > 0 && !ShowProgress);

        ReloadCommand = new RelayCommand(_ => DoReload(), _ => ExistingViews.Any(v => v.IsSelected));
        DeleteCommand = new RelayCommand(_ => DoDelete(), _ => ExistingViews.Any(v => v.IsSelected));
        SelectAllCommand = new RelayCommand(_ => { foreach (var v in ExistingViews) v.IsSelected = true; });
        SelectNoneCommand = new RelayCommand(_ => { foreach (var v in ExistingViews) v.IsSelected = false; });

        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
    }

    // ── Sheet/Range change notifiers ───────────────────────────

    public void OnSheetChanged()
    {
        OnPropertyChanged(nameof(AvailableRanges));
        if (SelectedTask != null)
        {
            var ranges = SelectedTask.AvailableRanges;
            if (ranges.Contains(ExcelReader.USED_RANGE))
                SelectedTask.SelectedRange = ExcelReader.USED_RANGE;
            else if (ranges.Count > 0)
                SelectedTask.SelectedRange = ranges[0];
        }
    }

    // ── Add Excel files ────────────────────────────────────────

    private void DoAddFiles()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Excel files",
            Filter = "Excel Files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm",
            Multiselect = true
        };

        if (dlg.ShowDialog() != true) return;

        StatusMessage = "Reading Excel files...";
        ForceUiUpdate();

        foreach (string file in dlg.FileNames)
        {
            // Skip duplicates
            if (ImportTasks.Any(t => t.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                var sheetsData = ExcelReader.GetSheetsAndRanges(file);
                if (sheetsData.Count == 0)
                {
                    StatusMessage = $"Could not read: {Path.GetFileName(file)}";
                    continue;
                }

                var task = new ExcelImportTaskInfo
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    SheetsData = sheetsData,
                    SelectedSheet = sheetsData.Keys.First()
                };

                // Set default range
                var firstRanges = sheetsData[task.SelectedSheet];
                if (firstRanges.ContainsKey(ExcelReader.USED_RANGE))
                    task.SelectedRange = ExcelReader.USED_RANGE;
                else if (firstRanges.Count > 0)
                    task.SelectedRange = firstRanges.Keys.First();

                ImportTasks.Add(task);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        if (ImportTasks.Count > 0 && SelectedTask == null)
            SelectedTask = ImportTasks[0];

        StatusMessage = $"{ImportTasks.Count} file(s) loaded";
    }

    private void DoRemoveFile()
    {
        if (SelectedTask == null) return;
        var task = SelectedTask;
        ImportTasks.Remove(task);
        SelectedTask = ImportTasks.FirstOrDefault();
    }

    // ── Import (draw tables in Revit) ──────────────────────────

    private void DoImport()
    {
        if (ImportTasks.Count == 0) return;

        ShowProgress = true;
        ProgressMax = ImportTasks.Count;
        ProgressValue = 0;
        int success = 0, errors = 0;

        foreach (var task in ImportTasks.ToList())
        {
            ProgressValue++;
            StatusMessage = $"Importing {task.FileName}... ({ProgressValue}/{ProgressMax})";
            ForceUiUpdate();

            try
            {
                string rangeAddr = task.GetRangeAddress();
                var data = ExcelReader.ReadTableData(task.FilePath, task.SelectedSheet, rangeAddr);

                if (data == null || data.Cells.Count == 0)
                {
                    task.Status = "Error: no data";
                    errors++;
                    continue;
                }

                // viewName encodes metadata: EXCEL_DATA|path|sheet|range (used for reload)
                string viewName = $"{task.FilePath}|{task.SelectedSheet}|{rangeAddr}";
                int result = _service.ImportExcelAsTable(data, viewName, task.SelectedViewType);

                if (result > 0)
                {
                    task.Status = "Imported";
                    success++;
                }
                else
                {
                    task.Status = "Failed";
                    errors++;
                }
            }
            catch (Exception ex)
            {
                task.Status = $"Error: {ex.Message}";
                errors++;
            }
        }

        ShowProgress = false;
        StatusMessage = $"Done! {success} imported, {errors} error(s)";

        if (success > 0)
            MessageBox.Show(
                $"Import complete.\nSuccessful: {success}\nErrors: {errors}",
                "AllO — Table Gen",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
    }

    // ── Manage existing ────────────────────────────────────────

    private void LoadExisting()
    {
        ExistingViews.Clear();
        foreach (var v in _service.GetExistingTableViews())
        {
            v.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ExistingTableViewInfo.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedExistingCount));
                }
            };
            ExistingViews.Add(v);
        }
        StatusMessage = $"{ExistingViews.Count} imported table(s) found";
    }

    public int SelectedExistingCount => ExistingViews.Count(v => v.IsSelected);

    private void DoReload()
    {
        var selected = ExistingViews.Where(v => v.IsSelected).ToList();
        if (selected.Count == 0) return;

        ShowProgress = true;
        ProgressMax = selected.Count;
        ProgressValue = 0;
        int reloaded = 0;

        foreach (var view in selected)
        {
            ProgressValue++;
            StatusMessage = $"Reloading {view.Name}... ({ProgressValue}/{ProgressMax})";
            ForceUiUpdate();

            try
            {
                if (string.IsNullOrEmpty(view.ExcelPath) || !File.Exists(view.ExcelPath))
                {
                    view.IsSelected = false;
                    continue;
                }

                var data = ExcelReader.ReadTableData(view.ExcelPath, view.SheetName, view.Range);
                if (data != null)
                {
                    _service.ReloadTableView(view.ElementId, data);
                    reloaded++;
                }
            }
            catch { }
        }

        ShowProgress = false;
        StatusMessage = $"Reloaded {reloaded} table(s)";
        LoadExisting();
    }

    private void DoDelete()
    {
        var ids = ExistingViews.Where(v => v.IsSelected).Select(v => v.ElementId).ToList();
        if (ids.Count == 0) return;

        if (MessageBox.Show(
            $"Delete {ids.Count} imported table view(s)?",
            "AllO — Table Gen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        int deleted = _service.DeleteTableViews(ids);
        StatusMessage = $"Deleted {deleted} view(s)";
        LoadExisting();
    }

    private void ForceUiUpdate()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            new DispatcherOperationCallback(o => { ((DispatcherFrame)o!).Continue = false; return null; }),
            frame);
        Dispatcher.PushFrame(frame);
    }
}
