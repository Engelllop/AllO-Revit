using System.Windows;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class FamilyTransferWindow : Window
{
    public FamilyTransferWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new FamilyTransferViewModel(service);
        vm.CloseAction = Close;
        DataContext = vm;
    }
}
