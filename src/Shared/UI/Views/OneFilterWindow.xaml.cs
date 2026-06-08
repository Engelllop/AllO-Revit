using System.Windows;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class OneFilterWindow : Window
{
    public OneFilterWindow(OneFilterViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += () => Close();
    }

    public bool? ShowDialogAndGetResult()
    {
        var vm = (OneFilterViewModel)DataContext;
        var result = ShowDialog();
        return vm.DialogResult;
    }
}
