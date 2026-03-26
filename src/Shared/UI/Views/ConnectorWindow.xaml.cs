using System.Windows;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class ConnectorWindow : Window
{
    public ConnectorWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new ConnectorViewModel(service);
        vm.CloseAction = () => Close();
        DataContext = vm;
    }
}
