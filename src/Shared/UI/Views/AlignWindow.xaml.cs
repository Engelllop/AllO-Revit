using System.Windows;
using AllO.Services;
using AllO.UI.ViewModels;

namespace AllO.UI.Views;

public partial class AlignWindow : Window
{
    public AlignWindow(IRevitService service)
    {
        InitializeComponent();
        var vm = new AlignViewModel(service);
        vm.CloseAction = () => Close();
        DataContext = vm;
    }
}
