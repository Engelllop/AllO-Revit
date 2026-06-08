using System.Windows;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class MatchParameterWindow : Window
{
    public MatchParameterWindow(MatchParameterViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += () => Close();
    }

    public bool? ShowDialogAndGetResult()
    {
        var vm = (MatchParameterViewModel)DataContext;
        var result = ShowDialog();
        return vm.DialogResult;
    }
}
