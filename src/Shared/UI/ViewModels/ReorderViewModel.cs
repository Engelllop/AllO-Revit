using System.Collections.ObjectModel;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;

namespace AllO.UI.ViewModels;

public class ReorderViewModel : ViewModelBase
{
    public ObservableCollection<ReorderItem> Items { get; } = new();

    private string _prefix = "";
    public string Prefix
    {
        get => _prefix;
        set
        {
            if (SetProperty(ref _prefix, value))
                RefreshPreview();
        }
    }

    private int _startNumber = 1;
    public int StartNumber
    {
        get => _startNumber;
        set
        {
            if (SetProperty(ref _startNumber, value))
                RefreshPreview();
        }
    }

    private string _suffix = "";
    public string Suffix
    {
        get => _suffix;
        set
        {
            if (SetProperty(ref _suffix, value))
                RefreshPreview();
        }
    }

    private string _parameterName = "Mark";
    public string ParameterName
    {
        get => _parameterName;
        set => SetProperty(ref _parameterName, value);
    }

    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand ExecuteCommand { get; }
    public ICommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }

    public ReorderViewModel(List<ReorderItem> items)
    {
        foreach (var item in items)
        {
            Items.Add(item);
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ReorderItem.IsSelected))
                    RefreshPreview();
            };
        }

        SelectAllCommand = new RelayCommand(_ =>
        {
            foreach (var i in Items) i.IsSelected = true;
        });

        SelectNoneCommand = new RelayCommand(_ =>
        {
            foreach (var i in Items) i.IsSelected = false;
        });

        ExecuteCommand = new RelayCommand(_ =>
        {
            DialogResult = true;
            OnCloseRequested();
        });

        CancelCommand = new RelayCommand(_ =>
        {
            DialogResult = false;
            OnCloseRequested();
        });

        RefreshPreview();
    }

    private void RefreshPreview()
    {
        int idx = StartNumber;
        foreach (var item in Items)
        {
            if (item.IsSelected)
            {
                item.PreviewValue = $"{Prefix}{idx}{Suffix}";
                idx++;
            }
            else
            {
                item.PreviewValue = item.CurrentValue;
            }
        }
    }

    public event Action? CloseRequested;
    private void OnCloseRequested() => CloseRequested?.Invoke();
}
