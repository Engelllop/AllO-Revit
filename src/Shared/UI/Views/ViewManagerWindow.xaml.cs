using System.Windows;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class ViewManagerWindow : Window
{
    public ViewManagerWindow(ViewManagerViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += () => Close();
    }

    public bool ShowDialogAndGetResult()
    {
        var vm = (ViewManagerViewModel)DataContext;
        ShowDialog();
        return vm.DialogResult == true;
    }
}
