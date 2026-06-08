using System.Windows;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class ReorderWindow : Window
{
    public ReorderWindow(ReorderViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += () => Close();
    }

    public bool? ShowDialogAndGetResult()
    {
        var vm = (ReorderViewModel)DataContext;
        var result = ShowDialog();
        return vm.DialogResult;
    }
}
