using System.Collections.ObjectModel;
using System.Windows.Input;
using AllO.Core;

namespace AllO.UI.ViewModels;

public class OneFilterViewModel : ViewModelBase
{
    public ObservableCollection<string> Parameters { get; } = new();
    public ObservableCollection<string> Operators { get; } = new() { "Equals", "Contains", "Greater", "Less" };

    private string _selectedParameter = "";
    public string SelectedParameter
    {
        get => _selectedParameter;
        set => SetProperty(ref _selectedParameter, value);
    }

    private string _selectedOperator = "Equals";
    public string SelectedOperator
    {
        get => _selectedOperator;
        set => SetProperty(ref _selectedOperator, value);
    }

    private string _targetValue = "";
    public string TargetValue
    {
        get => _targetValue;
        set => SetProperty(ref _targetValue, value);
    }

    private bool _useSelection;
    public bool UseSelection
    {
        get => _useSelection;
        set => SetProperty(ref _useSelection, value);
    }

    public ICommand ExecuteCommand { get; }
    public ICommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }

    public OneFilterViewModel(List<string> parameters, bool hasSelection)
    {
        foreach (var p in parameters)
            Parameters.Add(p);
        if (Parameters.Count > 0)
            SelectedParameter = Parameters[0];
        UseSelection = hasSelection;

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
