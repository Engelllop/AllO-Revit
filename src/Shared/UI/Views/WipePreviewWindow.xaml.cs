using System.Windows;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class WipePreviewWindow : Window
{
    public WipePreviewWindow(WipePreviewViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.CloseRequested += () => Close();
    }

    public bool? ShowDialogAndGetResult()
    {
        var vm = (WipePreviewViewModel)DataContext;
        var result = ShowDialog();
        return vm.DialogResult;
    }
}
