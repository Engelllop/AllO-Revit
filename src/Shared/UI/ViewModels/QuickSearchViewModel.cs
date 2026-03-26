using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class QuickSearchViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    public ObservableCollection<SearchResultInfo> Results { get; } = new();

    private ICollectionView? _resultsView;
    public ICollectionView? ResultsView
    {
        get => _resultsView;
        private set => SetProperty(ref _resultsView, value);
    }

    // ── Search query ──────────────────────────────────────────

    private string _searchQuery = string.Empty;
    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
                DoSearch();
        }
    }

    // ── Search scope toggles ──────────────────────────────────

    private bool _searchByName = true;
    public bool SearchByName
    {
        get => _searchByName;
        set { if (SetProperty(ref _searchByName, value)) DoSearch(); }
    }

    private bool _searchById;
    public bool SearchById
    {
        get => _searchById;
        set { if (SetProperty(ref _searchById, value)) DoSearch(); }
    }

    private bool _searchByCategory = true;
    public bool SearchByCategory
    {
        get => _searchByCategory;
        set { if (SetProperty(ref _searchByCategory, value)) DoSearch(); }
    }

    private bool _searchByFamily;
    public bool SearchByFamily
    {
        get => _searchByFamily;
        set { if (SetProperty(ref _searchByFamily, value)) DoSearch(); }
    }

    private bool _searchByParameter;
    public bool SearchByParameter
    {
        get => _searchByParameter;
        set { if (SetProperty(ref _searchByParameter, value)) DoSearch(); }
    }

    // ── Stats ─────────────────────────────────────────────────

    private int _resultCount;
    public int ResultCount
    {
        get => _resultCount;
        set => SetProperty(ref _resultCount, value);
    }

    private int _selectedCount;
    public int SelectedCount
    {
        get => _selectedCount;
        set => SetProperty(ref _selectedCount, value);
    }

    private string _statusMessage = "Type to search...";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // ── Commands ──────────────────────────────────────────────

    public ICommand SelectInRevitCommand { get; }
    public ICommand IsolateInViewCommand { get; }
    public ICommand ZoomToCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand CloseCommand { get; }

    public Action? CloseAction { get; set; }

    public QuickSearchViewModel(IRevitService service)
    {
        _service = service;

        SelectInRevitCommand = new RelayCommand(_ => DoSelectInRevit(), _ => SelectedCount > 0);
        IsolateInViewCommand = new RelayCommand(_ => DoIsolate(), _ => SelectedCount > 0);
        ZoomToCommand = new RelayCommand(p => DoZoomTo(p));
        SelectAllCommand = new RelayCommand(_ => SetAll(true));
        SelectNoneCommand = new RelayCommand(_ => SetAll(false));
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
    }

    private void DoSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2)
        {
            Results.Clear();
            ResultCount = 0;
            SelectedCount = 0;
            StatusMessage = SearchQuery.Length < 2 ? "Type at least 2 characters..." : "Type to search...";
            return;
        }

        var items = _service.SearchElements(SearchQuery, SearchByName, SearchById, SearchByCategory, SearchByFamily, SearchByParameter);

        Results.Clear();
        foreach (var item in items)
        {
            item.IsSelected = true;
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchResultInfo.IsSelected))
                    SelectedCount = Results.Count(r => r.IsSelected);
            };
            Results.Add(item);
        }

        ResultsView = CollectionViewSource.GetDefaultView(Results);
        ResultCount = Results.Count;
        SelectedCount = Results.Count;
        StatusMessage = $"Found {Results.Count} element(s)";
    }

    private void SetAll(bool val)
    {
        foreach (var r in Results) r.IsSelected = val;
    }

    private List<int> GetSelectedIds() =>
        Results.Where(r => r.IsSelected).Select(r => r.ElementId).ToList();

    private void DoSelectInRevit()
    {
        var ids = GetSelectedIds();
        int count = _service.SelectElements(ids);
        StatusMessage = $"Selected {count} element(s) in Revit";
    }

    private void DoIsolate()
    {
        var ids = GetSelectedIds();
        int count = _service.IsolateElements(ids);
        StatusMessage = $"Isolated {count} element(s) in active view";
    }

    private void DoZoomTo(object? param)
    {
        if (param is int id)
            _service.HighlightElement(id);
    }
}
