using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;
using AllO.Services;

namespace AllO.UI.ViewModels;

public class ConnectorViewModel : ViewModelBase
{
    private readonly IRevitService _service;

    public ObservableCollection<DisconnectedConnectorInfo> Disconnected { get; } = new();

    private ICollectionView? _disconnectedView;
    public ICollectionView? DisconnectedView
    {
        get => _disconnectedView;
        private set => SetProperty(ref _disconnectedView, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) DisconnectedView?.Refresh(); }
    }

    // Filter by category
    public List<string> CategoryFilters { get; } = new() { "All", "Pipes", "Ducts", "Cable Trays", "Conduits", "Fittings" };
    private string _selectedCategory = "All";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set { if (SetProperty(ref _selectedCategory, value)) DisconnectedView?.Refresh(); }
    }

    // Tolerance
    private double _tolerance = 0.5;
    public double Tolerance
    {
        get => _tolerance;
        set => SetProperty(ref _tolerance, value);
    }

    // Stats
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

    private int _totalDisconnected;
    public int TotalDisconnected
    {
        get => _totalDisconnected;
        set => SetProperty(ref _totalDisconnected, value);
    }

    private string _statusMessage = "Click Scan to find disconnected elements";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    // Commands
    public ICommand ScanCommand { get; }
    public ICommand AutoConnectCommand { get; }
    public ICommand HighlightCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand CloseCommand { get; }

    public Action? CloseAction { get; set; }

    public ConnectorViewModel(IRevitService service)
    {
        _service = service;

        ScanCommand = new RelayCommand(_ => DoScan());
        AutoConnectCommand = new RelayCommand(_ => DoAutoConnect(), _ => SelectedCount > 0);
        HighlightCommand = new RelayCommand(p => DoHighlight(p), _ => true);
        SelectAllCommand = new RelayCommand(_ => SetAll(true));
        SelectNoneCommand = new RelayCommand(_ => SetAll(false));
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
    }

    private void DoScan()
    {
        Disconnected.Clear();
        StatusMessage = "Scanning for disconnected elements...";

        var items = _service.FindDisconnectedElements();
        foreach (var item in items)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DisconnectedConnectorInfo.IsSelected))
                    SelectedCount = Disconnected.Count(x => x.IsSelected);
            };
            Disconnected.Add(item);
        }

        DisconnectedView = CollectionViewSource.GetDefaultView(Disconnected);
        DisconnectedView.Filter = FilterItem;

        TotalCount = Disconnected.Count;
        SelectedCount = Disconnected.Count(x => x.IsSelected);
        TotalDisconnected = Disconnected.Sum(x => x.DisconnectedCount);

        StatusMessage = Disconnected.Count > 0
            ? $"Found {Disconnected.Count} element(s) with {TotalDisconnected} disconnected connector(s)"
            : "No disconnected elements found";
    }

    private bool FilterItem(object obj)
    {
        if (obj is not DisconnectedConnectorInfo item) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            bool matchSearch = item.ElementName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0
                || item.SystemType.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            if (!matchSearch) return false;
        }

        if (SelectedCategory != "All" && !item.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private void SetAll(bool val) { foreach (var e in Disconnected) e.IsSelected = val; }

    private void DoAutoConnect()
    {
        int connected = _service.AutoConnectNearby(Tolerance);
        StatusMessage = $"Auto-connected {connected} connector pair(s) within {Tolerance:F1} ft tolerance";

        // Rescan to update the list
        DoScan();
    }

    private void DoHighlight(object? param)
    {
        if (param is int id)
            _service.HighlightElement(id);
    }
}
