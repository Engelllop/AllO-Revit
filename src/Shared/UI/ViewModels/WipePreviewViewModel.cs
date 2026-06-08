using System.Collections.ObjectModel;
using System.Windows.Input;
using AllO.Core;
using AllO.Models;

namespace AllO.UI.ViewModels;

public class WipePreviewViewModel : ViewModelBase
{
    public ObservableCollection<WipeItem> Items { get; } = new();

    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand ExecuteCommand { get; }
    public ICommand CancelCommand { get; }

    public bool? DialogResult { get; private set; }

    public WipePreviewViewModel(List<WipeItem> items)
    {
        foreach (var item in items)
            Items.Add(item);

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
    }

    public event Action? CloseRequested;
    private void OnCloseRequested() => CloseRequested?.Invoke();
}
