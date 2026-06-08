using System.Collections.ObjectModel;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;

namespace AllO.UI.ViewModels;

public class MatchParameterViewModel : ViewModelBase
{
    public ObservableCollection<MatchParameterItem> Parameters { get; } = new();

    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand SelectSafeCommand { get; }
    public ICommand ExecuteCommand { get; }
    public ICommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }

    public MatchParameterViewModel(List<MatchParameterItem> parameters)
    {
        foreach (var p in parameters)
            Parameters.Add(p);

        SelectAllCommand = new RelayCommand(_ =>
        {
            foreach (var p in Parameters) p.IsSelected = true;
        });

        SelectNoneCommand = new RelayCommand(_ =>
        {
            foreach (var p in Parameters) p.IsSelected = false;
        });

        SelectSafeCommand = new RelayCommand(_ =>
        {
            foreach (var p in Parameters) p.IsSelected = p.IsSafe;
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
    }

    public event Action? CloseRequested;
    private void OnCloseRequested() => CloseRequested?.Invoke();
}
