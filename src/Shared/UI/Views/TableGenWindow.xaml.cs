using System.Windows;
using System.Windows.Controls;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class TableGenWindow : Window
{
    private readonly TableGenViewModel _vm;

    public TableGenWindow(IRevitService service)
    {
        InitializeComponent();
        _vm = new TableGenViewModel(service);
        _vm.CloseAction = () => Close();
        DataContext = _vm;
    }

    private void OnSheetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _vm.OnSheetChanged();
    }
}
