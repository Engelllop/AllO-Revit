using System;
using System.Linq;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace AllO.UI.ViewModels;

public class LinkVisibilityViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    public string DocumentName { get; }

    public ObservableCollection<LinkDocumentInfo> Links { get; } = new();
    public ObservableCollection<LinkDisplayViewItem> Views { get; } = new();
    public ObservableCollection<string> DisplayModes { get; } = new()
    {
        "ByHostView",
        "ByLinkedView",
        "Custom"
    };

    private LinkDocumentInfo? _selectedLink;
    public LinkDocumentInfo? SelectedLink
    {
        get => _selectedLink;
        set { SetProperty(ref _selectedLink, value); OnLinkChanged(); }
    }

    private string _selectedDisplayMode = "ByHostView";
    public string SelectedDisplayMode
    {
        get => _selectedDisplayMode;
        set { SetProperty(ref _selectedDisplayMode, value); CommandManager.InvalidateRequerySuggested(); }
    }

    private string _viewSearch = string.Empty;
    public string ViewSearch
    {
        get => _viewSearch;
        set { SetProperty(ref _viewSearch, value); ViewsView.Refresh(); }
    }

    public ICollectionView ViewsView { get; }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand ApplyCommand { get; }
    public RelayCommand CloseCommand { get; }
    public RelayCommand SelectAllViewsCommand { get; }
    public RelayCommand SelectNoneViewsCommand { get; }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public LinkVisibilityViewModel(IRevitService service)
    {
        _service = service;
        DocumentName = _service.GetDocumentName();

        ViewsView = CollectionViewSource.GetDefaultView(Views);
        ViewsView.Filter = o => string.IsNullOrEmpty(ViewSearch) || ((LinkDisplayViewItem)o).Name.IndexOf(ViewSearch, StringComparison.OrdinalIgnoreCase) >= 0;

        RefreshCommand = new RelayCommand(_ => LoadData());
        ApplyCommand = new RelayCommand(_ => Apply(), _ => CanApply());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
        SelectAllViewsCommand = new RelayCommand(_ => SetAllViews(true));
        SelectNoneViewsCommand = new RelayCommand(_ => SetAllViews(false));

        LoadData();
    }

    public event Action? RequestClose;

    private void LoadData()
    {
        try
        {
            Links.Clear();
            foreach (var link in _service.GetLinkedDocuments())
                if (link.IsLoaded)
                    Links.Add(link);

            Views.Clear();
            foreach (var v in _service.GetViewsForLinkDisplay())
                Views.Add(v);

            StatusMessage = $"{Links.Count} link(s), {Views.Count} view(s) loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
        }
    }

    private void OnLinkChanged()
    {
        if (SelectedLink == null)
        {
            SelectedDisplayMode = "ByHostView";
            CommandManager.InvalidateRequerySuggested();
            return;
        }

        try
        {
            var firstView = Views.FirstOrDefault(v => v.IsSelected);
            int viewId = firstView?.ViewId ?? 0;
            if (viewId == 0 && Views.Count > 0)
                viewId = Views[0].ViewId;

            var state = _service.GetLinkDisplayState(SelectedLink.LinkInstanceId, viewId);
            SelectedDisplayMode = state.DisplayMode;
            StatusMessage = $"Current state for '{SelectedLink.Name}' in active view: {state.DisplayMode}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading link state: {ex.Message}";
        }
        CommandManager.InvalidateRequerySuggested();
    }

    private void SetAllViews(bool selected)
    {
        foreach (var v in Views) v.IsSelected = selected;
        ViewsView.Refresh();
        CommandManager.InvalidateRequerySuggested();
    }

    private bool CanApply()
    {
        return SelectedLink != null && Views.Any(v => v.IsSelected);
    }

    private void Apply()
    {
        if (SelectedLink == null) return;
        var viewIds = Views.Where(v => v.IsSelected).Select(v => v.ViewId).ToList();
        var state = new LinkDisplayState
        {
            LinkInstanceId = SelectedLink.LinkInstanceId,
            DisplayMode = SelectedDisplayMode,
            IsSelected = true
        };
        try
        {
            int modified = _service.ApplyLinkDisplaySettings(SelectedLink.LinkInstanceId, viewIds, state);
            StatusMessage = $"Applied '{SelectedDisplayMode}' to {modified} view(s).";
        }
        catch (NotSupportedException)
        {
            StatusMessage = "Link Display requires Revit 2024 or newer.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
        }
    }
}
