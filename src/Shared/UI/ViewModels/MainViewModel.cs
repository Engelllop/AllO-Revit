using System.Collections.ObjectModel;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IRevitService _revitService;

    private string _statusMessage = "Listo";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _documentName = string.Empty;
    public string DocumentName
    {
        get => _documentName;
        set => SetProperty(ref _documentName, value);
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

    private int _affectedCount;
    public int AffectedCount
    {
        get => _affectedCount;
        set => SetProperty(ref _affectedCount, value);
    }

    public ObservableCollection<SheetInfo> Sheets { get; } = new();

    public ICommand LoadSheetsCommand { get; }
    public ICommand ApplyRenameCommand { get; }
    public ICommand CloseCommand { get; }

    public Action? CloseAction { get; set; }

    public MainViewModel(IRevitService revitService)
    {
        _revitService = revitService;
        DocumentName = _revitService.GetDocumentName();
        StatusMessage = "Conectado a Revit";

        LoadSheetsCommand = new RelayCommand(_ => ExecuteLoadSheets());
        ApplyRenameCommand = new RelayCommand(_ => ExecuteApplyRename(), _ => AffectedCount > 0);
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());

        ExecuteLoadSheets();
    }

    private void ExecuteLoadSheets()
    {
        Sheets.Clear();
        foreach (var sheet in _revitService.GetAllSheets())
            Sheets.Add(sheet);
        AffectedCount = 0;
        StatusMessage = $"{Sheets.Count} plano(s) cargados";
    }

    private void UpdatePreview()
    {
        if (string.IsNullOrEmpty(FindText))
        {
            foreach (var sheet in Sheets)
                sheet.PreviewName = sheet.OriginalName;
            AffectedCount = 0;
            StatusMessage = "Ingresa el texto a buscar";
            OnPropertyChanged(nameof(Sheets));
            return;
        }

        int affected = 0;
        foreach (var sheet in Sheets)
        {
            sheet.PreviewName = sheet.OriginalName.Replace(FindText, ReplaceText ?? "");
            if (sheet.WillChange) affected++;
        }
        AffectedCount = affected;
        StatusMessage = affected > 0 ? $"{affected} plano(s) por renombrar" : "Sin coincidencias";
        OnPropertyChanged(nameof(Sheets));
    }

    private void ExecuteApplyRename()
    {
        var renames = Sheets.Where(s => s.WillChange).ToDictionary(s => s.ElementId, s => s.PreviewName);
        if (renames.Count == 0) return;

        int renamed = _revitService.RenameSheets(renames);
        StatusMessage = $"{renamed} plano(s) renombrados";
        ExecuteLoadSheets();
    }
}
